using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.ComponentModel.Mapping;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.AutoGen.Sample.Models.Mapping;

public class EntityBase
{
    public int Number { get; }

    public string MemberType
    {
        set { }
    }
}

public partial class InheritEntity : SampleEntity
{

}
public partial class SampleEntity : ObservableObject
{
    public int      Id    { get; set; }
    public string?  Name  { get; set; }
    public string?  Email { get; set; }
    public DateTime Time  { get; set; }

    [ObservableProperty]
    private string property;


    [AutoMap(Extra = [nameof(DoSomethingElse),])]
    [MapConstructor(nameof(Email))]
    [MapInclude(nameof(Property))]
    [MapExclude(nameof(Email))]
    [MapBetween(nameof(Time), nameof(SampleDto.DateTime), By = nameof(ToDateTime))]
    [return: MapDefault(nameof(SampleDto.Id), "")]
    public partial SampleDto ToDto();

    private static long ToDateTime(DateTime time)
        => time.ToFileTime();

    public static void DoSomethingElse(SampleDto target)
    {
        //TODO
    }

    [AutoMap]
    [MapExclude(nameof(Email))]
    public partial object ToAnonymous();

    void Fun([CallerFilePath] string path = "")
    {
        var directory       = (FilePath)path << 1;
        var full            = directory / "Antelcat.AutoGen.Sample" / "Example.cs";
        var changeExtension = full - 2 + ".g.cs";
        var list            = new List<string>();
        var (a, b, c, d) = list;
    }
}
public partial class SampleGeneric<T>
{
    public int      Id    { get; set; }
    public string?  Name  { get; set; }
    public string?  Email { get; set; }
    public DateTime Time  { get; set; }


    [AutoMap]
    public partial SampleGeneric<T> ToDto();
}