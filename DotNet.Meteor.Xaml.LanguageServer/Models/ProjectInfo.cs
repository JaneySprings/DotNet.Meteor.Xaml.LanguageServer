using OmniSharp.Extensions.LanguageServer.Protocol;

namespace DotNet.Meteor.Xaml.LanguageServer.Models;

public class ProjectInfo {
    public static async Task<ProjectInfo?> GetProjectInfoAsync(DocumentUri uri) {
        string path = uri.GetFileSystemPath();
        string root = Directory.GetDirectoryRoot(path);
        string? current = Path.GetDirectoryName(path);

        if (!File.Exists(path) || current == null)
            return null;

        var files = Array.Empty<FileInfo>();
        var info = await Task.Run(() => {
            while (root != current && files.Length == 0) {
                var directory = new DirectoryInfo(current!);
                files = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

                if (files.Length != 0)
                    break;

                current = Path.GetDirectoryName(current);
            }

            return files.Length != 0 ? new ProjectInfo(files.FirstOrDefault()?.FullName, current) : null;
        });

        return info;
    }

    ProjectInfo(string? projectPath, string? projectDirectory) {
        ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        ProjectDirectory = projectDirectory ?? throw new ArgumentNullException(nameof(projectDirectory));
    }

    /// <summary>
    /// Returns full project path
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Project directory path
    /// </summary>
    public string ProjectDirectory { get; }

    public string AssemblyPath() {
        string? path = string.Empty;

        string debugPath = Path.Combine(ProjectDirectory, "bin", "Debug");
        string assembly = Path.GetFileNameWithoutExtension(ProjectPath) + ".dll";

        if (Directory.Exists(debugPath)) {
            path = Directory.GetFiles(debugPath, assembly, SearchOption.AllDirectories).FirstOrDefault();
        }

        return path ?? string.Empty;
    }

    public bool IsAssemblyExist {
        get {
            string assemblyPath = AssemblyPath();
            return !string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath);
        }
    }

}