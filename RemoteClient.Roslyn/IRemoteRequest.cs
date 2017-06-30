using System.Collections.Immutable;

namespace RemoteClient.Roslyn
{
    public interface IRemoteRequest
    {
        IRemoteOperationDescriptor Descriptor { get; }
        IImmutableDictionary<string, object> QueryStringParameters { get; }
        IImmutableDictionary<string, object> BodyParameters { get; }
    }
}
