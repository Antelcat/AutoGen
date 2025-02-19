using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Antelcat.AutoGen.AssemblyWeavers;

public class AssemblyResolver : IAssemblyResolver
{
    private readonly Dictionary<string, string> referenceDictionary = [];
    private readonly TaskLogger                 logger;

    private readonly Dictionary<string, AssemblyDefinition> assemblyDefinitionCache =
        new(StringComparer.InvariantCultureIgnoreCase);

    public AssemblyResolver(TaskLogger logger, IEnumerable<string> splitReferences)
    {
        this.logger = logger;
        foreach (var filePath in splitReferences) referenceDictionary[GetAssemblyName(filePath)] = filePath;
    }

    private string GetAssemblyName(string filePath)
    {
        try
        {
            return GetAssembly(filePath, new(ReadingMode.Deferred)).Name.Name;
        }
        catch (Exception ex)
        {
            logger.LogDebug($"Could not load {filePath}, assuming the assembly name is equal to the file name: {ex}");
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }

    private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
    {
        if (assemblyDefinitionCache.TryGetValue(file, out var assembly))
        {
            return assembly;
        }

        parameters.AssemblyResolver ??= this;
        try
        {
            return assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
        }
        catch (Exception exception)
        {
            throw new($"Could not read '{file}'.", exception);
        }
    }

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference) =>
        Resolve(assemblyNameReference, new());

    public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference,
                                              ReaderParameters? parameters) =>
        referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences)
            ? GetAssembly(fileFromDerivedReferences, parameters ?? new())
            : null!;

    public void Dispose()
    {
        foreach (var value in assemblyDefinitionCache.Values)
        {
            value?.Dispose();
        }
    }
}