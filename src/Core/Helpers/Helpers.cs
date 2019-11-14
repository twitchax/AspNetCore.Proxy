using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyModel;

namespace AspNetCore.Proxy
{
    public delegate string EndpointComputerToString(HttpContext context, IDictionary<string, object> arguments);
    public delegate ValueTask<string> EndpointComputerToValueTask(HttpContext context, IDictionary<string, object> arguments);

    public interface IBuilder<TInterface, TConcrete> where TConcrete : class
    {
        TInterface New();
        TConcrete Build();
    }

    internal static class Helpers
    {
        internal static readonly string HttpProxyClientName = "AspNetCore.Proxy.HttpProxyClient";
        internal static readonly string[] WebSocketNotForwardedHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Accept", "Sec-WebSocket-Protocol", "Sec-WebSocket-Key", "Sec-WebSocket-Version", "Sec-WebSocket-Extensions" };

        internal static IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var assemblies = new List<Assembly>();
            var dependencies = DependencyContext.Default.RuntimeLibraries;
            foreach (var library in dependencies)
            {
                try
                {
                    var assembly = Assembly.Load(new AssemblyName(library.Name));
                    assemblies.Add(assembly);
                }
                catch (Exception) { }
            }
            return assemblies;
        }
    }
}