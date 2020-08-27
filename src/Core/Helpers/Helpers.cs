using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy
{
    /// <summary>
    /// The delegate type for computing an endpoint from a context and set of arguments.
    /// This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="String"/>`.
    /// </summary>
    /// <param name="context">The HTTP context of the current request at runtime.</param>
    /// <param name="arguments">The arguments of the current request at runtime.</param>
    /// <returns>The endpoint string.</returns>
    public delegate string EndpointComputerToString(HttpContext context, IDictionary<string, object> arguments);

    /// <summary>
    /// The delegate type for computing an endpoint from a context and set of arguments.
    /// This takes the form `(<see cref="HttpContext"/>, <see cref="IDictionary{String, Object}"/>) => <see cref="ValueTask{String}"/>`.
    /// </summary>
    /// <param name="context">The HTTP context of the current request at runtime.</param>
    /// <param name="arguments">The arguments of the current request at runtime.</param>
    /// <returns>The endpoint string as a <see cref="ValueTask{String}"/>.</returns>
    public delegate ValueTask<string> EndpointComputerToValueTask(HttpContext context, IDictionary<string, object> arguments);

    /// <summary>
    /// Interface for a builder.
    /// </summary>
    /// <typeparam name="TInterface">The interface type of the builder.</typeparam>
    /// <typeparam name="TConcrete">The concrete type that the builder creates upon being built.</typeparam>
    public interface IBuilder<TInterface, TConcrete> where TConcrete : class
    {
        /// <summary>
        /// Gets a 'new` instance initialized from `this` instance.
        /// </summary>
        /// <returns>A `new` instance of <typeparamref name="TInterface"/> initialized from `this` instance.</returns>
        ///
        TInterface New();

        /// <summary>
        /// Gets a `new` instance of <typeparamref name="TConcrete"/> initialized from `this` <typeparamref name="TInterface"/> settings.
        /// </summary>
        /// <returns>A `new` instance of <typeparamref name="TConcrete"/> initialized from `this` <typeparamref name="TInterface"/> settings.</returns>
        TConcrete Build();
    }

    internal static class Helpers
    {
        internal static readonly string HttpProxyClientName = "AspNetCore.Proxy.HttpProxyClient";
        internal static readonly string[] WebSocketNotForwardedHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Accept", "Sec-WebSocket-Protocol", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "Sec-WebSocket-Extensions" };

        internal static string TrimTrailingSlashes(this string s)
        {
            if(!s.EndsWith("/"))
                return s;

            var span = s.AsSpan();
            var count = 0;

            for(int k = span.Length - 1; k >= 0; k--)
            {
                if(s[k] == '/')
                    count++;
                else
                    break;
            }

            return s.Substring(0, s.Length - count);
        }

        internal static HttpContent ToHttpContent(this IFormCollection collection, string contentTypeHeader)
        {
            // @PreferLinux:
            // Form content types resource: https://stackoverflow.com/questions/4526273/what-does-enctype-multipart-form-data-mean/28380690
            // There are three possible form content types:
            // - text/plain, which should never be used and this does not handle (a request with that will not have IsFormContentType true anyway)
            // - application/x-www-form-urlencoded, which doesn't handle file uploads and escapes any special characters
            // - multipart/form-data, which does handle files and doesn't require any escaping, but is quite bulky for short data (due to using some content headers for each value, and a boundary sequence between them)

            // A single form element can have multiple values. When sending them they are handled as separate items with the same name, not a singe item with multiple values.
            // For example, a=1&a=2.

            var contentType = MediaTypeHeaderValue.Parse(contentTypeHeader);

            if (contentType.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)) // specification: https://url.spec.whatwg.org/#concept-urlencoded
                return new FormUrlEncodedContent(collection.SelectMany(formItemList => formItemList.Value.Select(value => new KeyValuePair<string, string>(formItemList.Key, value))));

            if (!contentType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Unknown form content type `{contentType.MediaType}`.");

            // multipart/form-data specification https://tools.ietf.org/html/rfc7578
            // It has each value separated by a boundary sequence, which is specified in the Content-Type header.
            // As a proxy it is probably best to reuse the boundary used in the original request as it is not necessarily random.
            var delimiter = contentType.Parameters.Single(p => p.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase)).Value.Trim('"');

            var multipart = new MultipartFormDataContent(delimiter);
            foreach (var formVal in collection)
            {
                foreach (var value in formVal.Value)
                    multipart.Add(new StringContent(value), formVal.Key);
            }
            foreach (var file in collection.Files)
            {
                var content = new StreamContent(file.OpenReadStream());
                foreach (var header in file.Headers)
                    content.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                multipart.Add(content, file.Name, file.FileName);
            }
            return multipart;
        }
    }
}