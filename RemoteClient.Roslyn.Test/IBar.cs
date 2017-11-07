using RemoteClient.Roslyn.Attributes;

namespace RemoteClient.Roslyn.Test
{
    [RemoteClient]
    public interface IBar
    {
        [WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
        string GetString(string value, string bar);


        [WebInvoke(Method = "GET", UriTemplate = "foo/{value}", RequestFormat = OperationWebMessageFormat.Json, ResponseFormat = OperationWebMessageFormat.Xml)]
        void ExecuteStringAsync(string value, string bar);
    }
}