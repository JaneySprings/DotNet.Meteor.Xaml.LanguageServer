using DotNet.Meteor.Xaml.LanguageServer.Services;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;

public class DidOpenTextDocumentHandler : DidOpenTextDocumentHandlerBase {
    private readonly WorkspaceService workspaceService;
    private readonly DiagnosticService diagnosticService;

    public DidOpenTextDocumentHandler(WorkspaceService workspaceService, DiagnosticService diagnosticService) {
        this.workspaceService = workspaceService;
        this.diagnosticService = diagnosticService;
    }

    protected override TextDocumentOpenRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) {
        return new TextDocumentOpenRegistrationOptions() {
            DocumentSelector = LanguageServer.SelectorFoXamlDocuments
        };
    }
    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken) {
        var uri = request.TextDocument.Uri;
        string text = request.TextDocument.Text;

        _ = await workspaceService.GetOrCreateProjectInfoAsync(uri).ConfigureAwait(false);

        workspaceService.BufferService.Add(uri, text);
        _ = diagnosticService.PublishDiagnosticsAsync(uri);

        return Unit.Value;
    }
}