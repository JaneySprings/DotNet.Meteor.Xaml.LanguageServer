using DotNet.Meteor.Xaml.LanguageServer.Services;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;

public class DidChangeTextDocumentHandler : DidChangeTextDocumentHandlerBase {
    private readonly WorkspaceService workspaceService;
    private readonly DiagnosticService diagnosticService;

    public DidChangeTextDocumentHandler(WorkspaceService workspaceService, DiagnosticService diagnosticService) {
        this.workspaceService = workspaceService;
        this.diagnosticService = diagnosticService;
    }

    protected override TextDocumentChangeRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) {
        return new TextDocumentChangeRegistrationOptions() {
            DocumentSelector = LanguageServer.SelectorFoXamlDocuments,
            SyncKind = TextDocumentSyncKind.Incremental
        };
    }
    public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken) {
        var uri = request.TextDocument.Uri;
        foreach (var change in request.ContentChanges) {
            if (change.Range != null) {
                workspaceService.BufferService.ApplyIncrementalChange(uri, change.Range, change.Text);
            } else {
                workspaceService.BufferService.ApplyFullChange(uri, change.Text);
            }
        }
        _ = diagnosticService.PublishDiagnosticsAsync(uri);
        return Unit.Task;
    }
}