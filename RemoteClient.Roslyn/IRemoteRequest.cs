using System.Collections.Generic;

namespace RemoteClient.Roslyn
{
    public interface IRemoteRequest
    {
        RemoteOperationDescriptor Descriptor { get; }
        IReadOnlyDictionary<string, object> QueryStringParameters { get; }
        IReadOnlyDictionary<string, object> BodyParameters { get; }
    }
}
