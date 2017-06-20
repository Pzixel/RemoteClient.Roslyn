using System;
using System.Diagnostics;
using CodeGeneration.Roslyn;

namespace RemoteClient.Roslyn
{
	[AttributeUsage(AttributeTargets.Interface)]
	[CodeGenerationAttribute(typeof(RemoteClientGenerator))]
	[Conditional("CodeGeneration")]
	public class RemoteClientAttribute : Attribute
	{
		public bool InheritInterface { get; }

		public RemoteClientAttribute(bool inheritInterface = false)
		{
			InheritInterface = inheritInterface;
		}
	}
}
