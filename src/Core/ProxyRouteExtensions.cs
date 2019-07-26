using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Proxy
{
    /// <summary>
    /// Proxy extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ProxyExtensions
    {
        /// <summary>
        /// A <see cref="Controller"/> extension method which allows for a single, simple call to use a proxy
        /// in existing controllers.
        /// </summary>
        /// <param name="controller">The calling controller.</param>
        /// <param name="uri">The URI to proxy.</param>
        /// <param name="onFailure">A failure handler (otherwise, failures result in a generic 500).</param>
        /// <returns>
        /// A <see cref="Task"/> which, upon completion, has proxied the specified address and copied the response contents into
        /// the response for the <see cref="HttpContext"/>.
        /// </returns>
        public static Task ProxyAsync(this ControllerBase controller, string uri, Func<HttpContext, Exception, Task> onFailure = null)
        {
            return Helpers.HandleProxy(controller.HttpContext, uri, onFailure);
        }

        /// <summary>
        /// Adds the required services needed for proxying requests.
        /// </summary>
        /// <param name="services">The application service collection.</param>
        /// <returns>The same instance.</returns>
        public static IServiceCollection AddProxies(this IServiceCollection services)
        {
            return services.AddHttpClient();
        }

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

                if(method.ReturnType != typeof(Task<string>) && method.ReturnType != typeof(string))
                    throw new InvalidOperationException($"Proxied generator method ({name}) must return a `Task<string>` or `string`.");

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

                    // Make sure to always return a `Task<string>`, but allow methods that just return a `string`.
                    
                    if(method.ReturnType == typeof(Task<string>))
                        return method.Invoke(null, castedArgs.ToArray()) as Task<string>;

                    return Task.FromResult(method.Invoke(null, castedArgs.ToArray()) as string);
                });
            }
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { (context, args) => Task[string] } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => Task }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<HttpContext, IDictionary<string, object>, Task<string>> getProxiedAddress, Func<HttpContext, Exception, Task> onFailure = null)
        {
            app.UseRouter(builder => {
                builder.MapMiddlewareRoute(endpoint, proxyApp => {
                    proxyApp.Run(async context => {
                        var uri = await getProxiedAddress(context, context.GetRouteData().Values.ToDictionary(v => v.Key, v => v.Value)).ConfigureAwait(false);
                        await Helpers.HandleProxy(context, uri, onFailure);
                    });
                });
            });
        }

        #region UseProxy Overloads

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { (args) => Task[string] } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => Task }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<IDictionary<string, object>, Task<string>> getProxiedAddress, Func<HttpContext, Exception, Task> onFailure = null)
        {
            Func<HttpContext, IDictionary<string, object>, Task<string>> gpa = (context, args) => getProxiedAddress(args);

            UseProxy(app, endpoint, gpa, onFailure);
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { () => Task[string] } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => Task }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<Task<string>> getProxiedAddress, Func<HttpContext, Exception, Task> onFailure = null)
        {
            Func<HttpContext, IDictionary<string, object>, Task<string>> gpa = (context, args) => getProxiedAddress();

            UseProxy(app, endpoint, gpa, onFailure);
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { (context, args) => string } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => void }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<HttpContext, IDictionary<string, object>, string> getProxiedAddress, Action<HttpContext, Exception> onFailure = null)
        {
            Func<HttpContext, IDictionary<string, object>, Task<string>> gpa = (context, args) => Task.FromResult(getProxiedAddress(context, args));

            Func<HttpContext, Exception, Task> of = null;
            if(onFailure != null)
                of = (context, e) => { onFailure(context, e); return Task.FromResult(0); };

            UseProxy(app, endpoint, gpa, of);
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { (args) => string } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => void }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<IDictionary<string, object>, string> getProxiedAddress, Action<HttpContext, Exception> onFailure = null)
        {
            Func<HttpContext, IDictionary<string, object>, Task<string>> gpa = (context, args) => Task.FromResult(getProxiedAddress(args));

            Func<HttpContext, Exception, Task> of = null;
            if(onFailure != null)
                of = (context, e) => { onFailure(context, e); return Task.FromResult(0); };

            UseProxy(app, endpoint, gpa, of);
        }

        /// <summary>
        /// Middleware which creates an ad hoc proxy over a specified endpoint.
        /// </summary>
        /// <param name="app">The ASP.NET <see cref="IApplicationBuilder"/>.</param>
        /// <param name="endpoint">The local route endpoint.</param>
        /// <param name="getProxiedAddress">A lambda { () => string } which returns the address to which the request is proxied.</param>
        /// <param name="onFailure">A lambda to handle proxy failures { (context, exception) => void }.</param>
        public static void UseProxy(this IApplicationBuilder app, string endpoint, Func<string> getProxiedAddress, Action<HttpContext, Exception> onFailure = null)
        {
            Func<HttpContext, IDictionary<string, object>, Task<string>> gpa = (context, args) => Task.FromResult(getProxiedAddress());

            Func<HttpContext, Exception, Task> of = null;
            if(onFailure != null)
                of = (context, e) => { onFailure(context, e); return Task.FromResult(0); };

            UseProxy(app, endpoint, gpa, of);
        }

        #endregion
    }
}