using System.Text;
using Avalonia.Ide.CompletionEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace DotNet.Meteor.Xaml.LanguageServer.Extensions;

public static class CompletionExtensions {
    public static CompletionItemKind ToCompletionItemKind(this CompletionKind completionKind) {
        return completionKind switch {
            CompletionKind.Snippet => CompletionItemKind.Snippet,
            CompletionKind.Class => CompletionItemKind.Class,
            CompletionKind.Property => CompletionItemKind.Property,
            CompletionKind.AttachedProperty => CompletionItemKind.Property,
            CompletionKind.StaticProperty => CompletionItemKind.EnumMember,
            CompletionKind.Enum => CompletionItemKind.EnumMember,
            CompletionKind.MarkupExtension => CompletionItemKind.Class,
            CompletionKind.Event => CompletionItemKind.Event,
            CompletionKind.AttachedEvent => CompletionItemKind.Event,
            CompletionKind.DataProperty => CompletionItemKind.Property,
            CompletionKind.TargetTypeClass => CompletionItemKind.Class,
            CompletionKind.Namespace => CompletionItemKind.Module,
            CompletionKind.VS_XMLNS => CompletionItemKind.Module,
            CompletionKind.Name => CompletionItemKind.Text,

            CompletionKind.Namespace | CompletionKind.VS_XMLNS => CompletionItemKind.Module,
            CompletionKind.Class | CompletionKind.TargetTypeClass => CompletionItemKind.Class,
            _ => CompletionItemKind.Text
        };
    }

    public static CompletionItem ToCompletionItem(this Completion completion) {
        return new CompletionItem {
            Label = completion.DisplayText,
            Detail = completion.Description,
            InsertText = completion.InsertText,
            InsertTextFormat = InsertTextFormat.Snippet,
            Kind = completion.Kind.ToCompletionItemKind(),
        };
    }

    public static CompletionItem ResolveEventCompletionItem(this Completion completion, string xamlDocumentPath) {
        var codeBehindDocumentPath = Path.ChangeExtension(xamlDocumentPath, ".xaml.cs");
        if (!File.Exists(codeBehindDocumentPath))
            return completion.ToCompletionItem();

        var metadataEvent = (MetadataEvent)completion.Data!;
        string methodName = completion.InsertText;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(codeBehindDocumentPath));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        EventSyntaxWalker eventSyntaxWalker = new();
        eventSyntaxWalker.Visit(root);

        EventSyntaxRewriter eventSyntaxRewriter = new(eventSyntaxWalker, completion);
        var newRoot = eventSyntaxRewriter.Visit(root);

        string newText = newRoot.ToString();
        var documentEdit = new TextDocumentEdit {
            TextDocument = new OptionalVersionedTextDocumentIdentifier {
                Uri = DocumentUri.FromFileSystemPath(codeBehindDocumentPath),
            },
            Edits = new[] {
                new TextEdit {
                    NewText = newText,
                    Range = new Range {
                        Start = new Position(0, 0),
                        End = new Position(int.MaxValue, int.MaxValue)
                    }
                }
            }
        };

        return new CompletionItem {
            Label = completion.DisplayText,
            InsertText = completion.InsertText, //todo
            Kind = completion.Kind.ToCompletionItemKind(),
            Command = new Command {
                Name = "dotnet-meteor.xaml.replaceCode",
                // arguments is JArray
                Arguments = new JArray {
                   JToken.FromObject(documentEdit)
                }
            },
        };
    }
}

public class EventSyntaxWalker : CSharpSyntaxWalker {
    public string MainClassName { get; private set; } = string.Empty;
    public SyntaxTriviaList LeftIndentationTrivia { get; private set; }
    public SyntaxTriviaList RightIndentationTrivia { get; private set; }

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
}
public class EventSyntaxRewriter : CSharpSyntaxRewriter {
    public EventSyntaxRewriter(EventSyntaxWalker walker, Completion completion) {
        MainClassName = walker.MainClassName;
        LeftIndentationTrivia = walker.LeftIndentationTrivia;
        RightIndentationTrivia = walker.RightIndentationTrivia;
        this.completion = completion;
    }

    private string MainClassName { get; }
    private SyntaxTriviaList LeftIndentationTrivia { get; }
    private SyntaxTriviaList RightIndentationTrivia { get; }
    private readonly Completion completion;

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
    string GeneratedHandlerMethod() {
        var metadataEvent = (MetadataEvent)completion.Data!;
        string methodName = completion.InsertText;
        if (metadataEvent.EventHandlerArgsSignatures.Count == 0)
            return $"private void {methodName}() {{ }}";
        string arguments = string.Join(", ", metadataEvent.EventHandlerArgsSignatures.Select(p => $"{p.TypeName} {p.Name}"));
        return $"private void {methodName}({arguments}) {{ }}";
    }
}