using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Antelcat.AutoGen.ComponentModel;


[AutoKeyAccessor]
public partial interface INeedAccessor
{
    public string A => nameof(A);
}

public record NeedBase
{
    public virtual int D { get; set; }
}

[AutoKeyAccessor(memberTypes: MemberTypes.Property)]
public partial record NeedAccessor : NeedBase, INeedAccessor
{
    public int B { get; init; }

    public override int D { get; set; }

    private int d;
}

[AutoKeyAccessor]
public partial struct StructAlsoNeedAccessor
{
    public byte C
    {
        set { }
    }
}

public static class Test
{
    public static void Test1()
    {
        string a = (dynamic)new object();
        
        var accessor = new StructAlsoNeedAccessor
        {
            ["C"] = 1,
        };
    }
}

[AutoKeyEnumerable]
public partial class A
{
    public virtual string Props { get; set; } = ";";
    
    public string Some { get; set; }
}

[AutoKeyEnumerable(nameof(KeyProperty))]
[AutoKeyAccessor]
public partial class B : A
{
    public new string Props { get; }
}

public class C : B
{
    public new string Props => ((A)this).Props;
}