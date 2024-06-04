//Let's see what native aot can do


using Antelcat.AutoGen.Native;

Console.WriteLine(new DemoClass().GetType());

Console.WriteLine(typeof(DemoClass));


public struct A
{
    public object? this[string key]
    {
        get => "";
        set
        {
            
        }
    }
}