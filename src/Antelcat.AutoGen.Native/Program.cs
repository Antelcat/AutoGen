using System.Diagnostics;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
[assembly: AutoRecordPlacebo]

var a = new A();
var b = new B { A = a };
a.B = b;

var s = a.GetHashCode();
Debugger.Break();

partial record A : O
{
    public B B { get; set; }
}


partial record B : O
{
    public A A { get; set; }
}

partial record O
{
}