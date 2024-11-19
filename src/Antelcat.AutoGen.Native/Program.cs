using System.Diagnostics;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

var a = new A();
var b = new B { A = a };
a.B = b;

var s = a.GetHashCode();
Debugger.Break();

record TempRecord<T>(T? Arg)
{
    public T? GetOnly => default;

    public None? SetOnly
    {
        set => value = default;
    }

    public T? Field;

    public Func<object>?       Delegate { get; set; }
    public event Func<object>? Event;
}


partial record A 
{
    public B B { get; set; }

    public override string ToString() => "";

}


partial record B() : O
{
    public A A { get; set; }

    public override string ToString() => "";
    
    public override int GetHashCode() => 1;

}

partial record O
{
}

record None
{
    
}