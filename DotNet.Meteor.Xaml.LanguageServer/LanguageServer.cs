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
                .AddSingleton<WorkspaceService>()
                .AddSingleton<DiagnosticService>())
            .WithHandler<DidOpenTextDocumentHandler>()
            .WithHandler<DidChangeTextDocumentHandler>()
            .WithHandler<DidCloseTextDocumentHandler>()
            // .WithHandler<DidChangeWatchedFilesHandler>()
            // .WithHandler<DidChangeWorkspaceFoldersHandler>()
            // .WithHandler<DocumentSymbolHandler>()
            // .WithHandler<HoverHandler>()
            .WithHandler<DocumentFormattingHandler>()
            .WithHandler<CompletionHandler>()
            // .WithHandler<CodeActionHandler>()
            // .WithHandler<DefinitionHandler>()
            .OnStarted((s, ct) => StartedHandlerAsync(s, args.FirstOrDefault(), ct))
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
    private static async Task StartedHandlerAsync(ILanguageServer server, string? targetProject, CancellationToken _) {
        var clientSettings = server.Workspace.ClientSettings;
        ObserveClientProcess(clientSettings.ProcessId);

        if (!string.IsNullOrEmpty(targetProject)) {
            var workspaceService = server.Services.GetService<WorkspaceService>()!;
            await workspaceService.InitializeAsync(targetProject).ConfigureAwait(false);
        }
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
