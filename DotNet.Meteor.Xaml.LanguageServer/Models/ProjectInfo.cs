using DotNet.Meteor.Common;
using DotNet.Meteor.Common.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using SystemPath = System.IO.Path;
using SystemDirectory = System.IO.Directory;

namespace DotNet.Meteor.Xaml.LanguageServer.Models;

public class ProjectInfo : Project {
    private string assemblyName;
    public string AssemblyName => assemblyName;

    private string assemblyPath;
    public string AssemblyPath => assemblyPath;

    public bool IsAssemblyExist => File.Exists(AssemblyPath);

    private ProjectInfo(string path) : base(path) { 
        assemblyName = this.EvaluateProperty("AssemblyName", SystemPath.GetFileNameWithoutExtension(Path)) ?? string.Empty;
        assemblyPath = FindAssemblyInPath(assemblyName, SystemPath.Combine(Directory, "obj", "Debug"));
        if (string.IsNullOrEmpty(assemblyPath))
            assemblyPath = FindAssemblyInPath(assemblyName, SystemPath.Combine(Directory, "bin", "Debug"));
    }

    private static string FindAssemblyInPath(string assemblyName, string path) {
        if (!SystemDirectory.Exists(path))
            return string.Empty;

        var outputAssemblies = SystemDirectory.GetFiles(path, $"{assemblyName}.dll", SearchOption.AllDirectories).Select(it => new FileInfo(it));
        var filteredAssemblies = outputAssemblies.Where(it => File.Exists(SystemPath.Combine(it.DirectoryName!, "Microsoft.Maui.Controls.dll")));
        var assemblyPath = filteredAssemblies.OrderByDescending(it => it.LastWriteTime).FirstOrDefault()?.FullName ?? string.Empty;
        return assemblyPath;
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