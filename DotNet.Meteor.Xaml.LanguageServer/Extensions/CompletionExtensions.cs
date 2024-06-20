using Avalonia.Ide.CompletionEngine;
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
        // var documentEdit = new TextDocumentEdit {
        //     TextDocument = new OptionalVersionedTextDocumentIdentifier {
        //         Uri = DocumentUri.FromFileSystemPath(codeBehindDocumentPath),
        //     },
        //     Edits = new[] {
        //         new TextEdit {
        //             NewText = $"{metadataEvent.ArgsName}",
        //             Range = new Range {
        //                 Start = new Position(0, 0),
        //                 End = new Position(1, 1)
        //             }
        //         }
        //     }
        // };

        return new CompletionItem {
            Label = completion.DisplayText,
            InsertText = completion.InsertText, //todo
            Kind = completion.Kind.ToCompletionItemKind(),
            // Command = new Command {
            //     Name = "dotnet-meteor.xaml.insertEvent",
            //     // arguments is JArray
            //     Arguments = new JArray {
            //        JToken.FromObject(documentEdit)
            //     }
            // },
        };
    }
}