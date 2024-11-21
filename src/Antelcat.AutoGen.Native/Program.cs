using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

var a = new A();
var b = new B { A = a };
a.B = b;

Console.WriteLine(a.GetHashCode());
Console.WriteLine(b.GetHashCode());
Console.WriteLine(a);
Console.WriteLine(b);
Debugger.Break();

record A
{
    public B B { get; set; }
}


record B
{
    [RecordIgnore] public A A { get; set; }

}

record Base
{
    public Record<int> R { get; set; }
}

record Record<T>(T Arg) : Antelcat.AutoGen.Sample.Models.Diagnostics.C
{
    public int GetOnly { get; }
    public int GetOnly2 { get; }
    public object GetOnly3 { get; }

    public nint SetOnly
    {
        set { }
    }

    public T Prop { get; set; }

    public T Field;

    public event Func<T> Event;
}