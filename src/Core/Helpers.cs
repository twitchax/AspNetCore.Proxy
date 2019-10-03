using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.Proxy
{
    internal static class Helpers
    {
        internal static readonly string ProxyClientName = "AspNetCore.Proxy.ProxyClient";

        internal static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
            {
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
                catch (Exception) { }
            }
            return assemblies;
        }

        internal static async Task ExecuteProxyOperation(HttpContext context, string uri, ProxyOptions options = null)
        {
            try
            {
                var proxiedRequest = context.CreateProxiedHttpRequest(uri, options?.ShouldAddForwardedHeaders ?? true);

                if(options?.BeforeSend != null)
                    await options.BeforeSend(context, proxiedRequest).ConfigureAwait(false);
                var proxiedResponse = await context.SendProxiedHttpRequest(proxiedRequest).ConfigureAwait(false);

                if(options?.AfterReceive != null)
                    await options.AfterReceive(context, proxiedResponse).ConfigureAwait(false);
                await context.WriteProxiedHttpResponse(proxiedResponse).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (options?.HandleFailure == null)
                {
                    // If the failures are not caught, then write a generic response.
                    context.Response.StatusCode = 502;
                    await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}.").ConfigureAwait(false);
                    return;
                }

                await options.HandleFailure(context, e).ConfigureAwait(false);
            }
        }
    }

    internal static class Extensions
    {
        internal static HttpRequestMessage CreateProxiedHttpRequest(this HttpContext context, string uriString, bool shouldAddForwardedHeaders)
        {
            var uri = new Uri(uriString);
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;

            // Write to request content, when necessary.
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers.
            foreach (var header in context.Request.Headers)
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

            // Add forwarded headers.
            if(shouldAddForwardedHeaders)
                AddForwardedHeadersToRequest(context, requestMessage);

            // Set destination and method.
            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        internal static Task<HttpResponseMessage> SendProxiedHttpRequest(this HttpContext context, HttpRequestMessage message)
        {
            return context.RequestServices
                .GetService<IHttpClientFactory>()
                .CreateClient(Helpers.ProxyClientName)
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }

        internal static async Task WriteProxiedHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
        {
            var response = context.Response;

            response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            response.Headers.Remove("transfer-encoding");
            
            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                await responseStream.CopyToAsync(response.Body, 81920, context.RequestAborted).ConfigureAwait(false);
            }
        }

        private static void AddForwardedHeadersToRequest(HttpContext context, HttpRequestMessage requestMessage)
        {
            var request = context.Request;
            var connection = context.Connection;

            var host = request.Host.ToString();
            var protocol = request.Scheme;

            var localIp = connection.LocalIpAddress?.ToString();
            var isLocalIpV6 = connection.LocalIpAddress?.AddressFamily == AddressFamily.InterNetworkV6;

            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            var isRemoteIpV6 = connection.RemoteIpAddress?.AddressFamily == AddressFamily.InterNetworkV6;

            if(remoteIp != null)
                requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-For", remoteIp);
            requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", protocol);
            requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);

            // Fix IPv6 IPs for the `Forwarded` header.
            var forwardedHeader = new StringBuilder($"proto={protocol};host={host};");

            if(localIp != null)
            {
                if(isLocalIpV6)
                    localIp = $"\"[{localIp}]\"";

                forwardedHeader.Append($"by={localIp};");
            }

            if(remoteIp != null)
            {
                if(isRemoteIpV6)
                    remoteIp = $"\"[{remoteIp}]\"";

                forwardedHeader.Append($"for={remoteIp};");
            }

            requestMessage.Headers.TryAddWithoutValidation("Forwarded", forwardedHeader.ToString());
        }
    }
}
