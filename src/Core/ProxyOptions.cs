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
        /// HttpClientName property.
        /// </summary>
        /// <value>
        /// Overrides the default <see cref="HttpClient"/> used for making the proxy call.
        /// Default is `null`.
        /// </value>
        public string HttpClientName { get; set; } = null;

        /// <summary>
        /// HandleFailure property.
        /// </summary>
        /// <value>A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.</value>
        public Func<HttpContext, Exception, Task> HandleFailure { get; set; }

        /// <summary>
        /// BeforeSend property.
        /// </summary>
        /// <value>
        /// An <see cref="Func{HttpContext, HttpRequestMessage, Task}"/> that is invoked before the call to the remote endpoint.
        /// The <see cref="HttpRequestMessage"/> can be edited before the call.
        /// </value>
        public Func<HttpContext, HttpRequestMessage, Task> BeforeSend { get; set; }

        /// <summary>
        /// AfterReceive property.
        /// </summary>
        /// <value>
        /// An <see cref="Func{HttpContext, HttpResponseMessage, Task}"/> that is invoked before the response is written to the client.
        /// The <see cref="HttpResponseMessage"/> can be edited before the response is written to the client.
        /// </value>
        public Func<HttpContext, HttpResponseMessage, Task> AfterReceive { get; set; }
        
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ProxyOptions() {}

        private ProxyOptions(
            bool shouldAddForwardedHeaders,
            string httpClientName,
            Func<HttpContext, Exception, Task> handleFailure,
            Func<HttpContext, HttpRequestMessage, Task> beforeSend,
            Func<HttpContext, HttpResponseMessage, Task> afterReceive)
        {
            ShouldAddForwardedHeaders = shouldAddForwardedHeaders;
            HttpClientName = httpClientName;
            HandleFailure = handleFailure;
            BeforeSend = beforeSend;
            AfterReceive = afterReceive;
        }

        private static ProxyOptions CreateFrom(
            ProxyOptions old, 
            bool? shouldAddForwardedHeaders = null,
            string httpClientName = null,
            Func<HttpContext, Exception, Task> handleFailure = null,
            Func<HttpContext, HttpRequestMessage, Task> beforeSend = null,
            Func<HttpContext, HttpResponseMessage, Task> afterReceive = null)
        {
            return new ProxyOptions(
                shouldAddForwardedHeaders ?? old.ShouldAddForwardedHeaders,
                httpClientName ?? old.HttpClientName,
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
        /// Sets the <see cref="HttpClientName"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="httpClientName"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithHttpClientName(string httpClientName) => CreateFrom(this, httpClientName: httpClientName);

        /// <summary>
        /// Sets the <see cref="HandleFailure"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure) => CreateFrom(this, handleFailure: handleFailure);

        /// <summary>
        /// Sets the <see cref="BeforeSend"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="beforeSend"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithBeforeSend(Func<HttpContext, HttpRequestMessage, Task> beforeSend) => CreateFrom(this, beforeSend: beforeSend);

        /// <summary>
        /// Sets the <see cref="AfterReceive"/> property to a cloned instance of this <see cref="ProxyOptions"/>.
        /// </summary>
        /// <param name="afterReceive"></param>
        /// <returns>A new instance of <see cref="ProxyOptions"/> with the new value for the property.</returns>
        public ProxyOptions WithAfterReceive(Func<HttpContext, HttpResponseMessage, Task> afterReceive) => CreateFrom(this, afterReceive: afterReceive);
    }
}