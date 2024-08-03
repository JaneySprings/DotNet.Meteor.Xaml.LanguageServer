using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNet.Meteor.Xaml.LanguageServer.Syntax;

public class CodeBehindSyntaxWalker : CSharpSyntaxWalker {
    public string MainClassName { get; private set; } = string.Empty;
    public SyntaxTriviaList LeftIndentationTrivia { get; private set; }
    public SyntaxTriviaList RightIndentationTrivia { get; private set; }
    public List<MethodSignatureInfo> MethodSignatureInfos { get; } = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
        base.VisitClassDeclaration(node);
    }
    public override void VisitConstructorInitializer(ConstructorInitializerSyntax node) {
        base.VisitConstructorInitializer(node);
    }
    public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
        base.VisitInvocationExpression(node);
        if (node.Expression is IdentifierNameSyntax identifierNameSyntax) {
            if (identifierNameSyntax.Identifier.Text == "InitializeComponent") {
                if (node.Parent?.Parent?.Parent is ConstructorDeclarationSyntax constructorDeclarationSyntax) {
                    LeftIndentationTrivia = constructorDeclarationSyntax.GetLeadingTrivia();
                    RightIndentationTrivia = constructorDeclarationSyntax.GetTrailingTrivia();
                    MainClassName = constructorDeclarationSyntax.Identifier.Text;
                }
            }
        }
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
        base.VisitMethodDeclaration(node);
        MethodSignatureInfo methodSignatureInfo = new() { Name = node.Identifier.Text };
        foreach (var parameter in node.ParameterList.Parameters) {
            var resolvedTypeSimpleName = parameter?.Type?.Resolve();
            methodSignatureInfo.ArgumentTypes.Add(resolvedTypeSimpleName ?? string.Empty);
        }
        MethodSignatureInfos.Add(methodSignatureInfo);
    }
    public class MethodSignatureInfo {
        public string Name { get; set; } = string.Empty;
        public List<string> ArgumentTypes { get; set; } = new();
    }
    public string GetUniqueMethodName(string insertText) {
        string methodName = insertText;
        for (int i = 0; IsMethodNameExists(methodName); i++) {
            methodName = $"{insertText}_{++i}";
        }
        return methodName;
    }
    private bool IsMethodNameExists(string methodName) {
        return MethodSignatureInfos.Any(p => p.Name == methodName);
    }
}
