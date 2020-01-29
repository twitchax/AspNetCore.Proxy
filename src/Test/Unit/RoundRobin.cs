using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Endpoints;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class RoundRobinTests
    {
        [Fact]
        public async Task CanExerciseRoundRobin()
        {
            const int testRounds = 17;
            var servers = new List<string> { "1", "2", "3", "4", "5" };
            var roundRobin = RoundRobin.Of(servers.ToArray());

            for(int k = 0; k < testRounds; k++)
            {
                var result = roundRobin(null, null);
                Assert.Equal(servers[k % servers.Count], result);
            }
        }
    }
}