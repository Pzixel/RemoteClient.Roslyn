using System.Linq;
using System.Threading.Tasks;
using Moq;
using RemoteClient.Roslyn.Attributes;
using RemoteClient.Roslyn.Test.Clients;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        [Fact]
        public async Task Test()
        {
            var mock = new Mock<IRemoteRequestProcessor>();

            IRemoteRequest savedRequest = null;
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
    }



	[RemoteClient(true, true)]
	public interface IFoo
	{
        /// <summary>
        /// Getting string value async
        /// </summary>
        /// <param name="value">Value parameter</param>
        /// <param name="bar">Bar parameter</param>
        /// <returns>Result of method</returns>
		[WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
		Task<string> GetStringAsync(string value, string bar);


	    [WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
	    Task ExecuteStringAsync(string value, string bar);
    }

	[RemoteClient]
	public interface IBar
	{
	    [WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
	    string GetString(string value, string bar);


	    [WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
	    void ExecuteStringAsync(string value, string bar);
    }
}
