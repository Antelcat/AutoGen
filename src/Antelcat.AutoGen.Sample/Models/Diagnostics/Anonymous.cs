using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: global::Antelcat.AutoGen.ComponentModel.Diagnostic.AutoTypeInference]

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
        var declared = new DeclaredClass
        {
            
        };

        DeclaredClass dec = new()
        {

        };
        
        var temp = new Temp
        {
            
        };

        Anony anony = new()
        {
            A = "",
            B = temp,
            C = dec.Property,
            D = field,
            E = dec.Method(),
            F = field is 0 ? 1 : 2,
            G = MemberTypes.Method,
            H = field++,
            I = new DeclaredClass(),
            J = (DeclaredClass)new object(),
            K = new { Anonymouse = 1 },
            L = null
        };

        var namespaceandclass = new Namespace.Class()
        {
            A = "",
            B = temp
        };
        
        var global = new global::Namespace.Class()
        {
            A = "",
            B = temp
        };
    }
}