using System.Threading.Tasks;
using RemoteClient.Roslyn.Attributes;
using RemoteClient.Roslyn.Test.Clients;
using Xunit;

namespace RemoteClient.Roslyn.Test
{
    public class GenerationTest
    {
        public void Test1()
        {
			var fooCLient = new FooClient(null);
        }
    }

	[RemoteClient(true)]
	public interface IFoo
	{
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
    };
}
