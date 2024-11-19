using System;
using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.CodeAnalysis;

namespace Antelcat.AutoGen.SourceGenerators.Generators.Internal;

[Generator]
internal class NoneGenerator : IIncrementalGenerator
{
    static NoneGenerator()
    {
       
    }
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(c =>
        {
            
        });
    }
}