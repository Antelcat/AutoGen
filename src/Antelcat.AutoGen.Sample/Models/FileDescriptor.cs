﻿using System.IO;
using Antelcat.AutoGen.ComponentModel.Mapping;

namespace Antelcat.AutoGen.Sample.Models;

public class FileDescriptor
{
    public required string FullName { get; set; }
    public          long   Length   { get; init; }
}

public static partial class Extension
{
}