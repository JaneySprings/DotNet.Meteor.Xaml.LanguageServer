using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;

namespace Avalonia.Ide.CompletionEngine;

public static class MetadataConverter {

    private readonly static Regex extractType = new Regex("System.Nullable`1<(?<Type>.*)>|System.Nullable`1\\[\\[(?<Type>.*)]].*", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsMarkupExtension(ITypeInformation type) {
        var def = type;
        while (def != null) {
            if (def.GetInterfaces().Any(i => i?.Name == "IMarkupExtension"))
                return true;
            def = def.GetBaseType();
        }
        //in avalonia 0.9 there is no required base class, but convention only
        if (type.FullName.EndsWith("Extension") && type.Methods.Any(m => m.Name == "ProvideValue")) {
            return true;
        } else if (type.Name.Equals("OnPlatformExtension") || type.Name.Equals("OnFormFactorExtension")) {
            // Special case for this, as it the type info can't find the ProvideValue method
            return true;
        }
        return false;
    }
    public static MetadataType ConvertTypeInfomation(ITypeInformation type) {
        return new MetadataType(type.Name) {
            FullName = type.FullName,
            AssemblyQualifiedName = type.AssemblyQualifiedName,
            IsStatic = type.IsStatic,
            IsMarkupExtension = IsMarkupExtension(type),
            IsEnum = type.IsEnum,
            HasHintValues = type.IsEnum,
            IsGeneric = type.IsGeneric,
            IsAbstract = type.IsAbstract,
            HintValues = type.IsEnum ? type.EnumValues.ToArray() : null,
        };
    }
    public static Metadata ConvertMetadata(IMetadataReaderSession provider) {
        var types = new Dictionary<string, MetadataType>();
        var typeDefs = new Dictionary<MetadataType, ITypeInformation>();
        var metadata = new Metadata();
        var pseudoclasses = new HashSet<string>();
        var typepseudoclasses = new HashSet<string>();

        PreProcessTypes(types, metadata);

        var targetAssembly = provider.Assemblies.First();
        foreach (var asm in provider.Assemblies) {
            var aliases = new Dictionary<string, string[]>();
            ProcessCustomAttributes(asm, aliases);

            Func<ITypeInformation, bool> typeFilter = type => !type.IsInterface && type.IsPublic;

            if (asm.AssemblyName == provider.TargetAssemblyName || asm.InternalsVisibleTo.Any(att =>
                {
                    var endNameIndex = att.IndexOf(',');
                    var assemblyName = att;
                    var targetPublicKey = targetAssembly.PublicKey;
                    if (endNameIndex > 0)
                    {
                        assemblyName = att.Substring(0, endNameIndex);
                    }
                    if (assemblyName == targetAssembly.Name)
                    {
                        if (endNameIndex == -1)
                        {
                            return true;
                        }
                        var publicKeyIndex = att.IndexOf("PublicKey", endNameIndex, StringComparison.OrdinalIgnoreCase);
                        if (publicKeyIndex > 0)
                        {
                            publicKeyIndex += 9;
                            if (publicKeyIndex > att.Length)
                            {
                                return false;
                            }
                            while (publicKeyIndex < att.Length && att[publicKeyIndex] is ' ' or '=')
                            {
                                publicKeyIndex++;
                            }
                            if (targetPublicKey.Length == att.Length - publicKeyIndex)
                            {
                                for (int i = publicKeyIndex; i < att.Length; i++)
                                {
                                    if (att[i] != targetPublicKey[i - publicKeyIndex])
                                    {
                                        return false;
                                    }
                                }
                                return true;
                            }
                        }
                    }
                    return false;
                }))
            {
                typeFilter = type => type.Name != "<Module>" && !type.IsInterface && !type.IsAbstract;
            }

            var asmTypes = asm.Types.Where(typeFilter).ToArray();

            foreach (var type in asmTypes) 
            {
                var mt = types[type.FullName] = ConvertTypeInfomation(type);
                typeDefs[mt] = type;
                metadata.AddType("clr-namespace:" + type.Namespace + ";assembly=" + asm.Name, mt);
                string usingNamespace = $"using:{type.Namespace}";
                if (!aliases.TryGetValue(type.Namespace, out var nsAliases)) 
                {
                    nsAliases = new string[] { usingNamespace };
                    aliases[type.Namespace] = nsAliases;
                } 
                else if (!nsAliases.Contains(usingNamespace)) 
                {
                    aliases[type.Namespace] = nsAliases.Union(new string[] { usingNamespace }).ToArray();
                }

                foreach (var alias in nsAliases)
                    metadata.AddType(alias, mt);
            }
        }

        var at = types.Values.ToArray();
        foreach (var type in at)
        {
            typeDefs.TryGetValue(type, out var typeDef);

            var ctors = typeDef?.Methods
                .Where(m => m.IsPublic && !m.IsStatic && m.Name == ".ctor" && m.Parameters.Count == 1);

            if (typeDef?.IsEnum ?? false)
            {
                foreach (var value in typeDef.EnumValues)
                {
                    var p = new MetadataProperty(value, type, type, false, true, true, false);

                    type.Properties.Add(p);
                }
            }

            int level = 0;
            typepseudoclasses.Clear();

            type.TemplateParts = (typeDef?.TemplateParts ??
                Array.Empty<(ITypeInformation, string)>())
                .Select(item => (Type: ConvertTypeInfomation(item.Type), item.Name));

            while (typeDef != null)
            {
                foreach (var pc in typeDef.Pseudoclasses)
                {
                    typepseudoclasses.Add(pc);
                    pseudoclasses.Add(pc);
                }

                var currentType = types.GetValueOrDefault(typeDef.AssemblyQualifiedName);
                foreach (var prop in typeDef.Properties)
                {
                    if (!prop.IsVisbleTo(targetAssembly))
                        continue;

                    var propertyType = GetType(types, prop.TypeFullName, prop.QualifiedTypeFullName);

                    var p = new MetadataProperty(prop.Name, propertyType,
                        currentType, false, prop.IsStatic, prop.HasPublicGetter,
                        prop.HasPublicSetter);

                    type.Properties.Add(p);
                }

                foreach (var eventDef in typeDef.Events)
                {
                    if (!eventDef.IsPublic)
                        continue;
                    var e = new MetadataEvent(eventDef);
                    type.Events.Add(e);
                }

                foreach (var fieldDef in typeDef.Fields)
                {
                    if (!fieldDef.IsPublic)
                        continue;

                    var f = new MetadataField(fieldDef.Name, GetType(types, fieldDef.QualifiedTypeFullName),
                        types.GetValueOrDefault(typeDef.FullName, typeDef.AssemblyQualifiedName), false, fieldDef.IsStatic);

                    type.Fields.Add(f);
                }

                if (level == 0)
                {
                    foreach (var fieldDef in typeDef.Fields)
                    {
                        if (fieldDef.IsStatic && fieldDef.IsPublic)
                        {
                            //RoutedEvents are not supported in MAUI/Xamarin
                            if (fieldDef.Name.EndsWith("Property", StringComparison.OrdinalIgnoreCase)
                                && fieldDef.ReturnTypeFullName.StartsWith("Microsoft.Maui.Controls.BindableProperty")
                                )
                            {
                                var name = fieldDef.Name.Substring(0, fieldDef.Name.Length - "Property".Length);

                                IMethodInformation? setMethod = null;
                                IMethodInformation? getMethod = null;

                                var setMethodName = $"Set{name}";
                                var getMethodName = $"Get{name}";

                                foreach (var methodDef in typeDef.Methods)
                                {
                                    if (methodDef.Name.Equals(setMethodName, StringComparison.OrdinalIgnoreCase) && methodDef.IsStatic && methodDef.IsPublic
                                        && methodDef.Parameters.Count == 2)
                                    {
                                        setMethod = methodDef;
                                    }
                                    if (methodDef.IsStatic
                                        && methodDef.Name.Equals(getMethodName, StringComparison.OrdinalIgnoreCase)
                                        && methodDef.IsPublic
                                        && methodDef.Parameters.Count == 1
                                        && !string.IsNullOrEmpty(methodDef.ReturnTypeFullName)
                                        )
                                    {
                                        getMethod = methodDef;
                                    }
                                }

                                if (getMethod is not null)
                                {
                                    type.Properties.Add(new MetadataProperty(name,
                                        Type: types.GetValueOrDefault(getMethod.ReturnTypeFullName, getMethod.QualifiedReturnTypeFullName),
                                        DeclaringType: types.GetValueOrDefault(typeDef.FullName, typeDef.AssemblyQualifiedName),
                                        IsAttached: true,
                                        IsStatic: false,
                                        HasGetter: true,
                                        HasSetter: setMethod is not null));
                                }
                            }
                            else if (type.IsStatic)
                            {
                                type.Properties.Add(new MetadataProperty(fieldDef.Name, null, type, false, true, true, false));
                            }
                        }
                    }
                }

                if (typeDef.FullName == "Microsoft.Maui.Controls.BindableObject")
                {
                    type.IsBindableObjectType = true;
                }

                typeDef = typeDef.GetBaseType();
                level++;
            }

            type.HasAttachedProperties = type.Properties.Any(p => p.IsAttached);
            type.HasAttachedEvents = false;// not supported in MAUI/Xamarin
            type.HasStaticGetProperties = type.Properties.Any(p => p.IsStatic && p.HasGetter);
            type.HasSetProperties = type.Properties.Any(p => !p.IsStatic && p.HasSetter);
            if (typepseudoclasses.Count > 0)
            {
                type.HasPseudoClasses = true;
                type.PseudoClasses = typepseudoclasses.ToArray();
            }

            if (ctors?.Any() == true)
            {
                bool supportType = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Type");
                bool supportObject = ctors.Any(m => m.Parameters[0].TypeFullName == "System.Object" ||
                                                    m.Parameters[0].TypeFullName == "System.String");

                if ((types.TryGetValue(ctors.First().Parameters[0].QualifiedTypeFullName, out MetadataType? parType)
                    || types.TryGetValue(ctors.First().Parameters[0].QualifiedTypeFullName, out parType))
                        && parType.HasHintValues)
                {
                    type.SupportCtorArgument = MetadataTypeCtorArgument.HintValues;
                    type.HasHintValues = true;
                    type.HintValues = parType.HintValues;
                }
                else if (supportType && supportObject)
                    type.SupportCtorArgument = MetadataTypeCtorArgument.TypeAndObject;
                else if (supportType)
                    type.SupportCtorArgument = MetadataTypeCtorArgument.Type;
                else if (supportObject)
                    type.SupportCtorArgument = MetadataTypeCtorArgument.Object;
            }
        }

        PostProcessTypes(types, metadata, pseudoclasses);

        MetadataType? GetType(Dictionary<string, MetadataType> types, params string[] keys)
        {
            MetadataType? type = default;
            foreach (var key in keys)
            {
                if (types.TryGetValue(key, out type))
                {
                    break;
                }
                else if (key.StartsWith("System.Nullable`1", StringComparison.OrdinalIgnoreCase))
                {
                    var typeName = extractType.Match(key);
                    if (typeName.Success && types.TryGetValue(typeName.Groups[1].Value, out type))
                    {
                        type = new MetadataType(key)
                        {
                            AssemblyQualifiedName = type.AssemblyQualifiedName,
                            FullName = $"System.Nullable`1<{type.FullName}>",
                            IsNullable = true,
                            UnderlyingType = type
                        };
                        types.Add(key, type);
                        break;
                    }
                }
            }
            return type;
        }

        return metadata;
    }

    private static void ProcessCustomAttributes(IAssemblyInformation asm, Dictionary<string, string[]> aliases) {
        foreach (var attr in asm.CustomAttributes.Where(a => a.TypeFullName == "Microsoft.Maui.Controls.XmlnsDefinitionAttribute")) {
            var ns = attr.ConstructorArguments[1].Value?.ToString();
            var val = attr.ConstructorArguments[0].Value?.ToString();
            if (ns is null || val is null)
                continue;

            var current = new[] { val };
            if (aliases.TryGetValue(ns, out var allns))
                allns = allns.Union(current).Distinct().ToArray();

            aliases[ns] = allns ?? current;
        }
    }

    private static void PreProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata) {
        MetadataType xDataType, xCompiledBindings, boolType, typeType;
        var toAdd = new List<MetadataType> {
            (boolType = new MetadataType(typeof(bool).Name!) {
                FullName = typeof(bool).FullName!,
                HasHintValues = true,
                HintValues = new[] { "True", "False" }
            }),
            (typeType = new MetadataType(typeof(System.Type).Name!) {
                FullName = typeof(System.Type).FullName!,
            }),
        };

        foreach (var t in toAdd)
            types.Add(t.Name, t);

        var portableXamlExtTypes = new[] {
            new MetadataType("NullExtension") {
                HasSetProperties = true,
                IsMarkupExtension = true,
            },
            new MetadataType("Class") {
                IsXamlDirective = true
            },
            new MetadataType("Name") {
                IsXamlDirective = true
            },
            new MetadataType("Key") {
                IsXamlDirective = true
            },
            xDataType = new MetadataType("DataType") {
                IsXamlDirective = true,
                Properties = { new MetadataProperty("", typeType, null, false, false, false, true)},
            },
            xCompiledBindings = new MetadataType("CompileBindings") {
                IsXamlDirective = true,
                Properties = { new MetadataProperty("", boolType, null, false, false, false, true)},
            },
            new MetadataType("True") {
                IsMarkupExtension = true,
            },
            new MetadataType("False") {
                IsMarkupExtension = true,
            },
            new MetadataType("String") {
                IsMarkupExtension = true,
            },
        };

        //as in avalonia 0.9 Portablexaml is missing we need to hardcode some extensions
        foreach (var t in portableXamlExtTypes)
            metadata.AddType(Utils.Xaml2006Namespace, t);

        types.Add(xDataType.Name, xDataType);
        types.Add(xCompiledBindings.Name, xCompiledBindings);
    }

