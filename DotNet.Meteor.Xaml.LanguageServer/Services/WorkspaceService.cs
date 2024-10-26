using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
using DotNet.Meteor.Xaml.LanguageServer.Logging;
using DotNet.Meteor.Xaml.LanguageServer.Models;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace DotNet.Meteor.Xaml.LanguageServer.Services;

public class WorkspaceService {
    private const string XamlFormatterConfigFileName = ".xamlstylerconfig";
    private Dictionary<string, ProjectInfo> projectInfosCache = new();

    public BufferService BufferService { get; } = new();

    public async Task<ProjectInfo?> GetOrCreateProjectInfoAsync(DocumentUri uri) {
        try {
            var projectInfo = await ProjectInfo.GetProjectInfoAsync(uri).ConfigureAwait(false);
            if (projectInfo == null)
                return null;

            if (projectInfosCache.ContainsKey(projectInfo.Path))
                return projectInfosCache[projectInfo.Path];

            projectInfosCache[projectInfo.Path] = projectInfo;
            projectInfo.CompletionMetadata = BuildCompletionMetadata(projectInfo);
            return projectInfo;
        } catch (Exception e) {
            CurrentSessionLogger.Error($"Failed to initialize workspace for document '{uri}': {e}");
            return null;
        }
    }
    public async Task<Metadata?> InitializeCompletionEngineAsync(DocumentUri uri) {
        var projectInfo = await GetOrCreateProjectInfoAsync(uri).ConfigureAwait(false);
        return projectInfo?.CompletionMetadata;
    }

    public string FindXamlFormatterConfigFile(DocumentUri uri) {
        var directoryPath = Path.GetDirectoryName(uri.GetFileSystemPath())!;
        var directory = new DirectoryInfo(directoryPath);
    
        var configFilePath = string.Empty;
        while (directory.Parent != null) {
            configFilePath = Path.Combine(directory.FullName, XamlFormatterConfigFileName);
            if (File.Exists(configFilePath))
                return configFilePath;

            directory = directory.Parent;
        }

        return string.Empty;
    }

    private Metadata? BuildCompletionMetadata(ProjectInfo projectInfo) {
        var metadata = _metadataReader.GetForTargetAssembly(projectInfo.AssemblyPath);
        if (metadata == null) {
            CurrentSessionLogger.Error($"Failed to build completion metadata for target assembly: {projectInfo.AssemblyPath}");
            return null;
        }
        CurrentSessionLogger.Debug($"Metadata is Initialized for project: '{projectInfo.Path}' -> '{projectInfo.AssemblyPath}'");
        metadata.TargetAssemblyName = projectInfo.AssemblyName;
        return metadata;
    }

    private readonly MetadataReader _metadataReader = new(new DnlibMetadataProvider());
}