using Avalonia.Ide.CompletionEngine;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

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
            _ => CompletionItemKind.Text
        };
    }
}