    private static void PostProcessTypes(Dictionary<string, MetadataType> types, Metadata metadata, HashSet<string> pseudoclasses) {
        var allProps = new Dictionary<string, MetadataProperty>();

        foreach (var type in types.Where(t => t.Value.IsBindableObjectType))
        {
            foreach (var v in type.Value.Properties.Where(p => p.HasSetter && p.HasGetter))
            {
                allProps[v.Name] = v;
            }
        }

        types.TryGetValue(typeof(Type).FullName!, out MetadataType? typeType);

        var dataContextType = new MetadataType("{BindingPath}")
        {
            FullName = "{BindingPath}",
            HasHintValues = true,
        };

        //bindings related hints
        if (types.TryGetValue("Microsoft.Maui.Controls.Xaml.BindingExtension", out MetadataType? bindingType))
        {
            bindingType.SupportCtorArgument = MetadataTypeCtorArgument.None;
            for (var i = 0; i < bindingType.Properties.Count; i++)
            {
                if (bindingType.Properties[i].Name == "Path"
                    || bindingType.Properties[i].Name == "FallbackValue"
                    || bindingType.Properties[i].Name == "TargetNullValue")
                {
                    bindingType.Properties[i] = bindingType.Properties[i] with
                    {
                        Type = dataContextType
                    };
                }
            }

            bindingType.Properties.Add(new MetadataProperty("", dataContextType, bindingType, false, false, true, true));
        }

        //typeExtension typeName hints
        if (types.TryGetValue("Microsoft.Maui.Controls.Xaml.TypeExtension", out MetadataType? typeExt))
        {
            if (typeExt.Properties.Count == 1 && typeExt.Properties[0].Name == "TypeName")
            {
                typeExt.Properties[0] = new MetadataProperty("TypeName", typeType, typeExt, false, false, true, true);
            }
        }

        // Style TargetType correction
        if (types.TryGetValue("Microsoft.Maui.Controls.Style", out MetadataType? styleType))
        {
            var targetTypeProp = styleType.Properties.FirstOrDefault(p => p.Name == "TargetType");
            if (targetTypeProp != null)
            {
                styleType.Properties.Remove(targetTypeProp);
                styleType.Properties.Add(targetTypeProp with { HasSetter = true });
            }
        }

        //colors
        if (types.TryGetValue("Microsoft.Maui.Graphics.Color", out MetadataType? colorType) &&
            types.TryGetValue("Microsoft.Maui.Graphics.Colors", out MetadataType? colors))
        {
            colorType.HasHintValues = true;
            colorType.HintValues = colors.Fields.Where(f => f.IsStatic).Select(f => f.Name).ToArray();
        }
        //brushes
        if (types.TryGetValue("Microsoft.Maui.Controls.Brush", out MetadataType? brushType))
        {
            brushType.HasHintValues = true;
            brushType.HintValues = brushType.Properties.Where(f => f.IsStatic).Select(f => f.Name).ToArray();
        }
        //easing
        if (types.TryGetValue("Microsoft.Maui.Easing", out MetadataType? easingType))
        {
            easingType.HasHintValues = true;
            easingType.HintValues = easingType.Fields.Where(f => f.IsStatic).Select(f => f.Name).ToArray();
        }
        //layout
        if (types.TryGetValue("Microsoft.Maui.Controls.LayoutOptions", out MetadataType? optionsType))
        {
            optionsType.HasHintValues = true;
            optionsType.HintValues = optionsType.Fields.Where(f => f.IsStatic).Select(f => f.Name).ToArray();
        }

        if (typeType != null)
        {
            var typeArguments = new MetadataType("TypeArguments")
            {
                IsXamlDirective = true,
                IsValidForXamlContextFunc = (a, t, p) => t?.IsGeneric == true,
                Properties = { new MetadataProperty("", typeType, null, false, false, false, true) }
            };

            metadata.AddType(Utils.Xaml2006Namespace, typeArguments);
        }
    }
}
