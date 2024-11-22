using Antelcat.AutoGen.ComponentModel.Diagnostic;

var a = new A();
var b = new B
{
    A = a
};
a.B = b;

Console.WriteLine(a.GetHashCode());
Console.WriteLine(b.GetHashCode());

record A
{
    [RecordIgnore]
    public B? B { get; set; }
}

record B
{
    public A? A { get; set; }
}

