using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

#if NETSTANDARD
using StrongNameKeyPair = Mono.Cecil.StrongNameKeyPair;
#else
using StrongNameKeyPair = System.Reflection.StrongNameKeyPair;
#endif

namespace Antelcat.AutoGen.AssemblyWeavers;

public class StrongKeyFinder(IWaveArguments waveArguments, ModuleDefinition module, TaskLogger logger)
{
    public StrongNameKeyPair? StrongNameKeyPair;
    public byte[]?            PublicKey;

    public void FindStrongNameKey()
    {
        var path = GetKeyFilePath();
        if (path == null) return;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"KeyFilePath was defined but file does not exist. '{path}'.");
        }

        var fileBytes = File.ReadAllBytes(path);

        if (!waveArguments.DelaySign)
        {
            try
            {
                logger.LogDebug("Extract public key from key file for signing.");

                StrongNameKeyPair = new(fileBytes);
                // Ensure that we can generate the public key from the key file. This requires the private key to
                // work. If we cannot generate the public key, an ArgumentException will be thrown. In this case,
                // the assembly is delay-signed with a public only key-file.
                // Note: The NETSTANDARD implementation of StrongNameKeyPair.PublicKey does never throw here.
                PublicKey = StrongNameKeyPair.PublicKey;
                return;
            }
            catch (ArgumentException)
            {
                logger.LogWarning("Failed to extract public key from key file, fall back to delay-signing.");
            }
        }

        // Fall back to delay signing, this was the original behavior, however that does not work in NETSTANDARD (s.a.)
        logger.LogDebug("Prepare public key for delay-signing.");

        // We know that we cannot sign the assembly with this key-file. Let's assume that it is a public
        // only key-file and pass along all the bytes.
        StrongNameKeyPair = null;
        PublicKey         = fileBytes;
    }

    private string? GetKeyFilePath()
    {
        if (waveArguments.AssemblyOriginatorKeyFile != null)
        {
            var keyFilePath = Path.GetFullPath(waveArguments.AssemblyOriginatorKeyFile);
            logger.LogDebug($"Using strong name key from KeyFilePath '{keyFilePath}'.");
            return keyFilePath;
        }

        var assemblyKeyFileAttribute = module
            .Assembly
            .CustomAttributes
            .FirstOrDefault(static attribute => attribute.AttributeType.Name == nameof(AssemblyKeyFileAttribute));
        if (assemblyKeyFileAttribute != null)
        {
            var keyFileSuffix = (string)assemblyKeyFileAttribute.ConstructorArguments.First().Value;
            var path          = Path.Combine(waveArguments.IntermediateDirectory!, keyFileSuffix);
            logger.LogDebug($"Using strong name key from [AssemblyKeyFileAttribute(\"{keyFileSuffix}\")] '{path}'");
            return path;
        }

        logger.LogDebug("No strong name key found");
        return null;
    }
}