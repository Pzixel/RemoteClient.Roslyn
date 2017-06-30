using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RemoteClient.Roslyn.Attributes;
using Validation;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RemoteClient.Roslyn
{
    public class RemoteClientGenerator : ICodeGenerator
	{
		private const string QueryStringParamters = "queryStringParamters";
		private const string BodyParamters = "bodyParamters";
		private const string ProcessorName = "processor";

		private readonly bool _inheritInterface;

		public RemoteClientGenerator(AttributeData attributeData)
		{
			Requires.NotNull(attributeData, nameof(attributeData));
			_inheritInterface = (bool) attributeData.ConstructorArguments[0].Value;
		}

		public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
		{
		    var applyToInterface = (InterfaceDeclarationSyntax) context.ProcessingMember;

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
			    var returnType = GetReturnType(context.SemanticModel, interfaceMethod.ReturnType);
			    implementedMembers.Add(GetMethodImplementation(interfaceMethod, context.SemanticModel, returnType));
			}

			clientClass = clientClass.AddMembers(implementedMembers.ToArray());

			if (_inheritInterface)
			{
				clientClass = clientClass.AddBaseListTypes(SimpleBaseType(ParseTypeName(applyToInterface.Identifier.ValueText)));
			}

			var clientsNamespace = NamespaceDeclaration(ParseName("Clients"))
				.AddUsings(
					GetUsingDirectiveSyntax("System"),
					GetUsingDirectiveSyntax("System.Collections.Immutable"),
					GetUsingDirectiveSyntax("System.Threading.Tasks"))
				.AddMembers(clientClass);

			return Task.FromResult(List<MemberDeclarationSyntax>().Add(clientsNamespace));
		}

		private static MethodDeclarationSyntax GetMethodImplementation(MethodDeclarationSyntax interfaceMethod, SemanticModel semanticModel, TypeSyntax returnType)
		{
		    var propertySymbol = semanticModel.GetDeclaredSymbol(interfaceMethod);
		    var attribute = propertySymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == nameof(WebInvokeAttribute));
            
            if (attribute == null)
                throw new InvalidOperationException($"Cannot proceed class generation due to missing {nameof(WebInvokeAttribute)}");

		    var attributeDictionary = attribute.NamedArguments.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.Value);

			var queryStringVariable = VariableDeclarator(QueryStringParamters);
			var bodyVariable = VariableDeclarator(BodyParamters);
		    var initializer = EqualsValueClause(InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("ImmutableDictionary"), GenericName(nameof(ImmutableDictionary.CreateBuilder))
                .WithTypeArgumentList(TypeArgumentList(SeparatedList(
					new[]
					{
						ParseTypeName("string"),
					    ParseTypeName("object")
                    }
				))))));
		    var queryStringDict = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
				.AddVariables(queryStringVariable.WithInitializer(initializer)));
			var bodyDict = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
				.AddVariables(bodyVariable.WithInitializer(initializer)));

		    string uriTemplate = attributeDictionary[nameof(WebInvokeAttribute.UriTemplate)].ToString();
            var list = new List<StatementSyntax> {queryStringDict, bodyDict};
			foreach (ParameterSyntax parameter in interfaceMethod.ParameterList.ChildNodes())
			{
				string dictToAdd = uriTemplate.Contains("{" + parameter.Identifier.ValueText + "}") ? QueryStringParamters : BodyParamters;
                
				var key = Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(parameter.Identifier.ValueText)));
				var value = Argument(IdentifierName(parameter.Identifier));

				var dictionaryAddMember = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(dictToAdd), IdentifierName(nameof(Dictionary<string, object>.Add)));

				var dictionaryAddCall =
					ExpressionStatement(
						InvocationExpression(dictionaryAddMember,
							ArgumentList(SeparatedList(new[] { key, value }))));

				list.Add(dictionaryAddCall);
			}
            
			list.AddRange(GetInvocationCode(queryStringVariable.Identifier, bodyVariable.Identifier, returnType, attributeDictionary));

			return MethodDeclaration(returnType, interfaceMethod.Identifier)
				.WithParameterList(interfaceMethod.ParameterList)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddBodyStatements(list.ToArray());
		}

		private static IEnumerable<StatementSyntax> GetInvocationCode(SyntaxToken queryStringDictToken, SyntaxToken bodyDictToken, TypeSyntax interfaceMethodReturnType, ImmutableDictionary<string, object> attributeData)
		{
			const string descriptor = "descriptor";

		    var remoteOperationDescriptorArguments = ArgumentList(SeparatedList(
		        new[]
		        {
		            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(attributeData[nameof(WebInvokeAttribute.Method)].ToString()))),
		            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(attributeData[nameof(WebInvokeAttribute.UriTemplate)].ToString()))),
		            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseTypeName(nameof(OperationWebMessageFormat)), IdentifierName(Enum.GetName(typeof(OperationWebMessageFormat), attributeData[nameof(WebInvokeAttribute.RequestFormat)])))),
		            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseTypeName(nameof(OperationWebMessageFormat)), IdentifierName(Enum.GetName(typeof(OperationWebMessageFormat), attributeData[nameof(WebInvokeAttribute.ResponseFormat)])))),
                }
		    ));

			var descriptorName = ParseTypeName(nameof(RemoteOperationDescriptor));
			var ctor = EqualsValueClause(InvocationExpression(ObjectCreationExpression(descriptorName), remoteOperationDescriptorArguments));

			var variableDeclaratorSyntax = VariableDeclarator(descriptor);
			var localDeclarationStatement = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
				.AddVariables(variableDeclaratorSyntax.WithInitializer(ctor)));

			yield return localDeclarationStatement;


			const string request = "request";
			var remoteRequestArguments = ArgumentList(SeparatedList(
				new[]
				{
					Argument(IdentifierName(variableDeclaratorSyntax.Identifier)),
					Argument(GetCallingExpression(queryStringDictToken, nameof(ImmutableDictionary<string, object>.Builder.ToImmutable))),
					Argument(GetCallingExpression(bodyDictToken, nameof(ImmutableDictionary<string, object>.Builder.ToImmutable)))
                }
			));
			
			var requestName = ParseTypeName(nameof(RemoteRequest));

			var requestCtor = EqualsValueClause(InvocationExpression(ObjectCreationExpression(requestName), remoteRequestArguments));

			var variableDeclaratorSyntax2 = VariableDeclarator(request);
			var localDeclarationStatement2 = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
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

	    private static ExpressionSyntax GetCallingExpression(SyntaxToken queryStringDictToken, string methodName) => 
            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(queryStringDictToken), IdentifierName(methodName)));

	    private static SimpleNameSyntax GetCallingMethod(TypeSyntax interfaceMethodReturnType)
		{
			switch (interfaceMethodReturnType)
			{
				case GenericNameSyntax genericName:
					return GenericName(nameof(IRemoteRequestProcessor.GetResultAsync)).WithTypeArgumentList(genericName.TypeArgumentList);
			    case QualifiedNameSyntax _:
                case IdentifierNameSyntax _:
					return IdentifierName(nameof(IRemoteRequestProcessor.ExecuteAsync));
				default:
					throw new InvalidOperationException($"Unknown return type: {interfaceMethodReturnType} ({interfaceMethodReturnType.GetType()})");
			}
		}

		private static UsingDirectiveSyntax GetUsingDirectiveSyntax(string namespaceName) => UsingDirective(
			ParseName(namespaceName)).NormalizeWhitespace();

		private static string TrimInterfaceFirstLetter(string interfaceName) => interfaceName[0] == 'I' ? interfaceName.Substring(1) : interfaceName;

	    private TypeSyntax GetReturnType(SemanticModel semanticModel, TypeSyntax returnType)
	    {
	        var returnTypeSymbol = (INamedTypeSymbol) semanticModel.GetSymbolInfo(returnType).Symbol;
            if (IsTask(returnTypeSymbol, semanticModel))
            {
                return returnType;
            }
            if (false)
	            throw new Exception("Interface contains methods with non-task return type");
	        return TypeSymbolMatchesType(returnTypeSymbol, typeof(void), semanticModel) ? ParseTypeName(nameof(Task)) : GenericName(Identifier("Task"), TypeArgumentList(SeparatedList(new[]{ returnType })));
	    }

	    private static bool IsTask(INamedTypeSymbol typeSymbol, SemanticModel semanticModel) => TypeSymbolMatchesType(typeSymbol, typeof(Task), semanticModel) || TypeSymbolMatchesType(typeSymbol.ConstructedFrom, typeof(Task<>), semanticModel);

	    private static bool TypeSymbolMatchesType(ISymbol typeSymbol, Type type, SemanticModel semanticModel) => GetTypeSymbolForType(type, semanticModel).Equals(typeSymbol);

	    private static INamedTypeSymbol GetTypeSymbolForType(Type type, SemanticModel semanticModel)
	    {
	        if (!type.IsConstructedGenericType)
	        {
	            return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
	        }

	        // get all typeInfo's for the Type arguments 
	        var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => GetTypeSymbolForType(a, semanticModel));

	        var openType = type.GetGenericTypeDefinition();
	        var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);
	        return typeSymbol.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
	    }
    }
}
