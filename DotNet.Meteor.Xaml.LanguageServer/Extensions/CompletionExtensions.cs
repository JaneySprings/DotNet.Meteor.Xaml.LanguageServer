using Avalonia.Ide.CompletionEngine;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DotNet.Meteor.Xaml.LanguageServer.Extensions;

public static class CompletionExtensions {
    public static CompletionItemKind ToCompletionItemKind(this CompletionKind completionKind) {
        return completionKind switch {
            CompletionKind.Comment => CompletionItemKind.Snippet,
            CompletionKind.Class => CompletionItemKind.Class,
            CompletionKind.Property => CompletionItemKind.Property,
            CompletionKind.AttachedProperty => CompletionItemKind.Property,
            CompletionKind.StaticProperty => CompletionItemKind.Property,
            CompletionKind.Namespace => CompletionItemKind.Module,
            CompletionKind.Enum => CompletionItemKind.Enum,
            CompletionKind.MarkupExtension => CompletionItemKind.Interface,
            CompletionKind.Event => CompletionItemKind.Event,
            CompletionKind.AttachedEvent => CompletionItemKind.Event,
            CompletionKind.DataProperty => CompletionItemKind.Property,
            CompletionKind.TargetTypeClass => CompletionItemKind.Class,
            CompletionKind.VS_XMLNS => CompletionItemKind.Module,
            CompletionKind.Selector => CompletionItemKind.Module,
            CompletionKind.Name => CompletionItemKind.Text,
            _ => CompletionItemKind.Text
        };
    }
}