
using System;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AspNetCore.Proxy.Tests
{
    public class Other
    {
        [Fact]
        public void CanFailOnBadFormContentType()
        {
            var contentType = "text/plain";
            var request = Mock.Of<HttpRequest>();
            request.ContentType = contentType;

            var e = Assert.ThrowsAny<Exception>(() => {
                var dummy = Helpers.ToHttpContent(null, request);
            });

            Assert.Equal($"Unknown form content type `{contentType}`.", e.Message);
        }
    }
}