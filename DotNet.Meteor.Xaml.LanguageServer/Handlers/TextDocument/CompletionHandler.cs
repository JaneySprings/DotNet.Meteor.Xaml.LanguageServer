using Avalonia.Ide.CompletionEngine;
using DotNet.Meteor.Xaml.LanguageServer.Extensions;
using DotNet.Meteor.Xaml.LanguageServer.Services;
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
            TriggerCharacters = new[] { "\'", "\"", " ", "<", ".", "[", "(", "#", "|", "/", "{", ":" },
            AllCommitCharacters = new[] { "\n" },
            ResolveProvider = true,
        };
    }

    public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken) {
        string documentPath = request.TextDocument.Uri.GetFileSystemPath();
        string? text = workspaceService.BufferService.GetTextTillPosition(request.TextDocument.Uri, request.Position);
        if (text == null)
            return new CompletionList();

        var metadata = await workspaceService.InitializeCompletionEngineAsync(request.TextDocument.Uri).ConfigureAwait(false);
        if (metadata == null)
            return new CompletionList();

        var set = completionEngine?.GetCompletions(metadata!, text, text.Length, workspaceService.ProjectInfo?.AssemblyName);
        if (set?.Completions.Count == 1 && set.Completions[0].Data is MetadataEvent)
            return new CompletionList(set.Completions[0].ResolveEventCompletionItem(documentPath));

        var completions = set?.Completions
            .Where(p => !p.DisplayText.Contains('`'))
            .Select(p => p.ToCompletionItem());

        return completions == null
            ? new CompletionList(true)
            : new CompletionList(completions, isIncomplete: false);
    }
    public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken) {
        if (request.Kind == CompletionItemKind.Event || request.Kind == CompletionItemKind.Property)
            return Task.FromResult(request with { Command = Command.Create("editor.action.triggerSuggest") });

        return Task.FromResult(request);
    }
}