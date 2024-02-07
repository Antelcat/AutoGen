using System;
using System.IO;

namespace Antelcat.AutoGen.SourceGenerators;

[Literal($"{nameof(Antelcat)}.{nameof(AutoGen)}.{nameof(FilePath)}")]
public readonly ref struct FilePath
{
    public FilePath(string path) => this.path = path;
    private readonly string path;

    /// <summary>
    /// <see cref="System.IO.Path.GetFullPath"/>
    /// </summary>
    public FilePath FullPath => Path.GetFullPath(path);

    /// <summary>
    /// <see cref="System.IO.Path.GetFileName"/>
    /// </summary>
    public string FileName => Path.GetFileName(path);

    /// <summary>
    /// <see cref="System.IO.Path.GetDirectoryName"/>
    /// </summary>
    public string? DirectoryName => Path.GetDirectoryName(path);

    /// <summary>
    /// <see cref="System.IO.Path.GetFileNameWithoutExtension"/>
    /// </summary>
    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// <see cref="System.IO.Path.GetPathRoot"/>
    /// </summary>
    public string PathRoot => Path.GetPathRoot(path);

    /// <summary>
    /// <see cref="System.IO.Path.GetExtension"/>
    /// </summary>
    public string Extension => Path.GetExtension(path);

    public override string ToString() => path;

    public static FilePath operator /(FilePath left, FilePath right) => Path.Combine(left, right);
    public static FilePath operator /(FilePath left, string right) => Path.Combine(left, right);
    public static FilePath operator /(FilePath left, string[] right) => Path.Combine([left, ..right]);
    public static FilePath operator /(string left, FilePath right) => Path.Combine(left, right);
    public static FilePath operator /(string[] left, FilePath right) => Path.Combine([..left, right]);

    public static FilePath operator +(FilePath left, FilePath right) => left.ToString()       + right.ToString();
    public static FilePath operator +(FilePath left, string right) => left.ToString()         + right;
    public static FilePath operator +(string left, FilePath right) => left                    + right.ToString();
    public static FilePath operator +(FilePath left, string[] right) => left.ToString()       + string.Join("", right);
    public static FilePath operator +(string[] left, FilePath right) => string.Join("", left) + right.ToString();

    public static FilePath operator -(FilePath path, int count) => path.ToString()[..^count];

    public static FilePath operator *(FilePath path, int times)
    {
        var result = path.ToString();
        while (times-- > 0)
        {
            result += path.ToString();
        }

        return result;
    }

    public static implicit operator FilePath(string path) => new(path);
    public static implicit operator FilePath(string[] paths) => Path.Combine(paths);
    public static implicit operator string(FilePath path) => path.ToString();
}