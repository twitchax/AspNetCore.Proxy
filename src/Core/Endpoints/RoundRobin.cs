namespace AspNetCore.Proxy.Endpoints
{
    /// <summary>
    /// Round robin endpoint helpers.
    /// </summary>
    public static class RoundRobin
    {
        /// <summary>
        /// A helper method that "round robins" over a set of endpoints.
        /// </summary>
        /// <param name="endpoints">The set of endpoints to "round robin" over.</param>
        /// <returns>An <see cref="EndpointComputerToString"/> that "round robins" over the provided endpoints.</returns>
        public static EndpointComputerToString Of(params string[] endpoints)
        {
            var position = 0;

            return (context, args) =>
            {
                if(position == endpoints.Length)
                    position = 0;

                return endpoints[position++];
            };
        }
    }
}