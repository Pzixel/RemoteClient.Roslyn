using System;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Validation;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RemoteClient.Roslyn
{
    public class RemoteClientGenerator : ICodeGenerator
	{
		public RemoteClientGenerator(AttributeData attributeData)
		{
			Requires.NotNull(attributeData, nameof(attributeData));
		}

		public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, CSharpCompilation compilation, IProgress<Diagnostic> progress,
			CancellationToken cancellationToken)
		{
			var applyToInterface = (InterfaceDeclarationSyntax) applyTo;

			var disposeMethod = MethodDeclaration(ParseTypeName("void"), nameof(IDisposable.Dispose))
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.WithBody(Block());

			var clientIdentifier = Identifier(applyToInterface.Identifier.ValueText + "Client");

			const string ctorParameterName = "processor";
			var proxyCtor = ConstructorDeclaration(clientIdentifier)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(Parameter(
					List<AttributeListSyntax>(),
					TokenList(),
					ParseTypeName(nameof(IAsyncRequestProcessor)),
					Identifier(ctorParameterName),
					null))
				.WithInitializer(ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
					// could be BaseConstructorInitializer or ThisConstructorInitializer
					.AddArgumentListArguments(
						Argument(IdentifierName(ctorParameterName))
					))
				.WithBody(Block());

			var clientClass = ClassDeclaration(clientIdentifier)
				.WithModifiers(SyntaxTokenList.Create(Token(SyntaxKind.PublicKeyword)))
				.AddBaseListTypes(SimpleBaseType(ParseTypeName(nameof(AsyncClientBase))),
					SimpleBaseType(ParseTypeName(applyToInterface.Identifier.ValueText)),
					SimpleBaseType(ParseTypeName(nameof(IDisposable))))
				.AddMembers(proxyCtor, disposeMethod);

			return Task.FromResult(List<MemberDeclarationSyntax>().Add(clientClass));
		}
	}
}
