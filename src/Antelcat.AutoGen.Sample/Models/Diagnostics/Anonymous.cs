using System.Reflection;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

[assembly: AutoTypeInference(
    Accessibility.Internal,
    Suffixes = ["Temp"],
    Kind = AutoTypeInferenceAttribute.TypeKind.Record,
    AttributeOnType = "[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]"
)]

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;

public class DeclaredClass
{
    public int Property { get; set; }

    public DeclaredClass Method()
    {
        return null;
    }
}

public class Anonymous
{
    public int? field;

    public void Test()
    {
        var local = () =>
        {
            return 3;
        };

        var declared = new DeclaredClass
        {
        };

        DeclaredClass dec = new()
        {
        };

        var temp = new AA.Temp
        {
        };

        AnonyTemp anony = new()
        {
            A = "",
            B = temp,
            C = dec.Property,
            D = field,
            E = dec.Method(),
            F = field is 0 ? 1 : 2,
            G = MemberTypes.Method,
            H = field++.Value,
            I = new DeclaredClass(),
            J = (DeclaredClass)new object(),
            K = new { Anonymouse = 1 },
            L = null,
            M = field ?? 2,
            N = local(),
            O = typeof(int)
        };

        var namespaceandclass = new Namespace.Then.Temp
        {
            A = "",
            B = temp
        };

        var global = new global::Namespace.Then.Temp
        {
            A = "",
            B = temp
        };
    }

    public interface IA;
    public class A : IA;

    public class CA : IA;

    public void Test2()
    {
        var a = new global::IA.Temp
        {
            A = new A()
        };
        var b =  new global::IA.Temp
        {
            A = new CA()
        };
        
    }
}