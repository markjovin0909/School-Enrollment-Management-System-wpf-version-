using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace School_Management_System.Services
{
    internal sealed class SchoolBrandingService
    {
        private readonly SchoolSettingService _schoolSettingService = new();

        public SchoolBrandingSnapshot GetCurrentBranding()
        {
            var setting = _schoolSettingService.GetLatest();
            var schoolName = AppBrandingDefaults.ResolveSchoolName(setting?.SchoolName);
            var schoolCode = string.IsNullOrWhiteSpace(setting?.SchoolCode)
                ? AppBrandingDefaults.SchoolCode
                : setting!.SchoolCode.Trim();

            var logoPath = ResolveLogoAbsolutePath(setting?.SchoolLogoPath);
            return new SchoolBrandingSnapshot(
                schoolName,
                schoolCode,
                setting?.SchoolAddress?.Trim() ?? string.Empty,
                setting?.PrincipalName?.Trim() ?? string.Empty,
                logoPath,
                LoadImage(logoPath));
        }

        public string GetDefaultLogoAbsolutePath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var relative in AppBrandingDefaults.RelativeLogoPaths)
            {
                var candidate = Path.Combine(baseDir, relative.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            var current = new DirectoryInfo(baseDir);
            while (current != null)
            {
                foreach (var fileName in AppBrandingDefaults.LogoFileNames)
                {
                    var candidate = Path.Combine(current.FullName, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    var assetsCandidate = Path.Combine(current.FullName, "Assets", fileName);
                    if (File.Exists(assetsCandidate))
                    {
                        return assetsCandidate;
                    }
                }

                current = current.Parent;
            }

            // Last resort path used by callers for diagnostics; LoadImage also tries pack URI.
            return Path.Combine(baseDir, "Assets", "Logo.png");
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
            if (!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))
            {
                var fileBitmap = new BitmapImage();
                fileBitmap.BeginInit();
                fileBitmap.CacheOption = BitmapCacheOption.OnLoad;
                fileBitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                fileBitmap.EndInit();
                fileBitmap.Freeze();
                return fileBitmap;
            }

            // Fallback: pack resource (WPF Resource item).
            foreach (var relative in AppBrandingDefaults.RelativeLogoPaths)
            {
                try
                {
                    var packUri = new Uri($"pack://application:,,,/{relative.Replace('\\', '/')}", UriKind.Absolute);
                    var resourceInfo = Application.GetResourceStream(packUri);
                    if (resourceInfo?.Stream == null)
                    {
                        continue;
                    }

                    using (resourceInfo.Stream)
                    {
                        var packBitmap = new BitmapImage();
                        packBitmap.BeginInit();
                        packBitmap.CacheOption = BitmapCacheOption.OnLoad;
                        packBitmap.StreamSource = resourceInfo.Stream;
                        packBitmap.EndInit();
                        packBitmap.Freeze();
                        return packBitmap;
                    }
                }
                catch
                {
                    // Try next candidate.
                }
            }

            // Transparent 1x1 placeholder so UI never crashes if logo is missing.
            var placeholder = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { 0, 0, 0, 0 },
                4);
            placeholder.Freeze();
            return placeholder;
        }
    }

    internal sealed record SchoolBrandingSnapshot(
        string SchoolName,
        string SchoolCode,
        string SchoolAddress,
        string PrincipalName,
        string LogoAbsolutePath,
        ImageSource LogoImage);
}
