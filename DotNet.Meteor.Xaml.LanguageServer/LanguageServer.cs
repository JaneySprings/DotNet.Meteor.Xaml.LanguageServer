using System.Diagnostics;
using DotNet.Meteor.Xaml.LanguageServer.Handlers.TextDocument;
using DotNet.Meteor.Xaml.LanguageServer.Logging;
using DotNet.Meteor.Xaml.LanguageServer.Services;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace DotNet.Meteor.Xaml.LanguageServer;

public class LanguageServer {
    public static TextDocumentSelector SelectorFoXamlDocuments => TextDocumentSelector.ForPattern("**/*.xaml");

    public static async Task Main(string[] args) {
        var server = await OmniSharpLanguageServer.From(options => options
            .AddDefaultLoggingProvider()
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithServices(services => services
                .AddSingleton<WorkspaceService>())
            .WithHandler<DidOpenTextDocumentHandler>()
            .WithHandler<DidChangeTextDocumentHandler>()
            .WithHandler<DidCloseTextDocumentHandler>()
            // .WithHandler<DidChangeWatchedFilesHandler>()
            // .WithHandler<DidChangeWorkspaceFoldersHandler>()
            // .WithHandler<WorkspaceSymbolsHandler>()
            // .WithHandler<DocumentSymbolHandler>()
            // .WithHandler<HoverHandler>()
            // .WithHandler<FoldingRangeHandler>()
            // .WithHandler<SignatureHelpHandler>()
            // .WithHandler<FormattingHandler>()
            // .WithHandler<RangeFormattingHandler>()
            // .WithHandler<RenameHandler>()
            .WithHandler<CompletionHandler>()
            // .WithHandler<CodeActionHandler>()
            // .WithHandler<ReferencesHandler>()
            // .WithHandler<ImplementationHandler>()
            // .WithHandler<DefinitionHandler>()
            // .WithHandler<TypeDefinitionHandler>()
            .OnStarted(StartedHandlerAsync)
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
    private static Task StartedHandlerAsync(ILanguageServer server, CancellationToken cancellationToken) {
        var clientSettings = server.Workspace.ClientSettings;
        ObserveClientProcess(clientSettings.ProcessId);
        return Task.CompletedTask;
    }
    private static void ObserveClientProcess(long? pid) {
        if (pid == null || pid <= 0)
            return;

        var ideProcess = Process.GetProcessById((int)pid.Value);
        ideProcess.EnableRaisingEvents = true;
        ideProcess.Exited += (_, _) => {
            CurrentSessionLogger.Debug($"Shutting down server because client process [{ideProcess.ProcessName}] has exited");
            Environment.Exit(0);
        };
        CurrentSessionLogger.Debug($"Server is observing client process {ideProcess.ProcessName} (PID: {pid})");
    }
}
