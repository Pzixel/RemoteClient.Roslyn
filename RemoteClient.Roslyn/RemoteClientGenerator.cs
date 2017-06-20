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


			var clientIdentifier = Identifier(applyToInterface.Identifier.ValueText + "Client");

			const string processorName = "processor";
			var processorField = FieldDeclaration(VariableDeclaration(ParseTypeName(nameof(IAsyncRequestProcessor)))
					.AddVariables(VariableDeclarator(processorName)))
				.AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));

			var processorFieldExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(processorName));

			var processorCtorParameter = Parameter(
				List<AttributeListSyntax>(),
				TokenList(),
				ParseTypeName(nameof(IAsyncRequestProcessor)),
				Identifier(processorName),
				null);

			var ctor = ConstructorDeclaration(clientIdentifier)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(processorCtorParameter)
				.WithBody(Block(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, processorFieldExpression, IdentifierName(processorName)))));


			var memberAccessExpressionSyntax = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, processorFieldExpression, IdentifierName(nameof(IDisposable.Dispose)));
			var invocationExpressionSyntax = InvocationExpression(memberAccessExpressionSyntax);
			var disposeMethod = MethodDeclaration(ParseTypeName("void"), nameof(IDisposable.Dispose))
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.WithBody(Block(ExpressionStatement(invocationExpressionSyntax)));

			var clientClass = ClassDeclaration(clientIdentifier)
				.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
				.AddBaseListTypes(SimpleBaseType(ParseTypeName(applyToInterface.Identifier.ValueText)),
					SimpleBaseType(ParseTypeName(nameof(IDisposable))))
				.AddMembers(processorField, ctor, disposeMethod);

			return Task.FromResult(List<MemberDeclarationSyntax>().Add(clientClass));
		}
	}
}
