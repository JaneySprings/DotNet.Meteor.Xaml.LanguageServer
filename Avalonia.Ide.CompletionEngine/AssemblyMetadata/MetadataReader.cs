using System;
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

    public Metadata? GetForTargetAssembly(string path)
    {
        if (!File.Exists(path))
            return null;

        using var session = _provider.GetMetadata(path);
        return MetadataConverter.ConvertMetadata(session);
    }
}
