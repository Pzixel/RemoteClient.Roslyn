using System.Threading.Tasks;
using RemoteClient.Roslyn.Attributes;

namespace RemoteClient.Roslyn.Test
{
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
}