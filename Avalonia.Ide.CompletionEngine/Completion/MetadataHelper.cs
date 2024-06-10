using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Avalonia.Ide.CompletionEngine;

public class MetadataHelper
{
    private Metadata? _metadata;
    public Metadata? Metadata => _metadata;
    public Dictionary<string, string>? Aliases { get; private set; }

    private Dictionary<string, MetadataType>? _types;
    private string? _currentAssemblyName;
    private static Regex? _findElementByNameRegex;
    internal static Regex FindElementByNameRegex => _findElementByNameRegex ??=
            new($"\\s(?:(x\\:)?Name)=\"(?<AttribValue>[\\w\\:\\s\\|\\.]+)\"", RegexOptions.Compiled);

    public void SetMetadata(Metadata metadata, string xml, string? currentAssemblyName = null)
    {
        var aliases = GetNamespaceAliases(xml);

        //Check if metadata and aliases can be reused
        if (_metadata == metadata && Aliases != null && _types != null && currentAssemblyName == _currentAssemblyName)
        {
            if (aliases.Count == Aliases.Count)
            {
                var mismatch = false;
                foreach (var alias in aliases)
                {
                    if (!Aliases.ContainsKey(alias.Key) || Aliases[alias.Key] != alias.Value)
                    {
                        mismatch = true;
                        break;
                    }
                }

                if (!mismatch)
                    return;
            }
        }
        Aliases = aliases;
        _metadata = metadata;
        _types = null;
        _currentAssemblyName = currentAssemblyName;

        var types = new Dictionary<string, MetadataType>();
        foreach (var alias in Aliases.Concat(new[] { new KeyValuePair<string, string>("", "") }))
        {
            var aliasValue = alias.Value ?? "";

            if (!string.IsNullOrEmpty(_currentAssemblyName) && aliasValue.StartsWith("clr-namespace:") && !aliasValue.Contains(";assembly="))
                aliasValue = $"{aliasValue};assembly={_currentAssemblyName}";

            if (!metadata.Namespaces.TryGetValue(aliasValue, out var ns))
                continue;

            var prefix = alias.Key.Length == 0 ? "" : alias.Key + ":";
            foreach (var type in ns.Values)
                types[prefix + type.Name] = type;
        }

        _types = types;
    }


