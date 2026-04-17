using System;

namespace School_Management_System.Models
{
    public sealed class RequirementChecklistItem
    {
        public long? RequirementId { get; init; }
        public string RequirementName { get; init; } = string.Empty;
        public RequirementChecklistStatus Status { get; init; }
        public string StatusText => Status.ToString();
        public bool IsRequired { get; init; }
        public DateTime? SubmittedAt { get; init; }
        public string Notes { get; init; } = string.Empty;
    }
}
