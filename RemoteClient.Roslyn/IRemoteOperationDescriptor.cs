namespace RemoteClient.Roslyn
{
	public interface IRemoteOperationDescriptor
	{
		string UriTemplate { get; }
		string Method { get; }
		OperationWebMessageFormat RequestFormat { get; }
		OperationWebMessageFormat ResponseFormat { get; }
	}
}