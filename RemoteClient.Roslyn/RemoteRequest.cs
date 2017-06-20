using System.Collections.Generic;

namespace RemoteClient.Roslyn
{
	public class RemoteRequest : IRemoteRequest
	{
		public IRemoteOperationDescriptor Descriptor { get; }
		public IReadOnlyDictionary<string, object> QueryStringParameters { get; }
		public IReadOnlyDictionary<string, object> BodyParameters { get; }

		public RemoteRequest(IRemoteOperationDescriptor descriptor, IReadOnlyDictionary<string, object> queryStringParameters, IReadOnlyDictionary<string, object> bodyParameters)
		{
			Descriptor = descriptor;
			QueryStringParameters = queryStringParameters;
			BodyParameters = bodyParameters;
		}
	}
}