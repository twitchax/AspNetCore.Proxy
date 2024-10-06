using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Proxy
{
    internal static class HttpExtensions
    {
        internal static async Task ExecuteHttpProxyOperationAsync(this HttpContext context, HttpProxy httpProxy)
        {
            var uri = await context.GetEndpointFromComputerAsync(httpProxy.EndpointComputer).ConfigureAwait(false);
            var options = httpProxy.Options;

            try
            {
                var clientFactory = context.RequestServices.GetService<IHttpClientFactory>()
                ?? throw new InvalidOperationException("IHttpClientFactory not registered. You need to add `services.AddProxies()` during startup (or configure a custom client with `builder.Services.AddHttpClient()`).");

                var httpClient = clientFactory.CreateClient(options?.HttpClientName ?? Helpers.HttpProxyClientName);

                // If `true`, this proxy call has been intercepted.
                if(options?.Intercept != null && await options.Intercept(context).ConfigureAwait(false))
                    return;

                if(context.WebSockets.IsWebSocketRequest)
                    throw new InvalidOperationException("A WebSocket request cannot be routed as an HTTP proxy operation.");

                if(!uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    if(httpClient.BaseAddress != null)
                        uri = $"{httpClient.BaseAddress}{uri}";
                    else
                        throw new InvalidOperationException("Only forwarded addresses starting with 'http://' or 'https://' are supported for HTTP requests.");
                }

                var proxiedRequest = await context.CreateProxiedHttpRequestAsync(uri, options?.ShouldAddForwardedHeaders ?? true);

                if(options?.BeforeSend != null)
                    await options.BeforeSend(context, proxiedRequest).ConfigureAwait(false);
                var proxiedResponse = await context
                    .SendProxiedHttpRequestAsync(proxiedRequest, httpClient)
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

        private async static Task<string> ReadRequestBodyAsStringAsync(HttpRequest request)
        {
            request.EnableBuffering();
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var bodyString = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            return bodyString;
        }

        private static string GetContentType(HttpRequest request)
        {
            if(request.Headers.TryGetValue("Content-Type", out var contentType))
            {
                return contentType.ToString();
            }
            return request.ContentType;
        }

        private static bool IsTextBasedMimeType(HttpRequest request)
        {
            var textBased = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/json",
                "application/xml",
                "text/html",
                "text/plain",
                "text/css",
                "text/javascript",
                "text/markdown",
                "text/csv"
            };
            
            return textBased.Contains(GetContentType(request));
        }

        private async static Task<HttpRequestMessage> CreateProxiedHttpRequestAsync(this HttpContext context, string uriString, bool shouldAddForwardedHeaders)
        {
            var uri = new Uri(uriString);
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            var usesStreamContent = true; // When using other content types, they specify the Content-Type header, and may also change the Content-Length.

            // Write to request content, when necessary.
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                if (request.HasFormContentType)
                {
                    usesStreamContent = false;
                    requestMessage.Content = request.Form.ToHttpContent(request);
                }
                else if(IsTextBasedMimeType(request))
                {
                    usesStreamContent = false;
                    var bodyString = await ReadRequestBodyAsStringAsync(request);
                    requestMessage.Content = new StringContent(bodyString, Encoding.UTF8, GetContentType(request));
                }
                else
                {
                    requestMessage.Content = new StreamContent(request.Body);
                }
            }

            // Copy the request headers.
            foreach (var header in request.Headers)
            {
                if (!usesStreamContent && (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            // Add forwarded headers.
            if(shouldAddForwardedHeaders)
                AddForwardedHeadersToHttpRequest(context, requestMessage);

            // Set destination and method.
            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(requestMethod);

            return requestMessage;
        }

        private static Task<HttpResponseMessage> SendProxiedHttpRequestAsync(this HttpContext context, HttpRequestMessage message, HttpClient httpClient)
        {
            return httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        }

        private static Task WriteProxiedHttpResponseAsync(this HttpContext context, HttpResponseMessage responseMessage)
        {
            var response = context.Response;

            response.StatusCode = (int)responseMessage.StatusCode;

            var httpResponseFeature = context.Features.Get<IHttpResponseFeature>();
            if (httpResponseFeature != null)
            {
                httpResponseFeature.ReasonPhrase = responseMessage.ReasonPhrase;
            }

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
