using System;

namespace Antelcat.AutoGen.ComponentModel;

[Flags]
public enum Accessibility
{
    Public    = 0x1,
    Internal  = 0x2,
    Protected = 0x4,
    Private   = 0x8,
}