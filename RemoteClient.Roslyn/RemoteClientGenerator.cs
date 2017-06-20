using System;
using System.Collections.Generic;
using System.Linq;
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
		private const string QueryStringParamters = "queryStringParamters";
		private const string ProcessorName = "processor";

		private readonly bool _inheritInterface;

		public RemoteClientGenerator(AttributeData attributeData)
		{
			Requires.NotNull(attributeData, nameof(attributeData));
			_inheritInterface = (bool) attributeData.ConstructorArguments[0].Value;
		}

		public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, CSharpCompilation compilation, IProgress<Diagnostic> progress,
			CancellationToken cancellationToken)
		{
			var applyToInterface = (InterfaceDeclarationSyntax) applyTo;

			var clientIdentifier = Identifier(TrimInterfaceFirstLetter(applyToInterface.Identifier.ValueText) + "Client");

			var processorField = FieldDeclaration(VariableDeclaration(ParseTypeName(nameof(IRemoteRequestProcessor)))
					.AddVariables(VariableDeclarator(ProcessorName)))
				.AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));

			var processorFieldExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(ProcessorName));

			var processorCtorParameter = Parameter(
				List<AttributeListSyntax>(),
				TokenList(),
				ParseTypeName(nameof(IRemoteRequestProcessor)),
				Identifier(ProcessorName),
				null);

			var ctor = ConstructorDeclaration(clientIdentifier)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(processorCtorParameter)
				.AddBodyStatements(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, processorFieldExpression, IdentifierName(ProcessorName))));


			var memberAccessExpressionSyntax = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, processorFieldExpression, IdentifierName(nameof(IDisposable.Dispose)));
			var invocationExpressionSyntax = InvocationExpression(memberAccessExpressionSyntax);
			var disposeMethod = MethodDeclaration(ParseTypeName("void"), nameof(IDisposable.Dispose))
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddBodyStatements(ExpressionStatement(invocationExpressionSyntax));

			var clientClass = ClassDeclaration(clientIdentifier)
				.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
				.AddBaseListTypes(SimpleBaseType(ParseTypeName(nameof(IDisposable))))
				.AddMembers(processorField, ctor, disposeMethod);

			
			var implementedMembers = new List<MemberDeclarationSyntax>();
			foreach (var interfaceMethod in applyToInterface.Members.OfType<MethodDeclarationSyntax>())
			{
				if (true)
				{
					implementedMembers.Add(GetMethodImplementation(interfaceMethod));
				}
				else
				{
					throw new Exception("Interface contains methods with non-task return type");
				}
			}

			clientClass = clientClass.AddMembers(implementedMembers.ToArray());

			if (_inheritInterface)
			{
				clientClass = clientClass.AddBaseListTypes(SimpleBaseType(ParseTypeName(applyToInterface.Identifier.ValueText)));
			}

			var clientsNamespace = NamespaceDeclaration(ParseName("Clients"))
				.AddUsings(
					GetUsingDirectiveSyntax("System"),
					GetUsingDirectiveSyntax("System.Collections.Generic"),
					GetUsingDirectiveSyntax("System.Threading.Tasks"))
				.AddMembers(clientClass);

			return Task.FromResult(List<MemberDeclarationSyntax>().Add(clientsNamespace));
		}

		private static MethodDeclarationSyntax GetMethodImplementation(MethodDeclarationSyntax interfaceMethod)
		{
			var dictionaryName = ParseTypeName("Dictionary<string, object>");
			var queryStringVariable = VariableDeclarator(QueryStringParamters);
			var queryStringDict = LocalDeclarationStatement(VariableDeclaration(dictionaryName)
				.AddVariables(queryStringVariable.WithInitializer(EqualsValueClause(InvocationExpression(ObjectCreationExpression(dictionaryName))))));

			var list = new List<StatementSyntax> {queryStringDict};
			foreach (ParameterSyntax parameter in interfaceMethod.ParameterList.ChildNodes())
			{
				string dictToAdd = QueryStringParamters;

				var key = Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(parameter.Identifier.ValueText)));
				var value = Argument(IdentifierName(parameter.Identifier));

				var dictionaryAddMember = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(dictToAdd), IdentifierName(nameof(Dictionary<string, object>.Add)));

				var dictionaryAddCall =
					ExpressionStatement(
						InvocationExpression(dictionaryAddMember,
							ArgumentList(SeparatedList(new[] { key, value }))));

				list.Add(dictionaryAddCall);
			}

			
			list.AddRange(GetInvocationCode(queryStringVariable.Identifier, interfaceMethod.ReturnType));

			return MethodDeclaration(interfaceMethod.ReturnType, interfaceMethod.Identifier)
				.WithParameterList(interfaceMethod.ParameterList)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddBodyStatements(list.ToArray());
		}

		private static IEnumerable<StatementSyntax> GetInvocationCode(SyntaxToken queryStringDictToken, TypeSyntax interfaceMethodReturnType)
		{
			const string descriptor = "descriptor";
			var remoteOperationDescriptorArguments = ArgumentList(SeparatedList(
				new[]
				{
					Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("1"))),
					Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("2"))),
					Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseTypeName(nameof(OperationWebMessageFormat)), IdentifierName("Xml"))),
					Argument(CastExpression(ParseTypeName(nameof(OperationWebMessageFormat)), LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
				}
			));

			var descriptorName = ParseTypeName(nameof(RemoteOperationDescriptor));
			var ctor = EqualsValueClause(InvocationExpression(ObjectCreationExpression(descriptorName), remoteOperationDescriptorArguments));

			var variableDeclaratorSyntax = VariableDeclarator(descriptor);
			var localDeclarationStatement = LocalDeclarationStatement(VariableDeclaration(descriptorName)
				.AddVariables(variableDeclaratorSyntax.WithInitializer(ctor)));

			yield return localDeclarationStatement;


			const string request = "request";
			var remoteRequestArguments = ArgumentList(SeparatedList(
				new[]
				{
					Argument(IdentifierName(variableDeclaratorSyntax.Identifier)),
					Argument(IdentifierName(queryStringDictToken)),
					Argument(IdentifierName(queryStringDictToken))
				}
			));
			
			var requestName = ParseTypeName(nameof(RemoteRequest));

			var requestCtor = EqualsValueClause(InvocationExpression(ObjectCreationExpression(requestName), remoteRequestArguments));

			var variableDeclaratorSyntax2 = VariableDeclarator(request);
			var localDeclarationStatement2 = LocalDeclarationStatement(VariableDeclaration(requestName)
				.AddVariables(variableDeclaratorSyntax2.WithInitializer(requestCtor)));

			yield return localDeclarationStatement2;
			
			var withTypeArgumentList = GetCallingMethod(interfaceMethodReturnType);
			var returnExpression = ReturnStatement(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				IdentifierName(ProcessorName), withTypeArgumentList),
				ArgumentList(SeparatedList(
					new[]
					{
						Argument(IdentifierName(request)),
					}
				))));

			yield return returnExpression;
		}

		private static SimpleNameSyntax GetCallingMethod(TypeSyntax interfaceMethodReturnType)
		{
			switch (interfaceMethodReturnType)
			{
				case GenericNameSyntax genericName:
					return GenericName(nameof(IRemoteRequestProcessor.GetResultAsync)).WithTypeArgumentList(genericName.TypeArgumentList);
				case IdentifierNameSyntax _:
					return IdentifierName(nameof(IRemoteRequestProcessor.ExecuteAsync));
				default:
					throw new InvalidOperationException("Never throws");
			}
		}

		private static UsingDirectiveSyntax GetUsingDirectiveSyntax(string namespaceName) => UsingDirective(
			ParseName(namespaceName)).NormalizeWhitespace();

		private static string TrimInterfaceFirstLetter(string interfaceName) => interfaceName[0] == 'I' ? interfaceName.Substring(1) : interfaceName;
	}
}
