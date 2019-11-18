using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCore.Proxy.Endpoints;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public partial class UnitTests
    {
        [Fact]
        public async Task CanExerciseRoundRobin()
        {
            var testRounds = 17;
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