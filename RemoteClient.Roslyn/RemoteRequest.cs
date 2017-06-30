using System.Collections.Immutable;

namespace RemoteClient.Roslyn
{
	public class RemoteRequest : IRemoteRequest
	{
		public IRemoteOperationDescriptor Descriptor { get; }
		public IImmutableDictionary<string, object> QueryStringParameters { get; }
		public IImmutableDictionary<string, object> BodyParameters { get; }

		public RemoteRequest(IRemoteOperationDescriptor descriptor, IImmutableDictionary<string, object> queryStringParameters, IImmutableDictionary<string, object> bodyParameters)
		{
			Descriptor = descriptor;
			QueryStringParameters = queryStringParameters;
			BodyParameters = bodyParameters;
		}
	}
}