using System;
using System.IO;

namespace School_Management_System.Services
{
    internal static class FileStorageService
    {
        private const string ProfilesFolder = "storage\\profiles";

        public static string? SaveProfileImage(string? sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetDir = Path.Combine(baseDir, ProfilesFolder);
            Directory.CreateDirectory(targetDir);

            var ext = Path.GetExtension(sourcePath);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var targetPath = Path.Combine(targetDir, fileName);
            File.Copy(sourcePath, targetPath, true);

            // Store only the key (file name) to keep DB values short and portable.
            return fileName;
        }

        public static string? GetAbsolutePath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return null;
            }

            if (Path.IsPathRooted(storedPath))
            {
                return storedPath;
            }

            // Backward-compatible with older stored relative paths like "storage\\profiles\\file.jpg".
            if (storedPath.Contains('\\') || storedPath.Contains('/'))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, storedPath);
            }

            // New format: just the file key/name.
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesFolder, storedPath);
        }
    }
}
