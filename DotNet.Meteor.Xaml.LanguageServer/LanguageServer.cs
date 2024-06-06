﻿using System.Diagnostics;
using DotNet.Meteor.Xaml.LanguageServer.Logging;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using OmniSharpLanguageServer = OmniSharp.Extensions.LanguageServer.Server.LanguageServer;

namespace DotNet.Meteor.Xaml.LanguageServer;

public class LanguageServer {
    public static TextDocumentSelector SelectorFoXamkDocuments => TextDocumentSelector.ForPattern("**/*.xaml");

    public static async Task Main(string[] args) {
        var server = await OmniSharpLanguageServer.From(options => options
            .AddDefaultLoggingProvider()
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            // .WithServices(services => services
                // .AddSingleton<WorkspaceService>()
                // .AddSingleton<CodeAnalysisService>()
                // .AddSingleton<DecompilationService>())
            // .WithHandler<DidOpenTextDocumentHandler>()
            // .WithHandler<DidChangeTextDocumentHandler>()
            // .WithHandler<DidCloseTextDocumentHandler>()
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
            // .WithHandler<CompletionHandler>()
            // .WithHandler<CodeActionHandler>()
            // .WithHandler<ReferencesHandler>()
            // .WithHandler<ImplementationHandler>()
            // .WithHandler<DefinitionHandler>()
            // .WithHandler<TypeDefinitionHandler>()
            .OnStarted((s, _) => StartedHandlerAsync(s, args))
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
    private static async Task StartedHandlerAsync(ILanguageServer server, string[] targets) {
        var clientSettings = server.Workspace.ClientSettings;
        // var configurationService = server.Services.GetService<ConfigurationService>()!;
        // var workspaceService = server.Services.GetService<WorkspaceService>()!;

        CurrentSessionLogger.Debug($"Server created with targets: {string.Join(", ", targets)}");
        ObserveClientProcess(clientSettings.ProcessId);

        // await configurationService.InitializeAsync().ConfigureAwait(false);
        // workspaceService.InitializeWorkspace(_ => server.ShowError(Resources.DotNetRegistrationFailed));

        // if (targets.Length != 0)
        //     workspaceService.AddTargets(targets);
        // else if (configurationService.ProjectFiles.Count != 0)
        //     workspaceService.AddTargets(configurationService.ProjectFiles);
        // else {
        //     var workspaceFolders = server.ClientSettings.WorkspaceFolders?.Select(it => it.Uri.GetFileSystemPath());
        //     workspaceService.FindTargetsInWorkspace(workspaceFolders);
        // }

        // _ = workspaceService.LoadSolutionAsync(CancellationToken.None);
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
