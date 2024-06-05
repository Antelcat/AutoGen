using Antelcat.AutoGen.ComponentModel;

namespace Antelcat.AutoGen.Sample.Models.Accessor;

[AutoKeyAccessor]
public partial interface INeedAccessor
{
    public string A => nameof(A);
}

public record NeedBase
{
    public virtual int D { get; set; }
}

[AutoKeyAccessor(includeField: true)]
public partial record NeedAccessor : NeedBase, INeedAccessor
{
    public int B { get; init; }

    public override int D { get; set; }

    private int d;
}

[AutoKeyAccessor]
public partial struct StructAlsoNeedAccessor
{
    public byte C
    {
        set { }
    }
}