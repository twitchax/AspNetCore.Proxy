using System;
using System.ComponentModel;
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
                var name = method.Name;
                var attribute = method.GetCustomAttributes(typeof(ProxyRouteAttribute), false).First() as ProxyRouteAttribute;
                var parameters = method.GetParameters();

                if(!(method.ReturnType == typeof(Task<string>)))
                    throw new Exception($"Proxied generator method ({name}) must return a Task<string>.");

                if(!method.IsStatic)
                    throw new Exception($"Proxied generator method ({name}) must be static.");
                
                app.UseProxy(attribute.Route, args => {
                    if(args.Count() != parameters.Count())
                        throw new Exception($"Proxied generator method ({name}) parameter mismatch.");

                    var castedArgs = args.Zip(parameters, (a, p) => new { ArgumentValue = a.Value.ToString(), ArgumentType = p.ParameterType, ParameterName = p.Name }).Select(z => {
                        try
                        {
                            return TypeDescriptor.GetConverter(z.ArgumentType).ConvertFromString(z.ArgumentValue);
                        }
                        catch(Exception)
                        { 
                            throw new Exception($"Proxied generator method ({name}) cannot cast to {z.ArgumentType.FullName} for parameter {z.ParameterName}.");
                        }
                    });

                    return method.Invoke(null, args.Select(kvp => kvp.Value).ToArray()) as Task<string>;
                });
            }
        }
    }
}
