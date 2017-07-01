using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CodeGeneration.Roslyn;

namespace RemoteClient.Roslyn
{
	[AttributeUsage(AttributeTargets.Interface)]
	[CodeGenerationAttribute(typeof(RemoteClientGenerator))]
	[Conditional("CodeGeneration")]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	public class RemoteClientAttribute : Attribute
	{
		public bool InheritServiceInterface { get; }
		public bool GenerateClientInterface { get; }

		public RemoteClientAttribute(bool inheritServiceInterface = false, bool generateClientInterface = false)
		{
			GenerateClientInterface = generateClientInterface;
			InheritServiceInterface = inheritServiceInterface;
		}
	}
}
