using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.Proxy.Options
{
    /// <summary>
    /// Defines the builder options that can be set for proxied WebSocket operations.
    /// </summary>
    public interface IWsProxyOptionsBuilder : IBuilder<IWsProxyOptionsBuilder, WsProxyOptions>
    {
        /// <summary>
        /// Sets the buffer size for the proxy calls.
        /// Default is `4096`.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <returns>This instance.</returns>
        IWsProxyOptionsBuilder WithBufferSize(int bufferSize);

        /// <summary>
        /// A <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </summary>
        /// <param name="intercept"></param>
        /// <returns>This instance.</returns>
        IWsProxyOptionsBuilder WithIntercept(Func<HttpContext, ValueTask<bool>> intercept);

        /// <summary>
        /// An <see cref="Func{HttpContext, ClientWebSocketOptions, Task}"/> that is invoked before the connect to the remote endpoint.
        /// The <see cref="ClientWebSocketOptions"/> can be edited before the call.
        /// </summary>
        /// <param name="beforeConnect"></param>
        /// <returns>This instance.</returns>
        IWsProxyOptionsBuilder WithBeforeConnect(Func<HttpContext, ClientWebSocketOptions, Task> beforeConnect);

        /// <summary>
        /// A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>This instance.</returns>
        IWsProxyOptionsBuilder WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure);
    }

    /// <summary>
    /// Defines the builder options that can be set for proxied WebSocket operations.
    /// </summary>
    public class WsProxyOptionsBuilder : IWsProxyOptionsBuilder
    {
        private int _bufferSize = 4096;
        private Func<HttpContext, ValueTask<bool>> _intercept;
        private Func<HttpContext, ClientWebSocketOptions, Task> _beforeConnect;
        private Func<HttpContext, Exception, Task> _handleFailure;

        /// <summary>
        /// The default constructor.
        /// </summary>
        private WsProxyOptionsBuilder()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="HttpProxyOptions"/> for building purposes.
        /// </summary>
        /// <returns>A new, default, instance of <see cref="HttpProxyOptions"/>.</returns>
        public static WsProxyOptionsBuilder Instance => new WsProxyOptionsBuilder();

        public IWsProxyOptionsBuilder New()
        {
            return Instance
                .WithBufferSize(_bufferSize)
                .WithIntercept(_intercept)
                .WithBeforeConnect(_beforeConnect)
                .WithHandleFailure(_handleFailure);
        }

        /// <summary>
        /// Build the equivalent <see cref="WsProxyOptions"/>.
        /// </summary>
        /// <returns>The equivalent <see cref="WsProxyOptions"/>.</returns>
        public WsProxyOptions Build()
        {
            return new WsProxyOptions(
                _bufferSize,
                _intercept,
                _beforeConnect,
                _handleFailure);
        }

        public IWsProxyOptionsBuilder WithBufferSize(int bufferSize)
        {
            _bufferSize = bufferSize;
            return this;
        }

        public IWsProxyOptionsBuilder WithIntercept(Func<HttpContext, ValueTask<bool>> intercept)
        {
            _intercept = intercept;
            return this;
        }

        public IWsProxyOptionsBuilder WithBeforeConnect(Func<HttpContext, ClientWebSocketOptions, Task> beforeConnect)
        {
            _beforeConnect = beforeConnect;
            return this;
        }

        /// <summary>
        /// A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.
        /// </summary>
        /// <param name="handleFailure"></param>
        /// <returns>This instance.</returns>
        public IWsProxyOptionsBuilder WithHandleFailure(Func<HttpContext, Exception, Task> handleFailure)
        {
            _handleFailure = handleFailure;
            return this;
        }
    }

    /// <summary>
    /// Defines the options that can be set for proxied WebSocket operations.
    /// </summary>
    public class WsProxyOptions
    {
        public int BufferSize { get; }

        /// <summary>
        /// Intercept property.
        /// </summary>
        /// <value>
        /// A <see cref="Func{HttpContext, Task}"/> that is invoked upon a call.
        /// The result should be `true` if the call is intercepted and **not** meant to be forwarded.
        /// </value>
        public Func<HttpContext, ValueTask<bool>> Intercept { get; }

        public Func<HttpContext, ClientWebSocketOptions, Task> BeforeConnect { get; }

        /// <summary>
        /// HandleFailure property.
        /// </summary>
        /// <value>A <see cref="Func{HttpContext, Exception, Task}"/> that is invoked once if the proxy operation fails.</value>
        public Func<HttpContext, Exception, Task> HandleFailure { get; }

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="handleFailure"></param>
        public WsProxyOptions(
            int bufferSize,
            Func<HttpContext, ValueTask<bool>> intercept,
            Func<HttpContext, ClientWebSocketOptions, Task> beforeConnect,
            Func<HttpContext, Exception, Task> handleFailure)
        {
            BufferSize = bufferSize;
            Intercept = intercept;
            BeforeConnect = beforeConnect;
            HandleFailure = handleFailure;
        }
    }
}