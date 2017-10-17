using System;

namespace AspNetCore.Proxy
{
    /// <summary>
    ///  This attribute indicates a static method which returns a URI to which a request will be proxied.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ProxyRouteAttribute : Attribute
    {
        /// <summary>
        /// The local route which will be proxied.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// This attribute indicates a static method which returns a URI to which a request will be proxied.
        /// </summary>
        /// <param name="route">The local route which will be proxied.</param>  
        public ProxyRouteAttribute(string route)
        {
            Route = route;
        }
    }
}
