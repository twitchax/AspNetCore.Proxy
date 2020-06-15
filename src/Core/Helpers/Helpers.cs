using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    }
}