using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace School_Management_System.Services
{
    internal sealed class SchoolBrandingService
    {
        private const string DefaultLogoRelativePath = "Assets/Logo.jpg";
        private const string WorkspaceLogoFileName = "Logo.jpg";
        private readonly SchoolSettingService _schoolSettingService = new();

        public SchoolBrandingSnapshot GetCurrentBranding()
        {
            var setting = _schoolSettingService.GetAll().OrderByDescending(x => x.Id).FirstOrDefault();
            var schoolName = string.IsNullOrWhiteSpace(setting?.SchoolName)
                ? "School Management System"
                : setting!.SchoolName.Trim();

            var logoPath = ResolveLogoAbsolutePath(setting?.SchoolLogoPath);
            return new SchoolBrandingSnapshot(
                schoolName,
                setting?.SchoolCode?.Trim() ?? string.Empty,
                logoPath,
                LoadImage(logoPath));
        }

        public string GetDefaultLogoAbsolutePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var bundledPath = Path.Combine(baseDir, DefaultLogoRelativePath);
            if (File.Exists(bundledPath))
            {
                return bundledPath;
            }

            var current = new DirectoryInfo(baseDir);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, WorkspaceLogoFileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            return bundledPath;
        }

        public string ResolveLogoAbsolutePath(string? logoPath)
        {
            var configured = FileStorageService.GetSchoolLogoAbsolutePath(logoPath);
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                return configured;
            }

            return GetDefaultLogoAbsolutePath();
        }

        public ImageSource LoadLogoImage(string? logoPath)
        {
            return LoadImage(ResolveLogoAbsolutePath(logoPath));
        }

        private static ImageSource LoadImage(string absolutePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }

    internal sealed record SchoolBrandingSnapshot(
        string SchoolName,
        string SchoolCode,
        string LogoAbsolutePath,
        ImageSource LogoImage);
}
