using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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

        internal static HttpContent ToHttpContent(this IFormCollection collection, string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType) || contentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                return new FormUrlEncodedContent(collection.SelectMany(formItemList => formItemList.Value.Select(value => new KeyValuePair<string, string>(formItemList.Key, value))));

            if (!contentType.StartsWith("multipart/form-data;", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Unknown form content type \"{contentType[0]}\"");

            const string boundary = "boundary=";
            var boundaryIndex = contentType.IndexOf(boundary, StringComparison.OrdinalIgnoreCase);
            if (boundaryIndex < 0)
                throw new Exception("Could not find multipart boundary");
            var delimiter = contentType.Substring(boundaryIndex + boundary.Length);

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