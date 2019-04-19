using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace AspNetCore.Proxy
{
    internal static class Helpers
    {
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

        internal static async Task HandleProxy(HttpContext context, string uri, Func<HttpContext, Exception, Task> onFailure = null)
        {
            try
            {
                var proxiedResponse = await context.SendProxyHttpRequest(uri).ConfigureAwait(false);
                await context.CopyProxyHttpResponse(proxiedResponse).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (onFailure == null)
                {
                    // If the failures are not caught, then write a generic response.
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}.").ConfigureAwait(false);
                    return;
                }

                await onFailure(context, e).ConfigureAwait(false);
            }
        }
    }

    internal static class Extensions
    {
        private static HttpRequestMessage CreateProxyHttpRequest(this HttpContext context, string uriString)
        {
            var uri = new Uri(uriString);
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;

            // Copy the request headers.
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                if (request.HasFormContentType)
                {
                    var formFields = request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                    requestMessage.Content = new FormUrlEncodedContent(formFields);
                }
                else
                {
                    var streamContent = new StreamContent(request.Body);
                    requestMessage.Content = streamContent;
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        internal static Task<HttpResponseMessage> SendProxyHttpRequest(this HttpContext context, string proxiedAddress)
        {
            var proxiedRequest = context.CreateProxyHttpRequest(proxiedAddress);

            return context.RequestServices
                .GetService<IHttpClientFactory>()
                .CreateClient()
                .SendAsync(proxiedRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }

        internal static async Task CopyProxyHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
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
    }
}