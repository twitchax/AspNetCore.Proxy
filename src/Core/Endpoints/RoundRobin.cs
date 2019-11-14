namespace AspNetCore.Proxy.Endpoints
{
    public static class RoundRobin
    {
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