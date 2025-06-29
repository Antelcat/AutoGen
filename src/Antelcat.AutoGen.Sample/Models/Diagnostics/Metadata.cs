using System;
using System.Linq;
using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace Antelcat.AutoGen.Sample.Models.Diagnostics;

public partial class Demo
{
    public void SampleMethod() { }
    
    public int Number { get; set; }
}

public class CustomScript
{
    [MetadataScript(typeof(Demo))]
    public object? WhatEverItCalls(Type type)
    {
        var sb = new StringBuilder("//this is a generated class\n")
            .AppendLine("namespace Antelcat.AutoGen.Sample.Models.Diagnostics;")
            .AppendLine("public partial class Demo{");
        foreach (var method in (type).GetMethods().Where(x=>!x.IsSpecialName))
        {
            sb.AppendLine("public System.Windows.Input.ICommand " + method.Name +
                          "Command" + " => "+ 
                          $" new CommunityToolkit.Mvvm.Input.RelayCommand({method.Name});");
        }

        return sb.AppendLine("}");
    }
}