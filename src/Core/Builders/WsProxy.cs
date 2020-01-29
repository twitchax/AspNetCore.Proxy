using System;
using System.Threading.Tasks;
using AspNetCore.Proxy.Options;

namespace AspNetCore.Proxy.Builders
{
    public interface IWsProxyBuilder : IBuilder<IWsProxyBuilder, WsProxy>
    {
        IWsProxyBuilder WithEndpoint(string endpoint);
        IWsProxyBuilder WithEndpoint(EndpointComputerToString endpoint);
        IWsProxyBuilder WithEndpoint(EndpointComputerToValueTask endpoint);

        IWsProxyBuilder WithOptions(IWsProxyOptionsBuilder options);
        IWsProxyBuilder WithOptions(Action<IWsProxyOptionsBuilder> builderAction);
    }

    public class WsProxyBuilder : IWsProxyBuilder
    {
        private EndpointComputerToValueTask _endpointComputer;

        private IWsProxyOptionsBuilder _optionsBuilder;

        private WsProxyBuilder()
        {
        }

        public static WsProxyBuilder Instance => new WsProxyBuilder();

        public IWsProxyBuilder New()
        {
            return Instance
                .WithEndpoint(_endpointComputer)
                .WithOptions(_optionsBuilder?.New());
        }

        public WsProxy Build()
        {
            if(_endpointComputer == null)
                throw new Exception("The endpoint must be specified on this WebSocket proxy builder.");

            return new WsProxy(
                _endpointComputer,
                _optionsBuilder?.Build());
        }

        public IWsProxyBuilder WithEndpoint(string endpoint) => this.WithEndpoint((context, args) => new ValueTask<string>(endpoint));

        public IWsProxyBuilder WithEndpoint(EndpointComputerToString endpointComputer) => this.WithEndpoint((context, args) => new ValueTask<string>(endpointComputer(context, args)));
        
        public IWsProxyBuilder WithEndpoint(EndpointComputerToValueTask endpointComputer)
        {
            _endpointComputer = endpointComputer;
            return this;
        }

        public IWsProxyBuilder WithOptions(IWsProxyOptionsBuilder optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
            return this;
        }

        public IWsProxyBuilder WithOptions(Action<IWsProxyOptionsBuilder> builderAction)
        {
            _optionsBuilder = WsProxyOptionsBuilder.Instance;
            builderAction?.Invoke(_optionsBuilder);

            return this;
        }
    }

    public class WsProxy
    {
        public EndpointComputerToValueTask EndpointComputer { get; internal set; }
        public WsProxyOptions Options { get; internal set; }

        internal WsProxy(EndpointComputerToValueTask endpointComputer, WsProxyOptions options)
        {
            EndpointComputer = endpointComputer;
            Options = options;
        }
    }
}