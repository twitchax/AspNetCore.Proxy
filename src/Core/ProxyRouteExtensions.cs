using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AspNetCore.Proxy
{
    /// <summary>
    /// Proxy extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ProxyExtensions
    {
        /// <summary>
        /// Middleware which instructs the runtime to detect static methods with [<see cref="ProxyRouteAttribute"/>] and route them.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>  
        public static void UseProxies(this IApplicationBuilder app)
        {
            var methods = Helpers.GetReferencingAssemblies().SelectMany(a => a.GetTypes()).SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttributes(typeof(ProxyRouteAttribute), false).Length > 0);

            foreach(var method in methods)
            {
                var name = method.Name;
                var attribute = method.GetCustomAttributes(typeof(ProxyRouteAttribute), false).First() as ProxyRouteAttribute;
                var parameters = method.GetParameters();

                if(!(method.ReturnType == typeof(Task<string>)))
                    throw new InvalidOperationException($"Proxied generator method ({name}) must return a Task<string>.");

                if(!method.IsStatic)
                    throw new InvalidOperationException($"Proxied generator method ({name}) must be static.");
                
                app.UseProxy(attribute.Route, args => {
                    if(args.Count() != parameters.Count())
                        throw new InvalidOperationException($"Proxied generator method ({name}) parameter mismatch.");

                    var castedArgs = args.Zip(parameters, (a, p) => new { ArgumentValue = a.Value.ToString(), ArgumentType = p.ParameterType, ParameterName = p.Name }).Select(z => {
                        try
                        {
                            return TypeDescriptor.GetConverter(z.ArgumentType).ConvertFromString(z.ArgumentValue);
                        }
                        catch(Exception)
                        { 
                            throw new InvalidOperationException($"Proxied generator method ({name}) cannot cast to {z.ArgumentType.FullName} for parameter {z.ParameterName}.");
                        }
                    });

                    return method.Invoke(null, castedArgs.ToArray()) as Task<string>;
                });
            }
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A functor which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A catch all for failures.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<IDictionary<string, object>, Task<string>> getProxiedAddress, Func<HttpContext, Exception, Task> onFailure = null)
        {
            app.UseRouter(builder => {
                builder.MapMiddlewareRoute(endpoint, proxyApp => {
                    proxyApp.Run(async context => {
                        try
                        {
                            var proxiedAddress = await getProxiedAddress(context.GetRouteData().Values.ToDictionary(v => v.Key, v => v.Value)).ConfigureAwait(false);
                            var proxiedResponse = await context.SendProxyHttpRequest(proxiedAddress).ConfigureAwait(false);
                            
                            await context.CopyProxyHttpResponse(proxiedResponse).ConfigureAwait(false);
                        }
                        catch(Exception e)
                        {
                            if(onFailure == null)
                            {
                                // If the failures are not caught, then write a generic response.
                                context.Response.StatusCode = 500;
                                await context.Response.WriteAsync($"Request could not be proxied.\n\n{e.Message}\n\n{e.StackTrace}.").ConfigureAwait(false);
                                return;
                            }
                            
                            await onFailure(context, e).ConfigureAwait(false);
                        }
                    });
                });
            });
        }
    }
}