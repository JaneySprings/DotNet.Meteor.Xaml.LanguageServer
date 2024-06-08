using DotNet.Meteor.Xaml.LanguageServer.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xavalon.XamlStyler;
using Xavalon.XamlStyler.Options;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;

public class DocumentFormattingHandler : DocumentFormattingHandlerBase {
    private readonly WorkspaceService workspaceService;

    public DocumentFormattingHandler(WorkspaceService workspaceService) {
        this.workspaceService = workspaceService;
    }

    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) {
        return new DocumentFormattingRegistrationOptions() {
            DocumentSelector = LanguageServer.SelectorFoXamlDocuments
        };
    }

    public override Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken) {
        var stylerService = new StylerService(
            new StylerOptions(workspaceService.FindXamlFormatterConfigFile()),
            new XamlLanguageOptions() { IsFormatable = true }
        );

        var documentContent = workspaceService.BufferService.GetText(request.TextDocument.Uri);
        if (documentContent == null)
            return Task.FromResult<TextEditContainer?>(null);

        var formattedDocument = stylerService.StyleDocument(documentContent);
        if (formattedDocument == null)
            return Task.FromResult<TextEditContainer?>(null);

        return Task.FromResult<TextEditContainer?>(new TextEditContainer(new TextEdit() {
            NewText = formattedDocument,
            Range = new Range() {
                Start = new Position(0, 0),
                End = new Position(int.MaxValue, int.MaxValue),
            }
        }));
    }
}