using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Antelcat.AutoGen.AssemblyWeavers.Weavers;

public class RecordPlaceboWeaver : Weaver
{
    [field: AllowNull, MaybeNull]
    private ModuleDefinition Module => field ??= AssemblyDefinition.MainModule;

    [field: AllowNull, MaybeNull]
    private TypeReference EqualityComparer => field ??=
        Module.TryGetTypeReference(typeof(EqualityComparer<>).FullName!, out var reference)
            ? reference
            : throw new KeyNotFoundException(typeof(EqualityComparer<>).FullName);

    [field: AllowNull, MaybeNull]
    private TypeDefinition EqualityComparerDefinition => field ??= EqualityComparer.Resolve();

    [field: AllowNull, MaybeNull]
    private MethodReference EnsureSufficientExecutionStack => field ??=
        Module.ImportReference(Module.TryGetTypeReference(typeof(RuntimeHelpers), out var reference)
                ? reference
                    .Resolve()
                    .Methods
                    .First(static x => x.Name == nameof(RuntimeHelpers.EnsureSufficientExecutionStack))
                : throw new NullReferenceException(nameof(RuntimeHelpers)))
            .Revise(Module, static (m, a) =>
            {
                m.DeclaringType.Scope = a;
            });

    [field: AllowNull, MaybeNull]
    private TypeDefinition StringBuilderDefinition => field ??= Module
        .GetTypeReference(typeof(StringBuilder))!
        .Resolve();

    [field: AllowNull, MaybeNull]
    private MethodReference StringBuilder_Append_String => field ??= Module.ImportReference(
            StringBuilderDefinition
                .Methods
                .First(static x => x.Name == nameof(StringBuilder.Append) &&
                                   x.Parameters.FirstOrDefault()?.ParameterType.Is(typeof(string)) is true))
        .Revise(Module, static (m, a) =>
        {
            m.DeclaringType.Scope = a;
            m.ReturnType.Scope    = a;
        });

    [field: AllowNull, MaybeNull]
    private MethodReference StringBuilder_Append_Object => field ??= Module.ImportReference(
            StringBuilderDefinition
                .Methods
                .First(static x => x.Name == nameof(StringBuilder.Append) &&
                                   x.Parameters.FirstOrDefault()?.ParameterType.Is(typeof(object)) is true))
        .Revise(Module, static (m, a) =>
        {
            m.DeclaringType.Scope = a;
            m.ReturnType.Scope    = a;
        });

    [field: AllowNull, MaybeNull]
    private MethodReference Object_ToString => field ??= Module.ImportReference(Module
            .GetTypeReference(typeof(object))!
            .Resolve()
            .Methods
            .First(static x => x.Name == nameof(object.ToString) &&
                               x.Parameters.Count is 0))
        .Revise(Module, static (m, a) =>
        {
            m.DeclaringType.Scope = a;
        });


    private (MethodReference DefaultGet, MethodReference HashCode) Resolve(TypeReference type)
    {
        var generic    = EqualityComparer;
        var genericDef = EqualityComparerDefinition;
        var getDefault = Module.ImportReference(
            genericDef.Properties.First(static x => x.Name == "Default").GetMethod);
        var getHashCode = Module.ImportReference(
            genericDef.Methods.First(static x => x.Name == nameof(GetHashCode)));
        var instance = new GenericInstanceType(generic);
        instance.GenericArguments.Add(type);
        var containingType = Module.ImportReference(instance);

        // EqualityComparer<T> EqualityComparer<MemberType>::get_Default() 
        getDefault.DeclaringType                     = containingType;
        getDefault.ReturnType.GetElementType().scope = containingType.Scope;

        // int32 EqualityComparer<MemberType>::GetHashCode(T) 
        getHashCode.DeclaringType = containingType;

        return (getDefault, getHashCode);
    }

    public override bool FilterMainModuleType(TypeDefinition typeDefinition) => typeDefinition.IsRecord();

    public override void Execute(IReadOnlyList<TypeDefinition> typeDefinitions)
    {
        foreach (var type in typeDefinitions)
        {
            var printMembers = type.Methods.FirstOrDefault(PrintMembers);
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

        if (Module.RuntimeFramework() != RuntimeFramework.NET) return;
        try
        {
            var privateCorLib = Module.AssemblyReferences.PrivateCoreLib();
            Module.AssemblyReferences.Remove(privateCorLib);
        }
        catch
        {
            //
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
                var typeTuple = Resolve(Module.GetTypeReference(typeof(Type))!);
                il.Emit(OpCodes.Call, typeTuple.DefaultGet);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, equalityContract.GetMethod);
                il.Emit(OpCodes.Callvirt, typeTuple.HashCode);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call,
                    Module.ImportReference(type.BaseType.Resolve().Methods
                        .First(x => x is { Name: nameof(GetHashCode), Parameters.Count: 0 })));
            }

            foreach (var field in type.Fields.Except(excludedFields))
            {
                var typeTuple = Resolve(Module.ImportReference(field.FieldType));
                il.Emit(OpCodes.Ldc_I4, -1521134295);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Call, typeTuple.DefaultGet);
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
                il.Emit(OpCodes.Call, Module.ImportReference(type.BaseType
                    .Resolve()
                    .Methods
                    .First(PrintMembers)));

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

    private static bool PrintMembers(MethodDefinition method) =>
        method.IsCompilerGenerated() && method is
        {
            Name      : nameof(PrintMembers),
            Parameters: { Count: 1 } parameters
        } &&
        parameters[0].ParameterType.Is(typeof(StringBuilder));

    private static Type EqualityContract => typeof(RecordPlaceboWeaver);
}