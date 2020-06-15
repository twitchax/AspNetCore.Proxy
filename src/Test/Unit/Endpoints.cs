using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Proxy.Endpoints;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class EndpointTests
    {
        [Fact]
        public void CanExerciseRoundRobin()
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

        [Fact]
        public void CanExerciseRandomRobin()
        {
            var servers = Enumerable.Range(1, 100).Select(i => i.ToString()).ToList();
            var roundRobin = RandomRobin.Of(servers.ToArray());

            // Draw 100.
            var firstDraw = Enumerable.Range(1, 100).Select(i => roundRobin(null, null)).ToList();

            // Draw another 100.
            var secondDraw = Enumerable.Range(1, 100).Select(i => roundRobin(null, null)).ToList();

            // Something like 1 in 100! (ish, since I am not reseeding, and since it is not an even distribution) chance this assert fails.
            Assert.NotEqual(firstDraw, secondDraw);
        }
    }
}