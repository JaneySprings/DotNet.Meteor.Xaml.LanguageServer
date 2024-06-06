using DotNet.Meteor.Xaml.LanguageServer.Services;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;

public class DidCloseTextDocumentHandler : DidCloseTextDocumentHandlerBase {
    private readonly WorkspaceService workspaceService;

    public DidCloseTextDocumentHandler(WorkspaceService workspaceService) {
        this.workspaceService = workspaceService;
    }

    protected override TextDocumentCloseRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) {
        return new TextDocumentCloseRegistrationOptions() {
            DocumentSelector = LanguageServer.SelectorFoXamlDocuments
        };
    }
    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken) {
        var uri = request.TextDocument.Uri;
        workspaceService.BufferService.Remove(uri);

        return Unit.Task;
    }
}