using System.Threading.Tasks;
using RemoteClient.Roslyn.Attributes;
using RemoteClient.Roslyn.Test.Clients;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        public void Test()
        {
			var fooClient = new FooClient(null);
            fooClient.GetStringAsync(null, null);
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
