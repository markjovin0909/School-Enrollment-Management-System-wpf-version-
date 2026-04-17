using System;
using System.Linq;

namespace School_Management_System.Services
{
    internal static class DataPrivacyService
    {
        public static string MaskPhone(string? value)
        {
            var input = (value ?? string.Empty).Trim();
            if (input.Length <= 4)
            {
                return input;
            }

            var tail = input[^4..];
            return new string('*', Math.Max(0, input.Length - 4)) + tail;
        }

        public static string MaskIdentifier(string? value, int keepTail = 4)
        {
            var input = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input) || input.Length <= keepTail)
            {
                return input;
            }

            var visible = input[^keepTail..];
            return new string('*', input.Length - keepTail) + visible;
        }

        public static string MaskName(string? value)
        {
            var input = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                parts[i] = part.Length <= 1 ? "*" : part[0] + new string('*', part.Length - 1);
            }

            return string.Join(' ', parts);
        }
    }
}
