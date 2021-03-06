﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RemoteClient.Roslyn.Attributes;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RemoteClient.Roslyn
{
    public class RemoteClientGenerator : ICodeGenerator
    {
        private const string QueryStringParamters = "queryStringParamters";
        private const string BodyParamters = "bodyParamters";
        private const string ProcessorName = "processor";

        private readonly bool _inheritServiceInterface;
        private readonly bool _generateClientInterface;

        public RemoteClientGenerator(AttributeData attributeData)
        {
            _inheritServiceInterface = (bool)attributeData.ConstructorArguments[0].Value;
            _generateClientInterface = (bool)attributeData.ConstructorArguments[1].Value;
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(TransformationContext context, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            try
            {
                return GenerateAsyncInternal(context);
            }
            catch (Exception ex)
            {
                progress?.Report(Diagnostic.Create("RC001", "RemoteClient", ex.Message, DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
                throw;
            }
        }

        private Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsyncInternal(TransformationContext context)
        {
            var applyToInterface = (InterfaceDeclarationSyntax) context.ProcessingNode;

            var clientIdentifier = Identifier(TrimInterfaceFirstLetter(applyToInterface.Identifier.ValueText) + "Client");

            var processorField = FieldDeclaration(VariableDeclaration(ParseTypeName(nameof(IRemoteRequestProcessor)))
                    .AddVariables(VariableDeclarator(ProcessorName)))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));

            var processorCtorParameter = Parameter(
                List<AttributeListSyntax>(),
                TokenList(),
                ParseTypeName(nameof(IRemoteRequestProcessor)),
                Identifier(ProcessorName),
                null);

            var processorFieldExpression = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(ProcessorName));
            var assigmentPart = BinaryExpression(
                SyntaxKind.CoalesceExpression,
                IdentifierName(ProcessorName),
                ThrowExpression(
                    ObjectCreationExpression(
                            IdentifierName(nameof(ArgumentNullException)))
                        .AddArgumentListArguments(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(ProcessorName))))));

            var ctor = ConstructorDeclaration(clientIdentifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(processorCtorParameter)
                .AddBodyStatements(ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, processorFieldExpression, assigmentPart)));


            var memberAccessExpressionSyntax = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName(ProcessorName), IdentifierName(nameof(IDisposable.Dispose)));
            var invocationExpressionSyntax = InvocationExpression(memberAccessExpressionSyntax);
            var disposeMethod = MethodDeclaration(ParseTypeName("void"), nameof(IDisposable.Dispose))
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .AddParameterListParameters(Parameter(
                    List<AttributeListSyntax>(),
                    TokenList(),
                    ParseTypeName("bool"),
                    Identifier("disposing"),
                    null))
                .AddBodyStatements(ExpressionStatement(invocationExpressionSyntax));

            var clientClass = ClassDeclaration(clientIdentifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(nameof(DisposableBase))), SimpleBaseType(ParseTypeName(nameof(IDisposable))))
                .AddMembers(processorField, ctor, disposeMethod);

            var inheritDocTrivia = !_inheritServiceInterface ? default(SyntaxTrivia?) : Trivia(
                DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    List(
                        new XmlNodeSyntax[]
                        {
                            XmlText()
                                .WithTextTokens(
                                    TokenList(
                                        XmlTextLiteral(
                                            TriviaList(
                                                DocumentationCommentExterior("///")),
                                            " ",
                                            " ",
                                            TriviaList()))),
                            XmlEmptyElement(XmlName(Identifier("inheritdoc")))
                                .WithAttributes(
                                    SingletonList<XmlAttributeSyntax>(
                                        XmlCrefAttribute(
                                            NameMemberCref(
                                                IdentifierName(applyToInterface.Identifier.ValueText))))),
                            XmlText()
                                .WithTextTokens(
                                    TokenList(
                                        XmlTextNewLine(
                                            TriviaList(),
                                            Environment.NewLine,
                                            Environment.NewLine,
                                            TriviaList())))
                        })));

            var implementedMembers = (from interfaceMethod in applyToInterface.Members.OfType<MethodDeclarationSyntax>()
                let returnType = GetReturnType(interfaceMethod.ReturnType, context.SemanticModel)
                let methodImplementation = GetMethodImplementation(interfaceMethod, context.SemanticModel, returnType, inheritDocTrivia)
                select (MemberDeclarationSyntax) methodImplementation).ToArray();

            clientClass = clientClass.AddMembers(implementedMembers);

            var clientsNamespace = NamespaceDeclaration(ParseName("Clients"))
                .AddUsings(
                    GetUsingDirectiveSyntax("System"),
                    GetUsingDirectiveSyntax("System.Collections.Immutable"),
                    GetUsingDirectiveSyntax("System.Threading.Tasks"));

            if (_inheritServiceInterface)
            {
                clientClass = clientClass.AddBaseListTypes(SimpleBaseType(ParseTypeName(applyToInterface.Identifier.ValueText)));
            }

            if (_generateClientInterface)
            {
                string interfaceIdentifier = "I" + clientClass.Identifier.ValueText;
                var methods = clientClass.Members.OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.ValueText != nameof(IDisposable.Dispose)).Select(m =>
                        (MemberDeclarationSyntax) MethodDeclaration(m.ReturnType, m.Identifier)
                            .WithSemicolonToken(ParseToken(";"))
                            .WithParameterList(m.ParameterList)).ToArray();
                var clientInterface = InterfaceDeclaration(interfaceIdentifier)
                    .AddBaseListTypes(SimpleBaseType(ParseTypeName(nameof(IDisposable))))
                    .AddMembers(
                        methods);

                clientClass = clientClass.AddBaseListTypes(SimpleBaseType(IdentifierName(interfaceIdentifier)));

                clientsNamespace = clientsNamespace.AddMembers(clientInterface);
            }

            return Task.FromResult(List<MemberDeclarationSyntax>().Add(clientsNamespace.AddMembers(clientClass)));
        }

        [SuppressMessage("ReSharper", "PossibleInvalidCastExceptionInForeachLoop")]
        private static MethodDeclarationSyntax GetMethodImplementation(MethodDeclarationSyntax interfaceMethod, SemanticModel semanticModel, TypeSyntax returnType, SyntaxTrivia? inheritDocTrivia)
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(interfaceMethod);
            var attribute = propertySymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass.Name == nameof(WebInvokeAttribute));

            if (attribute == null)
                throw new InvalidOperationException($"Cannot proceed class generation due to missing {nameof(WebInvokeAttribute)}");

            var getFullNameFromType = Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, GetCallingExpression(ThisExpression(), IdentifierName(nameof(GetType))), IdentifierName(nameof(Type.FullName))));
            var list = new List<StatementSyntax>
            {
                IfStatement(IdentifierName(Identifier(nameof(DisposableBase.IsDisposed))),
                    ThrowStatement(ObjectCreationExpression(ParseTypeName(nameof(ObjectDisposedException)), ArgumentList(SeparatedList(new[]{getFullNameFromType })), null)))
            };
            
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
            list.Add(queryStringDict);
            list.Add(bodyDict);
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

            var result = MethodDeclaration(returnType, interfaceMethod.Identifier)
                .WithParameterList(interfaceMethod.ParameterList)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddBodyStatements(list.ToArray());
            if (inheritDocTrivia != null)
            {
                result = result.WithLeadingTrivia(inheritDocTrivia.Value);
            }
            return result;
        }

        private static IEnumerable<StatementSyntax> GetInvocationCode(SyntaxToken queryStringDictToken, SyntaxToken bodyDictToken, TypeSyntax interfaceMethodReturnType, IReadOnlyDictionary<string, object> attributeData)
        {
            var descriptorDeclaration = VariableDeclarator("descriptor");
            var requestDeclaration = VariableDeclarator("request");
            return new[]
            {
                GetDescriptorInitialization(attributeData, descriptorDeclaration),
                GetRequestInitialization(queryStringDictToken, bodyDictToken, descriptorDeclaration, requestDeclaration),
                GetReturnExpression(interfaceMethodReturnType, requestDeclaration)
            };
        }

        private static StatementSyntax GetReturnExpression(TypeSyntax interfaceMethodReturnType, VariableDeclaratorSyntax requestDeclaration)
        {
            var processorCallingMethod = GetCallingMethod(interfaceMethodReturnType);
            var returnExpression =
                ReturnStatement(GetCallingExpression(IdentifierName(ProcessorName), processorCallingMethod, requestDeclaration.Identifier));

            return returnExpression;
        }

        private static StatementSyntax GetRequestInitialization(SyntaxToken queryStringDictToken, SyntaxToken bodyDictToken,
            VariableDeclaratorSyntax descriptorDeclaration, VariableDeclaratorSyntax requestDeclaration)
        {
            var remoteRequestArguments = ArgumentList(SeparatedList(
                new[]
                {
                    Argument(IdentifierName(descriptorDeclaration.Identifier)),
                    Argument(GetCallingExpression(queryStringDictToken, nameof(ImmutableDictionary<string, object>.Builder.ToImmutable))),
                    Argument(GetCallingExpression(bodyDictToken, nameof(ImmutableDictionary<string, object>.Builder.ToImmutable)))
                }
            ));

            var requestInitializer =
                EqualsValueClause(InvocationExpression(ObjectCreationExpression(ParseTypeName(nameof(RemoteRequest))), remoteRequestArguments));

            var requestInitialization = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                .AddVariables(requestDeclaration.WithInitializer(requestInitializer)));

            return requestInitialization;
        }

        private static StatementSyntax GetDescriptorInitialization(IReadOnlyDictionary<string, object> attributeData, VariableDeclaratorSyntax descriptorDeclaration)
        {
            if (!attributeData.ContainsKey(nameof(WebInvokeAttribute.Method)))
                throw new InvalidOperationException("Cannot generate call without specifying HTTP method");
            if (!attributeData.TryGetValue(nameof(WebInvokeAttribute.UriTemplate), out var uriTemplate))
            {
                uriTemplate = string.Empty;
            }
            if (!attributeData.TryGetValue(nameof(WebInvokeAttribute.RequestFormat), out var requestFormat))
            {
                requestFormat = default(OperationWebMessageFormat);
            }
            if (!attributeData.TryGetValue(nameof(WebInvokeAttribute.ResponseFormat), out var responseFormat))
            {
                responseFormat = default(OperationWebMessageFormat);
            }
            var remoteOperationDescriptorArguments = ArgumentList(SeparatedList(
                new[]
                {
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(attributeData[nameof(WebInvokeAttribute.Method)].ToString()))),
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression,
                        Literal(uriTemplate.ToString()))),
                    Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseTypeName(nameof(OperationWebMessageFormat)),
                        IdentifierName(Enum.GetName(typeof(OperationWebMessageFormat), requestFormat)))),
                    Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ParseTypeName(nameof(OperationWebMessageFormat)),
                        IdentifierName(Enum.GetName(typeof(OperationWebMessageFormat), responseFormat)))),
                }
            ));

            var descriptorInitializer = EqualsValueClause(InvocationExpression(ObjectCreationExpression(ParseTypeName(nameof(RemoteOperationDescriptor))),
                remoteOperationDescriptorArguments));

            var descriptorInitialization = LocalDeclarationStatement(VariableDeclaration(ParseTypeName("var"))
                .AddVariables(descriptorDeclaration.WithInitializer(descriptorInitializer)));

            return descriptorInitialization;
        }

        private static ExpressionSyntax GetCallingExpression(SyntaxToken invocationSite, string methodName, params SyntaxToken[] arguments) =>
		    GetCallingExpression(IdentifierName(invocationSite), IdentifierName(methodName), arguments);
		
		private static ExpressionSyntax GetCallingExpression(ExpressionSyntax invocationSite, SimpleNameSyntax methodName, params SyntaxToken[] arguments) =>
            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, invocationSite, methodName),
                                 ArgumentList(SeparatedList(arguments.Select(arg => Argument(IdentifierName(arg))))));


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

        private TypeSyntax GetReturnType(TypeSyntax returnType, SemanticModel semanticModel)
        {
            var returnTypeSymbol = (INamedTypeSymbol)semanticModel.GetSymbolInfo(returnType).Symbol;
            if (IsTask(returnTypeSymbol, semanticModel))
            {
                return returnType;
            }
            if (_inheritServiceInterface)
                throw new Exception("Service interface contains methods with non-task return type");
            return TypeSymbolMatchesType(returnTypeSymbol, typeof(void), semanticModel) ? ParseTypeName(nameof(Task)) : GenericName(Identifier("Task"), TypeArgumentList(SeparatedList(new[] { returnType })));
        }

        private static bool IsTask(INamedTypeSymbol typeSymbol, SemanticModel semanticModel) => typeSymbol.IsGenericType ?
            TypeSymbolMatchesType(typeSymbol.ConstructedFrom, typeof(Task<>), semanticModel) :
            TypeSymbolMatchesType(typeSymbol, typeof(Task), semanticModel);

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
