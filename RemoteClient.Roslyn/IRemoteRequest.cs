using System.Collections.Generic;

namespace RemoteClient.Roslyn
{
    public interface IRemoteRequest
    {
        IRemoteOperationDescriptor Descriptor { get; }
        IReadOnlyDictionary<string, object> QueryStringParameters { get; }
        IReadOnlyDictionary<string, object> BodyParameters { get; }
    }
}
