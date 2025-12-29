#if NET48
using System;
using System.IO;

namespace FTPSheep.BuildTools.Compatibility;

/// <summary>
/// Provides Path.GetRelativePath for .NET Framework 4.8
/// </summary>
internal static class PathHelper
{
    /// <summary>
    /// Returns a relative path from one path to another.
    /// </summary>
    public static string GetRelativePath(string relativeTo, string path)
    {
        if (string.IsNullOrEmpty(relativeTo))
            throw new ArgumentNullException(nameof(relativeTo));
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var relativeToUri = new Uri(GetFullPath(relativeTo) + Path.DirectorySeparatorChar);
        var pathUri = new Uri(GetFullPath(path));

        if (relativeToUri.Scheme != pathUri.Scheme)
        {
            return path; // Cannot make relative path
        }

        var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }
}
#endif
