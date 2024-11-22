using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeaver.Weavers;

public record RecordPlaceboWeaver : IWeaver
{
    private ModuleDefinition module;

    private Lazy<AssemblyNameReference> System_Collections => new(() =>
        module.AssemblyReferences.Single(x => x.Name ==
#if NETSTANDARD
                                              "System.Collections"
#else
                                                "mscorlib"
#endif
        ));
    
    private AssemblyNameReference? system_Runtime;

    private AssemblyNameReference System_Runtime => system_Runtime ??=
        module.AssemblyReferences.Single(x => x.Name == "System.Runtime");

    private ModuleDefinition? system_Collections_Module;

    private ModuleDefinition System_Collections_Module => system_Collections_Module ??=
        module.AssemblyResolver.Resolve(System_Collections.Value).MainModule;

    private Lazy<MethodReference> EnsureSufficientExecutionStack => new(() =>
        module.ImportReference(typeof(RuntimeHelpers).GetMethod(
            nameof(RuntimeHelpers.EnsureSufficientExecutionStack)
            , BindingFlags.Public | BindingFlags.Static)!));

    private Lazy<MethodReference> StringBuilder_Append_String => new(() =>
        module.ImportReference(typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(string)])!));
    
    private Lazy<MethodReference> StringBuilder_Append_Object => new(() =>
        module.ImportReference(typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), [typeof(object)])!));

    private Lazy<MethodReference> Object_ToString => new(() =>
        module.ImportReference(typeof(object).GetMethod(nameof(ToString),
            BindingFlags.Instance | BindingFlags.Public)!));

    private (MethodReference Default_Get, MethodReference HashCode) Resolve(TypeReference type)
    {
        var generic = module.ImportReference(typeof(EqualityComparer<>)).Resolve();
        var get_Default = module.ImportReference(
            generic.Properties.First(x => x.Name == "Default").GetMethod!);
        var getHashCode = module.ImportReference(
            generic.Methods.First(x => x.Name == nameof(GetHashCode)));
        var instance = new GenericInstanceType(generic);
        instance.GenericArguments.Add(type);
        var reference = type.IsGenericParameter || type.ContainsGenericParameter
            ? module.ReflectionImporter.ImportReference(typeof(EqualityComparer<>), type)
            : module.ImportReference(instance);

        //generic.module   = System_Collections_Module;
        generic.scope    = System_Collections.Value;
        //reference.module = ModuleDefinition;
        reference.scope  = System_Collections.Value;
        if (reference is GenericInstanceType genericInstance)
        {
            genericInstance.ElementType.scope  = System_Collections.Value;
            //genericInstance.ElementType.module = System_Collections_Module;
        }
        
        get_Default.DeclaringType       = reference;
        get_Default.DeclaringType.scope = System_Collections.Value;
        getHashCode.DeclaringType       = reference;
        getHashCode.DeclaringType.scope = System_Collections.Value;
        return (get_Default, getHashCode);
    }

    public void Execute(AssemblyDefinition assembly)
    {
        module = assembly.MainModule;
        var records = assembly.MainModule
            .Types
            .Where(x => x.IsRecord());
        foreach (var type in records)
        {
            var printMembers = type.Methods.FirstOrDefault(static x =>
                x.IsCompilerGenerated() && x is
                {
                    Name      : nameof(PrintMembers),
                    Parameters: { Count: 1 } parameters
                } &&
                parameters[0].ParameterType.Is(typeof(StringBuilder)));
            var getHashCode = type.Methods.FirstOrDefault(static x =>
                x.IsCompilerGenerated() && x is { Name: nameof(GetHashCode), Parameters.Count: 0 });

            if (printMembers is null && getHashCode is null) continue; // both been overridden

            var excludeProps = type.Properties
                .Where(static x => x.HasAttribute<RecordIgnoreAttribute>() ||
                                   x.GetBackingField()?.HasAttribute<RecordIgnoreAttribute>() is true)
                .ToArray();
            var excludeFields = type.Fields
                .Where(static x => x.HasAttribute<RecordIgnoreAttribute>() || 
                                   x.GetFrontingProperty()?.HasAttribute<RecordIgnoreAttribute>() is true)
                .ToArray();
            if (excludeFields.Length + excludeProps.Length == 0) continue; // nothing to exclude

            Rewrite(type,
                type.Properties.First(static x => x.Name == nameof(EqualityContract)),
                excludeProps,
                excludeFields,
                printMembers,
                getHashCode);
        } 
    }

    private void Rewrite(TypeDefinition type,
                         PropertyDefinition equalityContract,
                         PropertyDefinition[] excludedProps,
                         FieldDefinition[] excludedFields,
                         MethodDefinition? printMembers,
                         MethodDefinition? getHashCode)
    {
        if (getHashCode is not null)
        {
            var il = getHashCode.Body.GetILProcessor();
            il.Clear();
            if (type.BaseType.Is(typeof(object)))
            {
                // call EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract)
                var typeTuple = Resolve(module.ImportReference(typeof(Type)));
                il.Emit(OpCodes.Call, typeTuple.Default_Get);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, equalityContract.GetMethod!);
                il.Emit(OpCodes.Callvirt, typeTuple.HashCode);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call,
                    module.ImportReference(type.BaseType.Resolve().Methods
                        .First(x => x is { Name: nameof(GetHashCode), Parameters.Count: 0 })));
            }

            foreach (var field in type.Fields.Except(excludedFields))
            {
                var typeTuple = Resolve(module.ImportReference(field.FieldType));
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

        if (printMembers is not null)
        {
            var il = printMembers.Body.GetILProcessor();
            il.Clear();
            if (type.BaseType.Is(typeof(object)))
            {
                il.Emit(OpCodes.Call, EnsureSufficientExecutionStack.Value);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, module.ImportReference(type.BaseType.Resolve().Methods.First(x =>
                    x is
                    {
                        Name      : nameof(PrintMembers),
                        Parameters: { Count: 1 } parameters
                    } &&
                    parameters[0].ParameterType.Is(typeof(StringBuilder)))));

                il.Emit(OpCodes.Brfalse_S, new Instruction(0x15, OpCodes.Ldarg_1));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, ", ");
                il.Emit(OpCodes.Callvirt, StringBuilder_Append_String.Value);
                il.Emit(OpCodes.Pop);
            }
            foreach (var (member, index) in type.Properties
                .Where(x => x.GetMethod is not null)
                .Except([equalityContract])
                .Except(excludedProps)
                .Cast<IMemberDefinition>()
                .Concat(type.Fields.Where(x => !x.IsBackingField() && !excludedFields.Contains(x)))
                .Select((x, i) => (x, i)))
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, $"{(index is 0 ? "" : ", ")}{member.Name} = ");
                il.Emit(OpCodes.Callvirt, StringBuilder_Append_String.Value);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                switch (member)
                {
                    case PropertyDefinition property:
                        il.Emit(OpCodes.Call, property.GetMethod!);
                        EmitToAppend(property.PropertyType);
                        break;
                    case FieldDefinition field:
                        il.Emit(OpCodes.Ldfld, field);
                        EmitToAppend(field.FieldType);
                        break;
                    default:
                        throw new Exception($"watt is that {member.Name}?");
                }

                continue;

                void EmitToAppend(TypeReference argType)
                {
                    if (!argType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, argType);
                        il.Emit(OpCodes.Callvirt, StringBuilder_Append_Object.Value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc_0);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);
                        il.Emit(OpCodes.Constrained, argType);
                        il.Emit(OpCodes.Callvirt, Object_ToString.Value);
                        il.Emit(OpCodes.Callvirt, StringBuilder_Append_String.Value);
                    }
                    il.Emit(OpCodes.Pop);
                }
            }
            
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);
        }
    }

    public override string ToString() => GetType().Name;
}