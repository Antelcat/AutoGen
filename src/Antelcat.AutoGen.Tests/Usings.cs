global using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen;
using Antelcat.AutoGen.ComponentModel;
using Microsoft.CodeAnalysis;

[assembly: AutoStringTo]
[assembly: AutoFilePath]
[assembly: AutoDeconstructIndexable(16, typeof(Foo), typeof(Foo<object>), typeof(Foo<>))]
[assembly: AutoObjectClone]

public static class General
{
    public static FilePath Dir([CallerFilePath] string path = "") => path;
}

namespace Antelcat.AutoGen
{
    public class Foo
    {
        public object? this[int index]
        {
            get => null!;
            set { }
        }
    }

    public interface Foo<TA> where TA : class, new()
    {
        public TA this[int index] { get; set; }
    }

    public class Demo
    {
        public Demo()
        {
            Foo<Demo> list = null!;
            var (a, b, c, d, e, f, g, h) = list;
        }
    }
}