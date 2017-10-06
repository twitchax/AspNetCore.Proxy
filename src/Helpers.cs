using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyModel;

namespace AspNetCore.Proxy
{
    public static class Helpers
    {
        public static IEnumerable<Assembly> GetReferencingAssemblies()
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
                catch(Exception) { }
            }
            return assemblies;
        }
    }

    public static class Extensions
    {
        public static HttpRequestMessage CreateProxyHttpRequest(this HttpContext context, string uriString)
        {
            var uri = new Uri(uriString);
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        public static Task<HttpResponseMessage> SendProxyHttpRequest(this HttpContext context, string proxiedAddress)
        {
            var proxiedRequest = context.CreateProxyHttpRequest(proxiedAddress);
            return new HttpClient().SendAsync(proxiedRequest, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }
        
        public static async Task CopyProxyHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
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

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await responseStream.CopyToAsync(response.Body, 81920, context.RequestAborted);
            }
        }
        
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<IDictionary<string, object>, Task<string>> getProxiedAddress, Func<HttpContext, Exception, Task> onFailure = null)
        {
            app.UseRouter(builder => {
                builder.MapMiddlewareRoute(endpoint, proxyApp => {
                    proxyApp.Run(async context => {
                        try
                        {
                            var proxiedAddress = await getProxiedAddress(context.GetRouteData().Values.ToDictionary(v => v.Key, v => v.Value));

                            var proxiedResponse = await context.SendProxyHttpRequest(proxiedAddress);
                            
                            await context.CopyProxyHttpResponse(proxiedResponse);
                        }
                        catch(Exception e)
                        {
                            if(onFailure == null)
                            {
                                context.Response.StatusCode = 500;
                                await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}.");
                                return;
                            }
                            
                            await onFailure(context, e);
                        }
                    });
                });
            });
        }
    }
}