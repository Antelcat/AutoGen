using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace Antelcat.AutoGen.Sample.Models;

[AutoExtractInterface(ReceiveGeneric = [1,0], 
    PassGeneric = ["0", "0,1"], 
    Interfaces = [typeof(IList<>),typeof(IDictionary<,>)])]
public class WaitingForInterface<T1, T2, T3>(object ord) : I 
    where T1 : unmanaged
    where T2 : IList<T1> 
{
    public T1 GenericProp
    {
        get { return default; }
        set { }
    }

    public object InitProp { get; set; } = ord;

    public List<string> GetOnlyProp => null;

    public List<string> SetOnlyProp
    {
        private get => null;
        set { _ = value; }
    }

    public async Task<T1> ExistGenericMethod(IList<T1> arg) => throw new NotImplementedException();

    public void ExtraGenericMethod<T>()
    {
    }
    

    public List<string> NoneGenericMethod() => throw new NotImplementedException();

    public event Func<object> EventProperty
    {
        add { }
        remove { }
    }

    public event Func<object>? EventField, A;
    public int                 Get { get; set; }
}

public interface I
{
    public event Func<object> EventProperty;
    public event Func<object> EventField;

    internal int Get { get;  set; }

    internal List<string> NoneGenericMethod();
}