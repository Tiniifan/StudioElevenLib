using System;
using System.IO;
using System.Linq;

namespace StudioElevenLib.Tools
{
    public static class VirtualPath
    {
        /// <summary>
        /// Computes an absolute path based on a current path and a target path.
        /// Understands `..` (parent) and `.` (current).
        /// </summary>
        public static string GetAbsolutePath(string currentPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath)) return currentPath;

            string basePath = targetPath.StartsWith("/") ? "/" : currentPath;
            var segments = basePath.Split('/', (char)StringSplitOptions.RemoveEmptyEntries).ToList();

            var targetSegments = targetPath.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
            foreach (var seg in targetSegments)
            {
                if (seg == ".") continue;
                if (seg == "..")
                {
                    if (segments.Count > 0)
                        segments.RemoveAt(segments.Count - 1);
                }
                else
                {
                    segments.Add(seg);
                }
            }

            return "/" + string.Join("/", segments);
        }

        /// <summary>
        /// Splits a normalized absolute path into its parent segment and the final name.
        /// </summary>
        public static (string parentPath, string name) SplitLast(string absolutePath)
        {
            string path = absolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(path)) return ("/", "");

            int slash = path.LastIndexOf('/');
            return slash <= 0
                ? ("/", path.Substring(slash + 1))
                : (path.Substring(0, slash), path.Substring(slash + 1));
        }

        /// <summary>
        /// Navigates to the VirtualDirectory at the target absolute path.
        /// </summary>
        public static VirtualDirectory ResolveDir(VirtualDirectory root, string absolutePath)
        {
            var current = root;
            var segments = absolutePath.Split('/', (char)StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                current = current.GetFolder(segment)
                          ?? throw new DirectoryNotFoundException($"Virtual directory '{segment}' not found.");
            }
            return current;
        }

        /// <summary>
        /// Resolves the parent VirtualDirectory and the final name component.
        /// </summary>
        public static (VirtualDirectory parent, string name) ResolveParent(VirtualDirectory root, string absolutePath)
        {
            var (parentPath, name) = SplitLast(absolutePath);
            return (ResolveDir(root, parentPath), name);
        }
    }
}
