using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Antelcat.AutoGen.ComponentModel;

[assembly: AutoObjectClone]

namespace Antelcat.AutoGen.Native;

public class Class
{
    public int Int { get; set; }

    public string? String { get; set; }

    private Func<string>?             Delegate;
    private event Func<string>?       Event;
    public Class?                     ObjectRef     { get; set; }
    public List<Class>?               CollectionRef { get; set; }
    public Dictionary<string, Class>? DictionaryRef { get; set; }



    public override string ToString() => JsonSerializer.Serialize(this, ClassSerializerContext.Default.Class);

    private static Class Origin => new()
    {
        Int    = 1,
        String = "123",
        ObjectRef = new Class
        {
            Int    = 2,
            String = "???"
        },
        Delegate = () => "?",
        CollectionRef =
        [
            new()
            {
                Int    = 3,
                String = "!!!"
            }
        ],
        DictionaryRef = new()
        {
            {
                "1", new()
                {
                    Int = 4
                }
            }
        }
    };

    public static void RunTest()
    {
        var origin = Origin;
        origin.Event += () => "!";

        

        var watch = new Stopwatch();
        watch.Start();
        var cloned = origin.DeepClone();
        var time   = watch.ElapsedTicks;
        Console.WriteLine($"Clone : {time}");

        Console.WriteLine("origin :");
        watch.Restart();
        var originStr = origin.ToString();
        time = watch.ElapsedTicks;
        Console.WriteLine($"Serialize : {time}");
        Console.WriteLine(originStr);
        Console.WriteLine($"{nameof(Equals)} : {origin == cloned}");
        Console.WriteLine("cloned :");

        var clonedStr = cloned.ToString();
        Console.WriteLine(clonedStr);

        Console.WriteLine($"Json Equals : {originStr == clonedStr}");
    }

    public static void RunTime()
    {
        var origin = Origin;
        var watch  = new Stopwatch();

        var typeInfo = ClassSerializerContext.Default.Class;
        watch.Restart();
        JsonSerializer.Deserialize(JsonSerializer.Serialize(origin, typeInfo), typeInfo);
        var time = watch.ElapsedTicks;
        Console.WriteLine($"Serialize : {time}");
        
        watch.Restart();
        origin.DeepClone();
        time = watch.ElapsedTicks;
        Console.WriteLine($"Clone : {time}");
    }
}

[JsonSerializable(typeof(Class))]
[JsonSerializable(typeof(IEnumerable<Class>))]
[JsonSerializable(typeof(List<Class>))]
[JsonSerializable(typeof(Dictionary<string, Class>))]
[JsonSerializable(typeof(KeyValuePair<string, Class>))]
public partial class ClassSerializerContext : JsonSerializerContext;