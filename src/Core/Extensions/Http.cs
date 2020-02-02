using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Proxy.Extensions
{
    internal static class HttpExtensions
    {
        internal static async Task ExecuteHttpProxyOperationAsync(this HttpContext context, HttpProxy httpProxy)
        {
            var uri = await context.GetEndpointFromComputerAsync(httpProxy.EndpointComputer).ConfigureAwait(false);
            var options = httpProxy.Options;

            try
            {
                if(context.WebSockets.IsWebSocketRequest)
                    throw new InvalidOperationException("A WebSocket request cannot be routed as an HTTP proxy operation.");

                if(!uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only forwarded addresses starting with 'http://' or 'https://' are supported for HTTP requests.");

                // If `true`, this proxy call has been intercepted.
                if(options?.Intercept != null && await options.Intercept(context).ConfigureAwait(false))
                    return;

                var proxiedRequest = context.CreateProxiedHttpRequest(uri, options?.ShouldAddForwardedHeaders ?? true);

                if(options?.BeforeSend != null)
                    await options.BeforeSend(context, proxiedRequest).ConfigureAwait(false);
                var proxiedResponse = await context
                    .SendProxiedHttpRequestAsync(proxiedRequest, options?.HttpClientName ?? Helpers.HttpProxyClientName)
                    .ConfigureAwait(false);

                if(options?.AfterReceive != null)
                    await options.AfterReceive(context, proxiedResponse).ConfigureAwait(false);
                await context.WriteProxiedHttpResponseAsync(proxiedResponse).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if(!context.Response.HasStarted)
                {
                    if (options?.HandleFailure == null)
                    {
                        // If the failures are not caught, then write a generic response.
                        context.Response.StatusCode = 502 /* BAD GATEWAY */;
                        await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}").ConfigureAwait(false);
                        return;
                    }

                    await options.HandleFailure(context, e).ConfigureAwait(false);
                }
            }
        }

        private static HttpRequestMessage CreateProxiedHttpRequest(this HttpContext context, string uriString, bool shouldAddForwardedHeaders)
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
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Add forwarded headers.
            if(shouldAddForwardedHeaders)
                AddForwardedHeadersToHttpRequest(context, requestMessage);

            // Set destination and method.
            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        private static Task<HttpResponseMessage> SendProxiedHttpRequestAsync(this HttpContext context, HttpRequestMessage message, string httpClientName)
        {
            return context.RequestServices
                .GetService<IHttpClientFactory>()
                .CreateClient(httpClientName)
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }

        private static Task WriteProxiedHttpResponseAsync(this HttpContext context, HttpResponseMessage responseMessage)
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

            return responseMessage.Content.CopyToAsync(response.Body);
        }

        private static void AddForwardedHeadersToHttpRequest(HttpContext context, HttpRequestMessage requestMessage)
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

                forwardedHeader.Append("by=").Append(localIp).Append(';');
            }

            if(remoteIp != null)
            {
                if(isRemoteIpV6)
                    remoteIp = $"\"[{remoteIp}]\"";

                forwardedHeader.Append("for=").Append(remoteIp).Append(';');
            }

            requestMessage.Headers.TryAddWithoutValidation("Forwarded", forwardedHeader.ToString());
        }
    }
}