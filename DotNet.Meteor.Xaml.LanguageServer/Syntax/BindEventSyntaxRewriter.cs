using Avalonia.Ide.CompletionEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNet.Meteor.Xaml.LanguageServer.Syntax;

public class BindEventSyntaxRewriter : CSharpSyntaxRewriter {
    public BindEventSyntaxRewriter(CodeBehindSyntaxWalker walker, string eventHandlerName, MetadataEvent metadataEvent) {
        MainClassName = walker.MainClassName;
        LeftIndentationTrivia = walker.LeftIndentationTrivia;
        RightIndentationTrivia = walker.RightIndentationTrivia;
        EventHandlerName = eventHandlerName;
        MetadataEvent = metadataEvent;
    }

    private string MainClassName { get; }
    private SyntaxTriviaList LeftIndentationTrivia { get; }
    private SyntaxTriviaList RightIndentationTrivia { get; }
    private string EventHandlerName { get; }
    public MetadataEvent MetadataEvent { get; }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) {
        if (node.Identifier.Text != MainClassName)
            return base.VisitClassDeclaration(node);
        string methodHandler = GeneratedHandlerMethod();
        var method = SyntaxFactory.ParseMemberDeclaration(methodHandler)?.WithLeadingTrivia(LeftIndentationTrivia)?.WithTrailingTrivia(RightIndentationTrivia);
        if (method == null)
            return base.VisitClassDeclaration(node);
        var result = node.AddMembers(method);
        return result;
    }
    private string GeneratedHandlerMethod() {
        string methodName = EventHandlerName;
        if (MetadataEvent.EventHandlerArgsSignatures.Count == 0)
            return $"private void {methodName}() {{ }}";
        string arguments = string.Join(", ", MetadataEvent.EventHandlerArgsSignatures.Select(p => $"{p.TypeName} {p.Name}"));
        return $"private void {methodName}({arguments}) {{ }}";
    }
}