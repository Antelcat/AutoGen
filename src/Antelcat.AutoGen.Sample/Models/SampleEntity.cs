using System;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.AutoGen.ComponentModel.Mapping;
using Antelcat.AutoGen.Sample.Models;

namespace Antelcat;

public partial class SampleEntity
{
    public int     Id    { get; set; }
    public string? Name  { get; set; }
    public string? Email { get; set; }
    public DateTime    Time  { get; set; }

    
    [AutoMap(Extra = [nameof(DoSomethingElse)])]
    [MapConstructor(nameof(Email))]
    [MapBetween(nameof(Time),nameof(SampleDto.DateTime), By = nameof(ToDateTime))]
    [MapExclude(nameof(Email))]
    public partial SampleDto ToDto();

    private long ToDateTime(DateTime time)
        => time.ToFileTime();

    public void DoSomethingElse(SampleDto target)
    {
        //TODO
    }
    
    [AutoReport]
    internal partial void Report(AutoReport.ReportHandler handler);

    void Fun()
    {
        FilePath path = "";
    }

}