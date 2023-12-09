using System;
using Antelcat.AutoGen.ComponentModel.Abstractions;

namespace Antelcat.AutoGen.ComponentModel.Entity;


public class GenerateMapBetweenAttribute(Type one, Type another) : GenerateAttribute
{
    /// <summary>
    /// Extra mapper actions
    /// </summary>
    public string[]? Extra { get; set; }
}
