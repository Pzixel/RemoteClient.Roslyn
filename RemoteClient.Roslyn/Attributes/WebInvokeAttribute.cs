using System;

namespace RemoteClient.Roslyn.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
	public class WebInvokeAttribute : Attribute
	{
		public string Method { get; set; }
		public string UriTemplate { get; set; }
		public OperationWebMessageFormat RequestFormat { get; set; }
		public OperationWebMessageFormat ResponseFormat { get; set; }
	}
}
