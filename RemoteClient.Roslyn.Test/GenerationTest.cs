using System.Linq;
using System.Threading.Tasks;
using Moq;
using RemoteClient.Roslyn.Test.Clients;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        [Fact]
        public async Task TestGet()
        {
            IRemoteRequest savedRequest = null;
            var mock = new Mock<IRemoteRequestProcessor>();
            mock.Setup(processor => processor.GetResultAsync<string>(It.IsAny<IRemoteRequest>()))
                .Callback<IRemoteRequest>(request => savedRequest = request)
                .Returns(Task.FromResult("success"));

			var fooClient = new FooClient(mock.Object);
            string result = await fooClient.GetStringAsync("10", "20");

            Assert.Equal("success", result);
            Assert.NotNull(savedRequest);
            Assert.Equal("GET", savedRequest.Descriptor.Method);
            Assert.Equal("foo/{value}", savedRequest.Descriptor.UriTemplate);
            Assert.Equal(OperationWebMessageFormat.Json, savedRequest.Descriptor.RequestFormat);
            Assert.Equal(OperationWebMessageFormat.Xml, savedRequest.Descriptor.ResponseFormat);
            Assert.Equal(1, savedRequest.QueryStringParameters.Count);
            Assert.Equal(1, savedRequest.BodyParameters.Count);
            var queryParameter = savedRequest.QueryStringParameters.Single();
            var bodyParameter = savedRequest.BodyParameters.Single();
            Assert.Equal("value", queryParameter.Key);
            Assert.Equal("10", queryParameter.Value);
            Assert.Equal("bar", bodyParameter.Key);
            Assert.Equal("20", bodyParameter.Value);
        }

        [Fact]
        public async Task TestExecute()
        {
            IRemoteRequest savedRequest = null;
            var mock = new Mock<IRemoteRequestProcessor>();
            mock.Setup(processor => processor.ExecuteAsync(It.IsAny<IRemoteRequest>()))
                .Callback<IRemoteRequest>(request => savedRequest = request)
                .Returns(Task.CompletedTask);

			var fooClient = new FooClient(mock.Object);
            await fooClient.ExecuteStringAsync("10", "20");

            Assert.NotNull(savedRequest);
            Assert.Equal("GET", savedRequest.Descriptor.Method);
            Assert.Equal("foo/{value}", savedRequest.Descriptor.UriTemplate);
            Assert.Equal(OperationWebMessageFormat.Json, savedRequest.Descriptor.RequestFormat);
            Assert.Equal(OperationWebMessageFormat.Xml, savedRequest.Descriptor.ResponseFormat);
            Assert.Equal(1, savedRequest.QueryStringParameters.Count);
            Assert.Equal(1, savedRequest.BodyParameters.Count);
            var queryParameter = savedRequest.QueryStringParameters.Single();
            var bodyParameter = savedRequest.BodyParameters.Single();
            Assert.Equal("value", queryParameter.Key);
            Assert.Equal("10", queryParameter.Value);
            Assert.Equal("bar", bodyParameter.Key);
            Assert.Equal("20", bodyParameter.Value);
        }
    }
}
