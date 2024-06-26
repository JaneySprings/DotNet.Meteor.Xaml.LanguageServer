﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Avalonia.Ide.CompletionEngine.AssemblyMetadata;

public class MetadataReader
{
    private readonly IMetadataProvider _provider;

    public MetadataReader(IMetadataProvider provider)
    {
        _provider = provider;
    }

    private static IEnumerable<string> GetAssemblies(string path)
    {
        if (Path.GetDirectoryName(path) is not { } directory)
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(directory).Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            && !DepsJsonAssemblyListLoader.IsAssemblyBlacklisted(Path.GetFileName(f)));
    }

    public Metadata? GetForTargetAssembly(string path)
    {
        if (!File.Exists(path))
            return null;

        using var session = _provider.GetMetadata(MetadataReader.GetAssemblies(path));
        return MetadataConverter.ConvertMetadata(session);
    }
}
