using Antelcat.AutoGen.ComponentModel;

namespace Antelcat.AutoGen.Sample.Models.Accessor;

[AutoKeyAccessor]
public partial interface INeedAccessor
{
    public string A => nameof(A);
}

public class NeedBase
{
    public virtual int D { get; set; }
}

[AutoKeyAccessor(includeField: true, Get = false)]
public partial class NeedAccessor : NeedBase, INeedAccessor
{
    public int B { get; set; }

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