using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.ComponentModel.Diagnostics;
using Antelcat.AutoGen.Native;

[assembly:AutoRuntimeInfo(typeof(Parent),
                          MemberTypes = DynamicallyAccessedMemberTypes.All)]

namespace Antelcat.AutoGen.Native;

public class Parent
{
    public  bool PublicField;
    private bool privateField;

    public int Property { get; set; }

    public void Method()
    {
    }

    public Parent()
    {

    }
}