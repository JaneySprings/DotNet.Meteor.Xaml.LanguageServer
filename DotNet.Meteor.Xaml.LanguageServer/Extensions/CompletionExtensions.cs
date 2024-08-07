using System.Text;
using Avalonia.Ide.CompletionEngine;
using DotNet.Meteor.Xaml.LanguageServer.Syntax;
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
            CompletionKind.AttachedEvent => CompletionItemKind.Event, // not used
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
            Command = completion.Kind == CompletionKind.Event || completion.Kind == CompletionKind.Property 
                ? Command.Create("editor.action.triggerSuggest") : null
        };
    }

    public static List<CompletionItem> ResolveEventCompletionItem(this Completion completion, string xamlDocumentPath) {
        var codeBehindDocumentPath = Path.ChangeExtension(xamlDocumentPath, ".xaml.cs");
        if (!File.Exists(codeBehindDocumentPath))
            return new List<CompletionItem>() { completion.ToCompletionItem() };

        List<CompletionItem> completionItems = new();
        var metadataEvent = (MetadataEvent)completion.Data!;
        string methodName = completion.InsertText;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(codeBehindDocumentPath));
        CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

        CodeBehindSyntaxWalker eventSyntaxWalker = new();
        eventSyntaxWalker.Visit(root);
        foreach (var methodSignatureInfo in eventSyntaxWalker.MethodSignatureInfos) {
            if (methodSignatureInfo.ArgumentTypes.Count != metadataEvent.EventHandlerArgsSignatures.Count)
                continue;
            CompletionItem item = new CompletionItem {
                Label = methodSignatureInfo.Name,
                Detail = string.Join(", ", methodSignatureInfo.ArgumentTypes),
                InsertText = methodSignatureInfo.Name,
                InsertTextFormat = InsertTextFormat.Snippet,
                Kind = CompletionItemKind.Method,
            };
            completionItems.Add(item);
        }
        string eventHandlerName = eventSyntaxWalker.GetUniqueMethodName(methodName);
        BindEventSyntaxRewriter eventSyntaxRewriter = new(eventSyntaxWalker, eventHandlerName, metadataEvent);
        var newRoot = eventSyntaxRewriter.Visit(root);

        var documentEdit = new TextDocumentEdit {
            TextDocument = new OptionalVersionedTextDocumentIdentifier {
                Uri = DocumentUri.FromFileSystemPath(codeBehindDocumentPath),
            },
            Edits = new[] {
                new TextEdit {
                    NewText = newRoot.ToString(),
                    Range = new Range {
                        Start = new Position(0, 0),
                        End = new Position(int.MaxValue, int.MaxValue)
                    }
                }
            }
        };

        completionItems.Add(new CompletionItem {
            Label = completion.DisplayText,
            InsertText = eventHandlerName,
            Detail = $"Bind event to a newly created method called '{eventHandlerName}'.",
            Kind = completion.Kind.ToCompletionItemKind(),
            Command = new Command {
                Name = "dotnet-meteor.xaml.replaceCode",
                // arguments is JArray
                Arguments = new JArray {
                   JToken.FromObject(documentEdit)
                }
            },
        });
        return completionItems;
    }
}