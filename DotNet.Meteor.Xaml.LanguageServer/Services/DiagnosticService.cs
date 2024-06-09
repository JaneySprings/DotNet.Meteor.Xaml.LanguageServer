using System.Xml;
using DotNet.Meteor.Xaml.LanguageServer.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace DotNet.Meteor.Xaml.LanguageServer.Services;

public class DiagnosticService {
    private const int AnalysisFrequencyMs = 500;

    private readonly WorkspaceService workspaceService;
    private readonly ILanguageServerFacade languageServer;
    private CancellationTokenSource diagnosticTokenSource;

    public DiagnosticService(WorkspaceService workspaceService, ILanguageServerFacade languageServer) {
        this.workspaceService = workspaceService;
        this.languageServer = languageServer;
        diagnosticTokenSource = new CancellationTokenSource();
    }

    public Task PublishDiagnosticsAsync(DocumentUri uri) {
        if (workspaceService.ProjectInfo == null)
            return Task.CompletedTask;

        ResetCancellationToken();
        var cancellationToken = diagnosticTokenSource.Token;
        return ServerExtensions.InvokeAsync(async () => {
            await Task.Delay(AnalysisFrequencyMs, cancellationToken).ConfigureAwait(false);
            var documentContent = workspaceService.BufferService.GetText(uri);
            if (documentContent == null)
                return;

            var diagnostics = new Container<Diagnostic>();

            try {
                new XmlDocument().LoadXml(documentContent);
            } catch (XmlException e) {
                diagnostics = new Container<Diagnostic>(new Diagnostic() {
                    Message = e.Message,
                    Severity = DiagnosticSeverity.Error,
                    Code = $"XAML{e.LineNumber:D4}",
                    Range = new Range() {
                        Start = new Position(e.LineNumber - 1, e.LinePosition - 1),
                        End = new Position(e.LineNumber - 1, int.MaxValue)
                    },
                });
            } finally {
                languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams() {
                    Diagnostics = diagnostics,
                    Uri = uri,
                });
            }
        });
    }

    private void ResetCancellationToken() {
        diagnosticTokenSource.Cancel();
        diagnosticTokenSource?.Dispose();
        diagnosticTokenSource = new CancellationTokenSource();
    }
}