using Avalonia.Ide.CompletionEngine;
using DotNet.Meteor.Xaml.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;

public class CompletionHandler : CompletionHandlerBase {
    private readonly WorkspaceService workspaceService;
    private readonly CompletionEngine? completionEngine;

    public CompletionHandler(WorkspaceService workspaceService) {
        this.workspaceService = workspaceService;
        completionEngine = new CompletionEngine();
    }

    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) {
        return new CompletionRegistrationOptions {
            DocumentSelector = LanguageServer.SelectorFoXamlDocuments,
            TriggerCharacters = new[] { "\'", "\"", " ", "<", ".", "[", "(", "#", "|", "/", "{" },
            AllCommitCharacters = new[] { "\n" },
            ResolveProvider = false,
        };
    }

    public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken) {
        string? text = workspaceService.BufferService.GetTextTillPosition(request.TextDocument.Uri, request.Position);
        if (text == null)
            return new CompletionList();

        var metadata = await InitializeCompletionEngineAsync(request.TextDocument.Uri).ConfigureAwait(false);
        if (metadata == null)
            return new CompletionList();

        var set = completionEngine?.GetCompletions(metadata!, text, text.Length);
        var completions = set?.Completions
            .Where(p => !p.DisplayText.Contains('`'))
            .Select(p => new CompletionItem {
                Label = p.DisplayText,
                Detail = p.Description,
                InsertText = p.InsertText,
                Kind = GetCompletionItemKind(p.Kind),
            });

        return completions == null
            ? new CompletionList(true)
            : new CompletionList(completions, isIncomplete: false);
    }
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken) {
        return Task.FromResult(request);
    }

    private async Task<Metadata?> InitializeCompletionEngineAsync(DocumentUri uri) {
        if (workspaceService.ProjectInfo is not { IsAssemblyExist: true })
            return null;

        if (workspaceService.ProjectInfo.IsAssemblyExist && workspaceService.CompletionMetadata == null)
            await workspaceService.InitializeAsync(uri).ConfigureAwait(false);

        return workspaceService.CompletionMetadata;
    }
    private static CompletionItemKind GetCompletionItemKind(CompletionKind completionKind) {
        string name = Enum.GetName(completionKind) ?? string.Empty;

        var result = name switch {
            _ when name.Contains("Property") || name.Contains("AttachedProperty") => CompletionItemKind.Property,
            _ when name.Contains("Event") => CompletionItemKind.Event,
            _ when name.Contains("Namespace") || name.Contains("VS_XMLNS") => CompletionItemKind.Module,
            _ when name.Contains("MarkupExtension") => CompletionItemKind.Class,
            _ => GetRest(name)
        };

        return result;

        static CompletionItemKind GetRest(string enumName) {
            bool success = Enum.TryParse(enumName, out CompletionItemKind kind);
            return success ? kind : CompletionItemKind.Text;
        }
    }

}