using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeavers.Weavers;

public record RecordPlaceboWeaver : IWeaver
{
    private ModuleDefinition module;

    
    [field: AllowNull, MaybeNull]
    private TypeReference EqualityComparer => field ??=
        module.TryGetTypeReference(typeof(EqualityComparer<>).FullName!, out var reference)
            ? reference
            : throw new KeyNotFoundException(typeof(EqualityComparer<>).FullName);

    [field: AllowNull, MaybeNull]
    private TypeDefinition EqualityComparerDefinition => field ??= EqualityComparer.Resolve();

    [field: AllowNull, MaybeNull]
    private MethodReference EnsureSufficientExecutionStack
    {
        get
        {
            if (field is not null) return field;
            field = module.ImportReference(module.TryGetTypeReference(typeof(RuntimeHelpers), out var reference)
                    ? reference
                        .Resolve()
                        .Methods
                        .First(x => x.Name == nameof(RuntimeHelpers.EnsureSufficientExecutionStack))
                    : throw new NullReferenceException(nameof(RuntimeHelpers)));
            switch (module.RuntimeFramework())
            {
                case RuntimeFramework.NET:
                    field.DeclaringType.Scope = module.AssemblyReferences.ByName("System.Runtime");
                    break;
                case RuntimeFramework.NET_Standard:
                    field.DeclaringType.Scope = module.AssemblyReferences.NETStandard();
                    break;
            }
            return field;
        }
    }

    [field: AllowNull, MaybeNull]
    private TypeDefinition StringBuilderDefinition => field ??= module
        .GetTypeReference(typeof(StringBuilder))!
        .Resolve();

    [field: AllowNull, MaybeNull]
    private MethodReference StringBuilder_Append_String => field ??= module.ImportReference(
        StringBuilderDefinition
            .Methods
            .First(x => x.Name == nameof(StringBuilder.Append) &&
                        x.Parameters.First().ParameterType.Is(typeof(string))));

    [field: AllowNull, MaybeNull]
    private MethodReference StringBuilder_Append_Object => field ??= module.ImportReference(
        StringBuilderDefinition
            .Methods
            .First(x => x.Name == nameof(StringBuilder.Append) &&
                        x.Parameters.First().ParameterType.Is(typeof(object))));

    [field: AllowNull, MaybeNull]
    private MethodReference Object_ToString => field ??= module.ImportReference(module
            .GetTypeReference(typeof(object))!
            .Resolve()
            .Methods
            .First(x => x.Name == nameof(object.ToString) &&
                        x.Parameters.Count is 0));

    private (MethodReference Default_Get, MethodReference HashCode) Resolve(TypeReference type)
    {
        var generic = EqualityComparer;
        var genericDef = EqualityComparerDefinition;
        var getDefault = module.ImportReference(
            genericDef.Properties.First(x => x.Name == "Default").GetMethod);
        var getHashCode = module.ImportReference(
            genericDef.Methods.First(x => x.Name == nameof(GetHashCode)));
        TypeReference containingType;
        if (type.IsGenericParameter || type.ContainsGenericParameter)
        {
            containingType = module.MetadataImporter.ImportReference(generic, type);
        }
        else
        {
            var instance = new GenericInstanceType(generic);
            instance.GenericArguments.Add(type);
            containingType = module.ImportReference(instance);
        }
        
        // EqualityComparer<T> EqualityComparer<MemberType>::get_Default() 
        getDefault.DeclaringType                     = containingType;
        getDefault.ReturnType.GetElementType().scope = containingType.Scope;
        // int32 EqualityComparer<MemberType>::GetHashCode(T) 
        getHashCode.DeclaringType = containingType;
       
        return (getDefault, getHashCode);
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
                var typeTuple = Resolve(module.GetTypeReference(typeof(Type))!);
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
                il.Emit(OpCodes.Call, EnsureSufficientExecutionStack);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, module.ImportReference(type.BaseType
                    .Resolve()
                    .Methods
                    .First(x =>
                        x is
                        {
                            Name      : nameof(PrintMembers),
                            Parameters: { Count: 1 } parameters
                        } &&
                        parameters[0].ParameterType.Is(typeof(StringBuilder)))));

                il.Emit(OpCodes.Brfalse_S, new Instruction(0x15, OpCodes.Ldarg_1));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, ", ");
                il.Emit(OpCodes.Callvirt, StringBuilder_Append_String);
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
                il.Emit(OpCodes.Callvirt, StringBuilder_Append_String);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_0);
                switch (member)
                {
                    case PropertyDefinition property:
                        il.Emit(OpCodes.Call, property.GetMethod);
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
                        il.Emit(OpCodes.Callvirt, StringBuilder_Append_Object);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc_0);
                        il.Emit(OpCodes.Ldloca_S, (byte)0);
                        il.Emit(OpCodes.Constrained, argType);
                        il.Emit(OpCodes.Callvirt, Object_ToString);
                        il.Emit(OpCodes.Callvirt, StringBuilder_Append_String);
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