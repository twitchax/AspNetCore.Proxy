using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyModel;

namespace AspNetCore.Proxy
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ProxyRouteAttribute : Attribute
    {
        public string Route { get; set; }

        public ProxyRouteAttribute(string route)
        {
            Route = route;
        }
    }

    public static class ProxyExtensions
    {
        public static void UseProxies(this IApplicationBuilder app)
        {
            var methods = Helpers.GetReferencingAssemblies().SelectMany(a => a.GetTypes()).SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(ProxyRouteAttribute), false).Length > 0);

            foreach(var method in methods)
            {
                var attribute = method.GetCustomAttributes(typeof(ProxyRouteAttribute), false).First() as ProxyRouteAttribute;
                var parameters = method.GetParameters();
                var instance = Activator.CreateInstance(method.DeclaringType);

                if(!(method.ReturnType == typeof(Task<string>)))
                    throw new Exception("Proxied methods must return a Task<string>.");
                
                app.UseProxy(attribute.Route, args => {
                    if(args.Count() != parameters.Count())
                        throw new Exception("Parameter mismatch.");

                    return method.Invoke(instance, args.Select(kvp => kvp.Value).ToArray()) as Task<string>;
                });
            }
        }
    }
}
