using System;
using System.Threading.Tasks;
using AspNetCore.Proxy.Options;

namespace AspNetCore.Proxy.Builders
{
    public interface IHttpProxyBuilder : IBuilder<IHttpProxyBuilder, HttpProxy>
    {
        IHttpProxyBuilder WithEndpoint(string endpoint);
        IHttpProxyBuilder WithEndpoint(EndpointComputerToString endpoint);
        IHttpProxyBuilder WithEndpoint(EndpointComputerToValueTask endpoint);

        IHttpProxyBuilder WithOptions(IHttpProxyOptionsBuilder options);
        IHttpProxyBuilder WithOptions(Action<IHttpProxyOptionsBuilder> builderAction);
    }

    public class HttpProxyBuilder : IHttpProxyBuilder
    {
        private EndpointComputerToValueTask _endpointComputer;

        private IHttpProxyOptionsBuilder _optionsBuilder;

        private HttpProxyBuilder()
        {
        }

        public static HttpProxyBuilder Instance => new HttpProxyBuilder();

        public IHttpProxyBuilder New()
        {
            return Instance
                .WithEndpoint(_endpointComputer)
                .WithOptions(_optionsBuilder?.New());
        }

        public HttpProxy Build()
        {
            if(_endpointComputer == null)
                throw new Exception("The endpoint must be specified on this HTTP proxy builder.");

            return new HttpProxy(
                _endpointComputer,
                _optionsBuilder?.Build());
        }

        public IHttpProxyBuilder WithEndpoint(string endpoint) => this.WithEndpoint((context, args) => new ValueTask<string>(endpoint));

        public IHttpProxyBuilder WithEndpoint(EndpointComputerToString endpointComputer) => this.WithEndpoint((context, args) => new ValueTask<string>(endpointComputer(context, args)));

        public IHttpProxyBuilder WithEndpoint(EndpointComputerToValueTask endpointComputer)
        {
            _endpointComputer = endpointComputer;
            return this;
        }

        public IHttpProxyBuilder WithOptions(IHttpProxyOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
            return this;
        }

        public IHttpProxyBuilder WithOptions(Action<IHttpProxyOptionsBuilder> builderAction)
        {
            _optionsBuilder = HttpProxyOptionsBuilder.Instance;
            builderAction?.Invoke(_optionsBuilder);

            return this;
        }
    }

    public class HttpProxy
    {
        public EndpointComputerToValueTask EndpointComputer { get; internal set; }
        public HttpProxyOptions Options { get; internal set; }

        internal HttpProxy(EndpointComputerToValueTask endpointComputer, HttpProxyOptions options)
        {
            EndpointComputer = endpointComputer;
            Options = options;
        }
    }
}