using System.Text.Json;
using Avalonia.Ide.CompletionEngine;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
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
            throw new OperationCanceledException($"Failed to initialize workspace: {uri}", e);
        }
    }

    private Metadata? BuildCompletionMetadata(DocumentUri uri) {
        var slnFile = SolutionName(uri) ?? Path.GetFileNameWithoutExtension(ProjectInfo?.ProjectDirectory);

        if (slnFile == null)
            return null;


        var slnFilePath = Path.Combine(Path.GetTempPath(), $"{slnFile}.json");

        if (!File.Exists(slnFilePath))
            return _metadataReader.GetForTargetAssembly(ProjectInfo?.AssemblyPath() ?? "");

        string content = File.ReadAllText(slnFilePath);
        var package = JsonSerializer.Deserialize<SolutionData>(content);
        var exeProj = package!.GetExecutableProject();

        return _metadataReader.GetForTargetAssembly(exeProj?.TargetPath ?? "");
    }
    private string? SolutionName(DocumentUri uri) {
        string path = uri.GetFileSystemPath();
        string root = Directory.GetDirectoryRoot(path);
        string? current = Path.GetDirectoryName(path);

        if (!File.Exists(path) || current == null)
            return null;

        var files = Array.Empty<FileInfo>();

        while (root != current && files.Length == 0) {
            var directory = new DirectoryInfo(current!);
            files = directory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);

            if (files.Length != 0)
                break;

            current = Path.GetDirectoryName(current);
        }

        return files.FirstOrDefault()?.Name;
    }

    public Metadata? CompletionMetadata { get; private set; }
    readonly MetadataReader _metadataReader = new(new DnlibMetadataProvider());
}