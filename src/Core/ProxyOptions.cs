
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy
{
    /// <summary>
    /// Defines options that can be set for proxied operations.
    /// </summary>
    public class ProxyOptions
    {
        /// <summary>
        /// ShouldAddForwardedHeaders property.
        /// </summary>
        /// <value>
        /// Instructs the proxy operation to add `Forwarded` and `X-Forwarded-*` headers.
        /// Default behavior is `true`.
        /// </value>
        public bool ShouldAddForwardedHeaders { get; set; } = true;

        /// <summary>
        /// HandleFailure property.
        /// </summary>
        /// <value>An <see cref="Action"/> that is invoked once if the proxy operation fails.</value>
        public Action<HttpContext, Exception> HandleFailure { get; set; }

        /// <summary>
        /// BeforeSend property.
        /// </summary>
        /// <value>
        /// An <see cref="Action"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </value>
        public Action<HttpContext, HttpRequestMessage> BeforeSend { get; set; }

        /// <summary>
        /// AfterReceive property.
        /// </summary>
        /// <value>
        /// An <see cref="Action"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </value>
        public Action<HttpContext, HttpResponseMessage> AfterReceive { get; set; }
        
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ProxyOptions() {}

        private ProxyOptions(
            bool shouldAddForwardedHeaders,
            Action<HttpContext, Exception> handleFailure,
            Action<HttpContext, HttpRequestMessage> beforeSend,
            Action<HttpContext, HttpResponseMessage> afterReceive)
        {
            ShouldAddForwardedHeaders = shouldAddForwardedHeaders;
            HandleFailure = handleFailure;
            BeforeSend = beforeSend;
            AfterReceive = afterReceive;
        }

        private static ProxyOptions CreateFrom(
            ProxyOptions old, 
            bool? shouldAddForwardedHeaders = null,
            Action<HttpContext, Exception> handleFailure = null,
            Action<HttpContext, HttpRequestMessage> beforeSend = null,
            Action<HttpContext, HttpResponseMessage> afterReceive = null)
        {
            return new ProxyOptions(
                shouldAddForwardedHeaders ?? old.ShouldAddForwardedHeaders,
                handleFailure ?? old.HandleFailure,
                beforeSend ?? old.BeforeSend,
                afterReceive ?? old.AfterReceive);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProxyOptions"/> for building purposes.
        /// </summary>
        /// <returns>A new, default, instance of <see cref="ProxyOptions"/>.</returns>
        public static ProxyOptions Instance => new ProxyOptions();

        /// <summary>
        /// Sets the <see cref="ShouldAddForwardedHeaders"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="shouldAddForwardedHeaders"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithShouldAddForwardedHeaders(bool shouldAddForwardedHeaders) => CreateFrom(this, shouldAddForwardedHeaders: shouldAddForwardedHeaders);

        /// <summary>
        /// Sets the <see cref="HandleFailure"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithHandleFailure(Action<HttpContext, Exception> handleFailure) => CreateFrom(this, handleFailure: handleFailure);

        /// <summary>
        /// Sets the <see cref="BeforeSend"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="beforeSend"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithBeforeSend(Action<HttpContext, HttpRequestMessage> beforeSend) => CreateFrom(this, beforeSend: beforeSend);

        /// <summary>
        /// Sets the <see cref="AfterReceive"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="afterReceive"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithAfterReceive(Action<HttpContext, HttpResponseMessage> afterReceive) => CreateFrom(this, afterReceive: afterReceive);
    }
}