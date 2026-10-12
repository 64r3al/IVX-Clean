namespace IvxClean.Core.IO;

/// <summary>An entry yielded during a safe enumeration.</summary>
/// <param name="Path">The absolute path.</param>
/// <param name="IsDirectory">Whether the entry is a directory.</param>
public readonly record struct FsEntry(string Path, bool IsDirectory);

/// <summary>
/// Depth-first file/directory enumeration that never follows reparse points (junctions, symlinks,
/// mount points) and never throws on an inaccessible subtree — it skips it. Handles very large
/// trees and long paths.
/// </summary>
public static class SafeFileEnumerator
{
    /// <summary>
    /// Enumerates files (and, when <paramref name="includeDirectories"/> is set, directories)
    /// under <paramref name="root"/>. Reparse-point directories are yielded (so callers can see
    /// them) but never descended into.
    /// </summary>
    public static IEnumerable<FsEntry> Enumerate(string root, bool recursive = true, bool includeDirectories = true)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            string current = stack.Pop();

            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(current);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
            {
                continue; // skip an unreadable directory
            }

            foreach (string entry in entries)
            {
                FileAttributes attributes;
                try
                {
                    attributes = File.GetAttributes(entry);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or FileNotFoundException or DirectoryNotFoundException)
                {
                    continue;
                }

                bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                bool isReparse = (attributes & FileAttributes.ReparsePoint) != 0;

                if (isDirectory)
                {
                    if (includeDirectories)
                    {
                        yield return new FsEntry(entry, true);
                    }

                    if (recursive && !isReparse)
                    {
                        stack.Push(entry);
                    }
                }
                else
                {
                    yield return new FsEntry(entry, false);
                }
            }
        }
    }
}
