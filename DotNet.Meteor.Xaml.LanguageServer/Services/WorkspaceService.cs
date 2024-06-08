using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
using DotNet.Meteor.Xaml.LanguageServer.Logging;
using DotNet.Meteor.Xaml.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace DotNet.Meteor.Xaml.LanguageServer.Services;

public class WorkspaceService {
    public ProjectInfo? ProjectInfo { get; private set; }
    public BufferService BufferService { get; } = new();

    public async Task InitializeAsync(DocumentUri uri) {
        try {
            ProjectInfo = await ProjectInfo.GetProjectInfoAsync(uri).ConfigureAwait(false);
            CompletionMetadata = BuildCompletionMetadata(uri);
        } catch (Exception e) {
            CurrentSessionLogger.Error(e);
            CurrentSessionLogger.Error($"Failed to initialize workspace: {uri}");
        }
    }

    private Metadata? BuildCompletionMetadata(DocumentUri uri) {
        var outputAssemblyPath = ProjectInfo?.AssemblyPath();
        if (string.IsNullOrEmpty(outputAssemblyPath)) {
            CurrentSessionLogger.Error($"Failed to get output assembly path for {uri}");
            return null;
        }
        return _metadataReader.GetForTargetAssembly(outputAssemblyPath);
    }

    public Metadata? CompletionMetadata { get; private set; }
    private readonly MetadataReader _metadataReader = new(new DnlibMetadataProvider());
}