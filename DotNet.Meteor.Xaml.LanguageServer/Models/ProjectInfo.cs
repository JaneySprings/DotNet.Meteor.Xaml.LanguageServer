using DotNet.Meteor.Common;
using OmniSharp.Extensions.LanguageServer.Protocol;
using MSBuildProject = DotNet.Meteor.Common.Project;
using SystemPath = System.IO.Path;

namespace DotNet.Meteor.Xaml.LanguageServer.Models;

public class ProjectInfo : MSBuildProject {
    public string ProjectDirectory => SystemPath.GetDirectoryName(Path) ?? string.Empty;
    public string AssemblyName => SystemPath.GetFileNameWithoutExtension(AssemblyPath);
    public bool IsAssemblyExist => !string.IsNullOrEmpty(AssemblyPath) && File.Exists(AssemblyPath);

    private string? assemblyPath;
    public string AssemblyPath {
        get {
            if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
                return assemblyPath;

            string assemblyName = this.EvaluateProperty("AssemblyName");
            if (string.IsNullOrEmpty(assemblyName))
                assemblyName = SystemPath.GetFileNameWithoutExtension(Path);

            string debugPath = SystemPath.Combine(ProjectDirectory, "bin", "Debug");
            if (!Directory.Exists(debugPath))
                return string.Empty;

            var outputAssemblies = Directory.GetFiles(debugPath, $"{assemblyName}.dll", SearchOption.AllDirectories).Select(it => new FileInfo(it));
            assemblyPath = outputAssemblies.OrderByDescending(it => it.LastWriteTime).FirstOrDefault()?.FullName ?? string.Empty;
            return assemblyPath;
        }
    }

    private ProjectInfo(string path) : base(path) { }

    public static async Task<ProjectInfo?> GetProjectInfoAsync(DocumentUri uri) {
        string path = uri.GetFileSystemPath();
        string root = Directory.GetDirectoryRoot(path);
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