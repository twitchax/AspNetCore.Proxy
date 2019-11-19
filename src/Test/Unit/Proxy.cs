using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Builders;
using AspNetCore.Proxy.Options;
using Moq;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public partial class UnitTests
    {
        [Fact]
        public async Task CanExerciseProxyBuilder()
        {
            var httpEndpoint = "http://from";
            var wsEndpoint = "ws://from";

            var proxyString = ProxyBuilder.Instance.UseHttp(httpEndpoint).UseWs(wsEndpoint).New().Build();
            Assert.Equal(httpEndpoint, await proxyString.HttpProxy.EndpointComputer(null, null));
            Assert.Equal(wsEndpoint, await proxyString.WsProxy.EndpointComputer(null, null));

            var proxyComputerToString = ProxyBuilder.Instance.UseHttp((c, a) => httpEndpoint).UseWs((c, a) => wsEndpoint).New().Build();
            Assert.Equal(httpEndpoint, await proxyComputerToString.HttpProxy.EndpointComputer(null, null));
            Assert.Equal(wsEndpoint, await proxyComputerToString.WsProxy.EndpointComputer(null, null));

            var proxyComputerToValueTask = ProxyBuilder.Instance.UseHttp((c, a) => new ValueTask<string>(httpEndpoint)).UseWs((c, a) => new ValueTask<string>(wsEndpoint)).New().Build();
            Assert.Equal(httpEndpoint, await proxyComputerToValueTask.HttpProxy.EndpointComputer(null, null));
            Assert.Equal(wsEndpoint, await proxyComputerToValueTask.WsProxy.EndpointComputer(null, null));
        }
        

        [Fact]
        public async Task CanProxyBuilderFailWithoutHttpOrWsProxy()
        {
            Assert.ThrowsAny<Exception>(() => 
            {
                ProxyBuilder.Instance.New().Build();
            });
        }

        [Fact]
        public async Task CanProxyBuilderFailWithMultiplProxiesOfSameType()
        {
            Assert.ThrowsAny<Exception>(() => {
                ProxyBuilder.Instance.UseHttp("").UseHttp("");
            });

            Assert.ThrowsAny<Exception>(() => {
                ProxyBuilder.Instance.UseWs("").UseWs("");
            });
        }

        [Fact]
        public async Task CanProxyBuilderFailWhenRoutelessAbused()
        {
            Assert.ThrowsAny<Exception>(() => {
                ProxyBuilder.Instance.WithIsRouteless(true).WithRoute("");
            });
        }
    }
}