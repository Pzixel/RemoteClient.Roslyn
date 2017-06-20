namespace RemoteClient.Roslyn
{
    public struct RemoteOperationDescriptor
    {
        public string UriTemplate { get; }
        public string Method { get; }
        public OperationWebMessageFormat RequestFormat { get; }
        public OperationWebMessageFormat ResponseFormat { get; }

        public RemoteOperationDescriptor(string uriTemplate, string method, OperationWebMessageFormat requestFormat, OperationWebMessageFormat responseFormat)
        {
            UriTemplate = uriTemplate;
            Method = method;
            RequestFormat = requestFormat;
            ResponseFormat = responseFormat;
        }
    }
}