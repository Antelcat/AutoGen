using System;
using System.Collections.Generic;

namespace Antelcat.AutoGen.AssemblyWeavers.Exceptions;

public class WeaverExceptions(IEnumerable<WeaverException> innerExceptions) : Exception
{
    public IEnumerable<WeaverException> Exceptions => innerExceptions;
}
