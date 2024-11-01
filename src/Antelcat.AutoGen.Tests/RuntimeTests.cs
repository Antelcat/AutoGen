using System;
using System.Collections.Generic;

namespace Antelcat.AutoGen.Tests;

public class RuntimeTests
{
    public class Class
    {
        public int         A { get; set; }
        public string      B { get; set; }
        public Class       C { get; set; }
        public List<Class> D { get; set; }
    }

    public struct Struct
    {
        public int A { get; set; }
    }

    [Test]
    public void TestClone()
    {
        var c = new Class
        {
            A = 3,
            B = "b",
            C = null,
            D = [new Class { A = 4, C = new() }]
        };
    }
}