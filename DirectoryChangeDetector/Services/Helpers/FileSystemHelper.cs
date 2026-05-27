using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DirectoryChangeDetector.Services.Helpers;

internal static class FileSystemHelper
{
    /// <summary>
    /// Resolves the real path of a directory, following symlinks to detect cycles.
    /// </summary>
    public static string ResolveRealPath(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            var target = dirInfo.ResolveLinkTarget(returnFinalTarget: true);
            return target?.FullName ?? Path.GetFullPath(path);
        }
        catch
        {
            return Path.GetFullPath(path);
        }
    }

    /// <summary>
    /// Computes an SHA-256 hash of the file content.
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }
    
    /// <summary>
    /// Returns a normalized relative path using forward slashes.
    /// </summary>
    public static string GetRelativePath(string rootPath, string fullPath)
    {
        return Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
    }

    /// <summary>
    /// Generates a deterministic snapshot file path based oon the directory path
    /// </summary>
    public static string GetSnapshotFileName(string directoryPath)
    {
        var normalizedPath = directoryPath.ToLowerInvariant()
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"snapshot_{hashString}.json";
    }
}