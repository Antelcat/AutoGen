//Let's see what native aot can do

using Antelcat.AutoGen.ComponentModel;

[assembly: AutoDeconstructIndexable(16, typeof(Foo))]


var foo = new Foo();

// 传统方式
var first  = foo[0];
var second = foo[1];
var third  = foo[2];
var forth  = foo[3];
Console.WriteLine($"{first} {second} {third} {forth}");

var (a, b, c, d) = foo; //新的方式
Console.WriteLine($"{a} {b} {c} {d}");


public class Foo
{
    public object? this[int index] => null;
}
