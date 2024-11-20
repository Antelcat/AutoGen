using System;
using System.Collections.Generic;
using System.Linq;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeaver.Weavers;

public record RecordPlaceboWeaver : IWeaver
{
    private ModuleDefinition ModuleDefinition;

    private AssemblyNameReference? system_Collections;
    private AssemblyNameReference System_Collections => system_Collections ??=
        ModuleDefinition.AssemblyReferences.Single(x => x.Name == "System.Collections");
    
    private AssemblyNameReference? system_Runtime;

    private AssemblyNameReference System_Runtime => system_Runtime ??=
        ModuleDefinition.AssemblyReferences.Single(x => x.Name == "System.Runtime");

    private ModuleDefinition? system_Collections_Module;

    private ModuleDefinition System_Collections_Module => system_Collections_Module ??=
        ModuleDefinition.AssemblyResolver.Resolve(System_Collections).MainModule;

    private (MethodReference Default_Get, MethodReference HashCode) Resolve(TypeReference type)
    {
        var generic = ModuleDefinition.ImportReference(typeof(EqualityComparer<>)).Resolve();
        var get_Default = ModuleDefinition.ImportReference(
            generic.Properties.First(x => x.Name == "Default").GetMethod);
        var getHashCode = ModuleDefinition.ImportReference(
            generic.Methods.First(x => x.Name == nameof(GetHashCode)));
        var instance = new GenericInstanceType(generic);
        instance.GenericArguments.Add(type);
        var reference = type.IsGenericParameter
            ? ModuleDefinition.ReflectionImporter.ImportReference(typeof(EqualityComparer<>), type)
            : ModuleDefinition.ImportReference(instance);

        //generic.module   = System_Collections_Module;
        generic.scope    = System_Collections;
        //reference.module = ModuleDefinition;
        reference.scope  = System_Collections;
        if (reference is GenericInstanceType genericInstance)
        {
            genericInstance.ElementType.scope  = System_Collections;
            //genericInstance.ElementType.module = System_Collections_Module;
        }
        
        get_Default.DeclaringType       = reference;
        get_Default.DeclaringType.scope = System_Collections;
        getHashCode.DeclaringType       = reference;
        getHashCode.DeclaringType.scope = System_Collections;
        return (get_Default, getHashCode);
    }

    public void Execute(AssemblyDefinition assembly)
    {
        ModuleDefinition = assembly.MainModule;
        var records = assembly.MainModule
            .Types
            .Where(x => x.IsRecord());
        foreach (var type in records)
        {
            var toString = type.Methods.FirstOrDefault(x =>
                x.IsCompilerGenerated() && x is { Name: nameof(ToString), Parameters.Count : 0 });
            var getHashCode = type.Methods.FirstOrDefault(x =>
                x.IsCompilerGenerated() && x is { Name: nameof(GetHashCode), Parameters.Count: 0 });

            if (toString is null && getHashCode is null) continue; // both been override

            var excludeProps = type.Properties.Where(x =>
                    x.GetAttributes<RecordExcludeAttribute>().Any())
                .ToList();
            var excludeFields = type.Fields.Where(x =>
                    x.GetAttributes<RecordExcludeAttribute>().Any())
                .ToList();
            if (excludeFields.Count + excludeProps.Count == 0) continue; // nothing to exclude

            Rewrite(type,
                type.Properties.First(static x => x.Name == nameof(EqualityContract)),
                type.Fields.Where(x => !excludeFields.Contains(x) &&
                                       (!x.IsBackingField() || !excludeProps.Contains(x.GetFrontingProperty()!))),
                toString,
                getHashCode);
        } 
    }

    private void Rewrite(TypeDefinition type,
                          PropertyDefinition equalityContractProp,
                                IEnumerable<FieldDefinition> qualifiedMembers,
                                MethodDefinition? toString,
                                MethodDefinition? getHashCode)
    {
        if (getHashCode is not null)
        {
            var il = getHashCode.Body.GetILProcessor();
            il.Clear();
            if (type.BaseType.Is(typeof(object)))
            {   
                // call EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract)
                var typeTuple = Resolve(ModuleDefinition.ImportReference(typeof(Type)));
                il.Emit(OpCodes.Call, typeTuple.Default_Get);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, equalityContractProp.GetMethod);
                il.Emit(OpCodes.Callvirt, typeTuple.HashCode);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call,
                    type.BaseType.Resolve().Methods
                        .First(x => x is { Name: nameof(GetHashCode), Parameters.Count: 0 }));
            }

            foreach (var field in qualifiedMembers)
            {
                var typeTuple = Resolve(ModuleDefinition.ImportReference(field.FieldType));
                il.Emit(OpCodes.Ldc_I4, -1521134295);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Call, typeTuple.Default_Get);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Callvirt, typeTuple.HashCode);
                il.Emit(OpCodes.Add);
            }
            il.Emit(OpCodes.Ret);

        }
    }

    public override string ToString() => GetType().Name;
}