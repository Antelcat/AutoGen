global using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;
using Antelcat.AutoGen;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

[assembly: AutoStringTo]
[assembly: AutoFilePath]
[assembly: Antelcat.AutoGen.ComponentModel.
    AutoDeconstructIndexable(16, typeof(Foo<>))]


public static class General
{
    public static FilePath Dir([CallerFilePath] string path = "") => path;
}

namespace Antelcat.AutoGen
{
    public class Foo
    {
        public object this[int index]
        {
            get => null!;
            set { }
        }
    }

    [AutoWatch]
    public class Foo<T> where T : class
    {
        public Foo<T> Type = null!;
        public T this[int index]
        {
            get => null!;
        }
    }
}