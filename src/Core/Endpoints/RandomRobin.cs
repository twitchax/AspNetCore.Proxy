using System;

namespace AspNetCore.Proxy.Endpoints
{
    /// <summary>
    /// Random round robin endpoint helpers.
    /// </summary>
    public static class RandomRobin
    {
        /// <summary>
        /// A helper method that "round robins" over a set of endpoints randomly.
        /// </summary>
        /// <param name="endpoints">The set of endpoints to randomly "round robin" over.</param>
        /// <returns>An <see cref="EndpointComputerToString"/> that "round robins" over the provided endpoints randomly.</returns>
        public static EndpointComputerToString Of(params string[] endpoints)
        {
            var rand = new Random();
            return (context, args) => endpoints[rand.Next(endpoints.Length)];
        }
    }
}