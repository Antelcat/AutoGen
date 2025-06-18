using System;
using System.Diagnostics;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;

public class Demo
{
    public int    Number { get; set; }
    public string Name   { get; set; }
}

public class CustomScript : MetadataScript
{
    [MetadataScript(typeof(Demo))]
    public override object? Execute(params object[] Value)
    {
        var sb = new StringBuilder("public class New_Demo {");
        foreach (var property in (Value[0] as Type).GetProperties())
        {
            sb.AppendLine($"public {property.PropertyType.FullName} New_{property.Name} {{ get; set; }}");
        }

        return sb.Append("}");
    }

    public void Test()
    {
        Debug.Write(nameof(New_Demo.New_Name));
        Debug.Write(nameof(New_Demo.New_Number));
    }
}