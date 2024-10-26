using DotNet.Meteor.Common;
using DotNet.Meteor.Common.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using SystemPath = System.IO.Path;
using SystemDirectory = System.IO.Directory;
using Avalonia.Ide.CompletionEngine;

namespace DotNet.Meteor.Xaml.LanguageServer.Models;

public class ProjectInfo : Project {
    public Metadata? CompletionMetadata { get; set; }

    private string assemblyName;
    public string AssemblyName => assemblyName;

    private string assemblyPath;
    public string AssemblyPath => assemblyPath;

    public bool IsAssemblyExist => File.Exists(AssemblyPath);

    private ProjectInfo(string path) : base(path) { 
        assemblyName = this.EvaluateProperty("AssemblyName", SystemPath.GetFileNameWithoutExtension(Path)) ?? string.Empty;
        
        var outputAssemblies = SystemDirectory.GetFiles(Directory, $"{assemblyName}.dll", SearchOption.AllDirectories).Select(it => new FileInfo(it));
        var filteredAssemblies = outputAssemblies.Where(it => File.Exists(SystemPath.Combine(it.DirectoryName!, "Microsoft.Maui.Controls.dll")));
        assemblyPath = filteredAssemblies.OrderByDescending(it => it.LastWriteTime).FirstOrDefault()?.FullName ?? string.Empty;
    }

    public static async Task<ProjectInfo?> GetProjectInfoAsync(DocumentUri uri) {
        string path = uri.GetFileSystemPath();
        string root = SystemDirectory.GetDirectoryRoot(path);
        string? current = SystemPath.GetDirectoryName(path);

        if (!File.Exists(path) || current == null)
            return null;

        var files = Array.Empty<FileInfo>();
        var info = await Task.Run(() => {
            while (root != current && files.Length == 0) {
                var directory = new DirectoryInfo(current!);
                files = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

                if (files.Length != 0)
                    break;

                current = SystemPath.GetDirectoryName(current);
            }

            return files.Length != 0 ? new ProjectInfo(files.First().FullName) : null;
        });

        return info;
    }
}