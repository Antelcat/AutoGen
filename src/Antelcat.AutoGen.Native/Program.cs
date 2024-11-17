using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
var a = new A();
var b = new B { A = a };
a.B = b;

var s = (a as object).GetHashCode();
Debugger.Break();

[AutoRecordPlacebo]
partial record A : O
{
    public B B { get; set; }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }

}


partial record B : O
{
    public A A { get; set; }
}

partial record O
{
}