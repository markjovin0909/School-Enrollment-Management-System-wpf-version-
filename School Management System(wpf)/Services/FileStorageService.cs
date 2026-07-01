using System;
using System.IO;

namespace School_Management_System.Services
{
    internal static class FileStorageService
    {
        private const string ProfilesFolder = "storage\\profiles";
        private const string BrandingFolder = "storage\\branding";

        public static string? SaveProfileImage(string? sourcePath)
        {
            return SaveManagedImage(sourcePath, ProfilesFolder);
        }

        public static string? SaveSchoolLogo(string? sourcePath)
        {
            return SaveManagedImage(sourcePath, BrandingFolder);
        }

        public static string? GetAbsolutePath(string? storedPath)
        {
            return GetAbsolutePath(storedPath, ProfilesFolder);
        }

        public static string? GetSchoolLogoAbsolutePath(string? storedPath)
        {
            return GetAbsolutePath(storedPath, BrandingFolder);
        }

        public static void DeleteSchoolLogo(string? storedPath)
        {
            TryDeleteManagedFile(GetSchoolLogoAbsolutePath(storedPath));
        }

        public static string GetBrandingStorageDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BrandingFolder);
        }

        private static string? SaveManagedImage(string? sourcePath, string relativeFolder)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetDir = Path.Combine(baseDir, relativeFolder);
            Directory.CreateDirectory(targetDir);

            var ext = Path.GetExtension(sourcePath);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var targetPath = Path.Combine(targetDir, fileName);
            File.Copy(sourcePath, targetPath, true);

            return Path.Combine(relativeFolder, fileName);
        }

        private static string? GetAbsolutePath(string? storedPath, string relativeFolder)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return null;
            }

            if (Path.IsPathRooted(storedPath))
            {
                return storedPath;
            }

            if (storedPath.Contains('\\') || storedPath.Contains('/'))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, storedPath);
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFolder, storedPath);
        }

        private static void TryDeleteManagedFile(string? absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                return;
            }

            try
            {
                File.Delete(absolutePath);
            }
            catch
            {
                // Branding cleanup should not block save flows.
            }
        }
    }
}
