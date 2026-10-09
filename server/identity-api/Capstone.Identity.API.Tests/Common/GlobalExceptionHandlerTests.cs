using Capstone.Identity.API.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Capstone.Identity.API.Tests.Common
{
    [TestClass]
    public class GlobalExceptionHandlerTests
    {
        private const string TraceId = "test-trace-id";
        private const string InternalMessage = "secret internal detail";

        private static async Task<(DefaultHttpContext Context, bool Handled, string Body)> RunHandlerAsync()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.TraceIdentifier = TraceId;

            var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

            var handled = await handler.TryHandleAsync(
                context, new InvalidOperationException(InternalMessage), CancellationToken.None);

            context.Response.Body.Position = 0;
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

            return (context, handled, body);
        }

        [TestMethod]
        public async Task TryHandleAsync_ReturnsTrue()
        {
            var (_, handled, _) = await RunHandlerAsync();

            Assert.IsTrue(handled);
        }

        [TestMethod]
        public async Task TryHandleAsync_Sets500AndProblemJsonContentType()
        {
            var (context, _, _) = await RunHandlerAsync();

            Assert.AreEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            StringAssert.StartsWith(context.Response.ContentType, "application/problem+json");
        }

        [TestMethod]
        public async Task TryHandleAsync_BodyIncludesStatusAndTraceId()
        {
            var (_, _, body) = await RunHandlerAsync();

            using var json = JsonDocument.Parse(body);
            Assert.AreEqual(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.AreEqual(TraceId, json.RootElement.GetProperty("traceId").GetString());
        }

        [TestMethod]
        public async Task TryHandleAsync_DoesNotLeakExceptionMessage()
        {
            var (_, _, body) = await RunHandlerAsync();

            Assert.IsFalse(body.Contains(InternalMessage),
                "The response body must not contain the exception's message.");
        }
    }
}
