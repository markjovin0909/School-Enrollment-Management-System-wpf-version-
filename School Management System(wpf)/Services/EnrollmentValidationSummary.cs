using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class EnrollmentValidationSummary
    {
        public long SchoolYearId { get; set; }
        public long StudentId { get; set; }
        public long SectionId { get; set; }
        public string SchoolYearName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string EnrollmentType { get; set; } = "NEW";
        public EnrollmentStatus SuggestedStatus { get; set; } = EnrollmentStatus.PENDING;
        public bool SchoolYearOpen { get; set; }
        public bool DuplicateEnrollmentExists { get; set; }
        public bool RequirementsComplete { get; set; }
        public bool SectionHasCapacity { get; set; }
        public int? WaitlistPosition { get; set; }
        public int CurrentSectionEnrolled { get; set; }
        public int? SectionCapacity { get; set; }
        public List<string> Messages { get; } = new();

        public bool CanSubmit =>
            SchoolYearOpen &&
            !DuplicateEnrollmentExists &&
            Messages.All(m => !m.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase));

        public string ToDisplayText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Enrollment Validation Summary");
            builder.AppendLine($"School Year: {SchoolYearName}");
            builder.AppendLine($"Student: {StudentName}");
            builder.AppendLine($"Section: {SectionName}");
            builder.AppendLine($"Student Type: {EnrollmentType}");
            builder.AppendLine($"Suggested Status: {SuggestedStatus}");
            builder.AppendLine($"Enrollment Window Open: {(SchoolYearOpen ? "Yes" : "No")}");
            builder.AppendLine($"Duplicate Enrollment Found: {(DuplicateEnrollmentExists ? "Yes" : "No")}");
            builder.AppendLine($"Requirements Complete: {(RequirementsComplete ? "Yes" : "No")}");
            builder.AppendLine($"Section Capacity: {(SectionCapacity.HasValue ? $"{CurrentSectionEnrolled}/{SectionCapacity}" : "No limit")}");
            if (!SectionHasCapacity && WaitlistPosition.HasValue)
            {
                builder.AppendLine($"Waitlist Position: #{WaitlistPosition.Value}");
            }

            if (Messages.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Checks:");
                foreach (var message in Messages)
                {
                    builder.AppendLine($"- {message}");
                }
            }

            return builder.ToString().TrimEnd();
        }
    }
}