    public IEnumerable<KeyValuePair<string, MetadataType>> FilterTypes(string? prefix, bool withAttachedPropertiesOrEventsOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
    {
        if (_types is null)
        {
            return Array.Empty<KeyValuePair<string, MetadataType>>();
        }

        prefix ??= "";

        var e = _types
            .Where(t => t.Value.IsXamlDirective == xamlDirectiveOnly && t.Key.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Key.Equals("ControlTemplateResult") && !x.Key.Equals("DataTemplateExtensions"));
        if (withAttachedPropertiesOrEventsOnly)
            e = e.Where(t => t.Value.HasAttachedProperties || t.Value.HasAttachedEvents);
        if (markupExtensionsOnly)
            e = e.Where(t => t.Value.IsMarkupExtension);
        if (staticGettersOnly)
            e = e.Where(t => t.Value.HasStaticGetProperties);

        return e;
    }

    public IEnumerable<string> FilterTypeNames(string? prefix, bool withAttachedPropertiesOrEventsOnly = false, bool markupExtensionsOnly = false, bool staticGettersOnly = false, bool xamlDirectiveOnly = false)
    {
        return FilterTypes(prefix, withAttachedPropertiesOrEventsOnly, markupExtensionsOnly, staticGettersOnly, xamlDirectiveOnly).Select(s => s.Key);
    }

    public IEnumerable<string> FilterPropertyNames(string typeName, string? propName,
        bool? attached,
        bool hasSetter,
        bool staticGetter = false)
    {
        var t = LookupType(typeName);
        return MetadataHelper.FilterPropertyNames(t, propName, attached, hasSetter, staticGetter);
    }

    public static IEnumerable<string> FilterPropertyNames(MetadataType? t,
        string? propName,
        bool? attached,
        bool hasSetter,
        bool staticGetter = false)
    {
        return FilterProperty(t, propName, attached, hasSetter, staticGetter).Select(p => p.Name);
    }

    public static IEnumerable<MetadataProperty> FilterProperty(MetadataType? t, string? propName,
        bool? attached,
        bool hasSetter,
        bool staticGetter = false
        )
    {
        propName ??= "";
        if (t == null)
            return Array.Empty<MetadataProperty>();

        var e = t.Properties.Where(p => p.Name.Contains(propName, StringComparison.OrdinalIgnoreCase) && (hasSetter ? p.HasSetter : p.HasGetter));

        if (attached.HasValue)
            e = e.Where(p => p.IsAttached == attached);
        if (staticGetter)
            e = e.Where(p => p.IsStatic && p.HasGetter);
        else
            e = e.Where(p => !p.IsStatic);

        return e;
    }

    public IEnumerable<string> FilterEventNames(string typeName, string? propName, bool attached)
    {
        var t = LookupType(typeName);
        propName ??= "";
        if (t == null)
            return Array.Empty<string>();

        return t.Events.Where(n => n.IsAttached == attached && n.Name.Contains(propName, StringComparison.OrdinalIgnoreCase)).Select(n => n.Name);
    }

    public IEnumerable<string> FilterHintValues(MetadataType type, string? entered, string? currentAssemblyName, XmlParser? state)
    {
        entered ??= "";

        if (type == null)
            yield break;

        if (!string.IsNullOrEmpty(currentAssemblyName) && type.XamlContextHintValuesFunc != null)
        {
            foreach (var v in type.XamlContextHintValuesFunc(currentAssemblyName, type, null).Where(v => v.Contains(entered, StringComparison.OrdinalIgnoreCase)))
            {
                yield return v;
            }
        }

        if (type.HintValues is not null)
        {
            // Don't filter values here by 'StartsWith' (old behavior), provide all the hints,
            // For VS, Intellisense will filter the results for us, other users of the completion
            // engine (outside of VS) will need to filter later
            // Otherwise, in VS, it's impossible to get the full list for something like brushes:
            // Background="Red" -> Background="B", will only populate with the 'B' brushes and hitting
            // backspace after that will keep the 'B' brushes only instead of showing the whole list
            // WPF/UWP loads the full list of brushes and highlights starting at the B and then
            // filters the list down from there - otherwise its difficult to keep the completion list
            // and see all choices if making edits
            foreach (var v in type.HintValues)
            {
                yield return v;
            }
        }
    }


    public MetadataProperty? LookupProperty(string? typeName, string? propName)
        => LookupType(typeName)?.Properties?.FirstOrDefault(p => p.Name == propName);

    public MetadataType? LookupType(string? name)
    {
        if (name is null)
        {
            return null;
        }

        MetadataType? rv = null;
        if (!(_types?.TryGetValue(name, out rv) == true))
        {
            // Markup extensions used as XML elements will fail to lookup because
            // the tag name won't include 'Extension'
            _types?.TryGetValue($"{name}Extension", out rv);
        }
        return rv;
    }

    public static Dictionary<string, string> GetNamespaceAliases(string xml)
    {
        var rv = new Dictionary<string, string>();
        try
        {
            var xmlRdr = XmlReader.Create(new StringReader(xml));
            var result = true;
            while (result && xmlRdr.NodeType != XmlNodeType.Element)
            {
                try
                {
                    result = xmlRdr.Read();
                }
                catch
                {
                    if (xmlRdr.NodeType != XmlNodeType.Element)
                        result = false;
                }
            }

            if (result)
            {
                for (var c = 0; c < xmlRdr.AttributeCount; c++)
                {
                    xmlRdr.MoveToAttribute(c);
                    var ns = xmlRdr.Name;
                    if (ns != "xmlns" && !ns.StartsWith("xmlns:"))
                        continue;
                    ns = ns == "xmlns" ? "" : ns.Substring(6);
                    rv[ns] = xmlRdr.Value;
                }
            }
        }
        catch
        {
            //
        }
        if (!rv.ContainsKey(""))
            rv[""] = Utils.AvaloniaNamespace;
        return rv;
    }
}