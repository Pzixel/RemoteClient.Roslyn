namespace RemoteClient.Roslyn
{
    public class RemoteOperationDescriptor : IRemoteOperationDescriptor
	{
	    public string Method { get; }
	    public string UriTemplate { get; }
	    public OperationWebMessageFormat RequestFormat { get; }
        public OperationWebMessageFormat ResponseFormat { get; }

        public RemoteOperationDescriptor(string method, string uriTemplate, OperationWebMessageFormat requestFormat, OperationWebMessageFormat responseFormat)
        {
            Method = method;
            UriTemplate = uriTemplate;
            RequestFormat = requestFormat;
            ResponseFormat = responseFormat;
        }
    }
}