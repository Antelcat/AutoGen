using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using CommunityToolkit.Mvvm.ComponentModel;

var a = new A();
var b = new B { A = a };
a.B = b;

var s = (a as object).GetHashCode();
Debugger.Break();

[ObservableObject]
[AutoRecordPlacebo]
partial record A 
{
    public B B { get; set; }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }

    [ObservableProperty]
    private string read;

}


partial record B : O
{
    public A A { get; set; }
}

partial record O
{
}