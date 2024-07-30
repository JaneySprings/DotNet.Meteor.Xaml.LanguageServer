using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
using DotNet.Meteor.Xaml.LanguageServer.Logging;
using DotNet.Meteor.Xaml.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace DotNet.Meteor.Xaml.LanguageServer.Services;

public class WorkspaceService {
    private const string XamlFormatterConfigFileName = ".xamlstylerconfig";

    public ProjectInfo? ProjectInfo { get; private set; }
    public BufferService BufferService { get; } = new();

    public Task InitializeAsync(string filePath) {
        return InitializeAsync(DocumentUri.FromFileSystemPath(filePath));
    }
    public async Task InitializeAsync(DocumentUri uri) {
        try {
            var projectInfo = await ProjectInfo.GetProjectInfoAsync(uri).ConfigureAwait(false);
            if (projectInfo?.Path == ProjectInfo?.Path)
                return;

            ProjectInfo = projectInfo;
            CompletionMetadata = BuildCompletionMetadata(uri);
        } catch (Exception e) {
            CurrentSessionLogger.Error(e);
            CurrentSessionLogger.Error($"Failed to initialize workspace for document: {uri}");
        }
    }
    public async Task<Metadata?> InitializeCompletionEngineAsync(DocumentUri uri) {
        if (ProjectInfo is not { IsAssemblyExist: true })
            return null;

        if (ProjectInfo.IsAssemblyExist && CompletionMetadata == null)
            await InitializeAsync(uri).ConfigureAwait(false);

        return CompletionMetadata;
    }

    public string FindXamlFormatterConfigFile() {
        if (ProjectInfo == null)
            return string.Empty;

        var configFilePath = string.Empty;
        var directory = new DirectoryInfo(ProjectInfo.ProjectDirectory);
        while (directory.Parent != null) {
            configFilePath = Path.Combine(directory.FullName, XamlFormatterConfigFileName);
            if (File.Exists(configFilePath))
                return configFilePath;

            directory = directory.Parent;
        }

        return string.Empty;
    }

    private Metadata? BuildCompletionMetadata(DocumentUri uri) {
        var outputAssemblyPath = ProjectInfo?.AssemblyPath;
        if (string.IsNullOrEmpty(outputAssemblyPath)) {
            CurrentSessionLogger.Error($"Failed to get output assembly path for {uri}");
            return null;
        }
        return _metadataReader.GetForTargetAssembly(outputAssemblyPath);
    }

    public Metadata? CompletionMetadata { get; private set; }
    private readonly MetadataReader _metadataReader = new(new DnlibMetadataProvider());
}