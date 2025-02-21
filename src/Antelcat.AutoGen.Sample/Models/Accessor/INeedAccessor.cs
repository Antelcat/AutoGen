﻿using System;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel;


#if !NETSTANDARD2_0
[AutoKeyAccessor]
#endif
public partial interface INeedAccessor
{
    public string A { get; }
}

public record NeedBase
{
    public virtual int D { get; set; }
}

[AutoKeyAccessor(memberTypes: MemberTypes.Property)]
public partial record NeedAccessor(string A) : NeedBase, INeedAccessor
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
        string a = 
            #if !NETSTANDARD2_0
            (dynamic)
            #else
            (string)
            #endif
            new object();
        
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

[AutoKeyEnumerable(nameof(KeyProperty), GenerateType = MemberTypes.Property)]
[AutoKeyAccessor]
public partial class B : A
{
    public new string Props { get; }

    private string some;

    public void Export()
    {
        foreach (var key in KeyProperty)
        {
            Console.WriteLine($"My {key} is {this[key]}");
        }
    }
}

public class C : B
{
    public new string Props => ((A)this).Props;
}