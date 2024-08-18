//Let's see what native aot can do

using System.Reflection;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.Native;

[assembly: AutoDeconstructIndexable(16, typeof(Foo))]

object parent = new Parent();

Console.WriteLine(parent.GetType()
                      .GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                      .Length);


public class Foo
{
    public object? this[int index] => null;
    
}
