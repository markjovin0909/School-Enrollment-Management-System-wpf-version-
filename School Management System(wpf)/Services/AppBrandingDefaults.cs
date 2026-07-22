namespace School_Management_System.Services
{
    /// <summary>
    /// Product branding taken from the official eTinun-an logo artwork.
    /// </summary>
    internal static class AppBrandingDefaults
    {
        public const string AppName = "eTinun-an";
        public const string Tagline = "Digital Learners Management System";
        public const string SchoolCode = "ETN";
        public const string StudentNumberPrefix = "ETN";
        public const string PrintHeaderLine1 = AppName;
        public const string PrintHeaderLine2 = Tagline;

        /// <summary>
        /// Bundled logo file names, preferred first.
        /// </summary>
        public static readonly string[] LogoFileNames =
        {
            "Logo.png",
            "Logo.jpg",
            "Logo.jpeg"
        };

        public static readonly string[] RelativeLogoPaths =
        {
            "Assets/Logo.png",
            "Assets/Logo.jpg",
            "Assets/Logo.jpeg"
        };

        public static bool IsUnsetOrLegacySchoolName(string? schoolName)
        {
            if (string.IsNullOrWhiteSpace(schoolName))
            {
                return true;
            }

            var trimmed = schoolName.Trim();
            return string.Equals(trimmed, "School Management System", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "SMS", System.StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveSchoolName(string? configuredName)
        {
            return IsUnsetOrLegacySchoolName(configuredName)
                ? AppName
                : configuredName!.Trim();
        }
    }
}
