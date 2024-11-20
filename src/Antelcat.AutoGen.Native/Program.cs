using System.Diagnostics;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

var a = new A();
var b = new B { A = a };
a.B = b;

Console.WriteLine(a.GetHashCode());
Console.WriteLine(b.GetHashCode());

record A 
{
    public B B { get; set; }

    public override string ToString() => "";
}


record B
{
    [RecordExclude]
    public A A { get; set; }

    public override string ToString() => "";

}
