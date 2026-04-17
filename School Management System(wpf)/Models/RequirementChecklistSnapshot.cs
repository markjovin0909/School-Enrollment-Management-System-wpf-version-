using System.Collections.Generic;

namespace School_Management_System.Models
{
    public sealed class RequirementChecklistSnapshot
    {
        public List<RequirementChecklistItem> Items { get; init; } = new();
        public int RequiredCount { get; init; }
        public int MissingCount { get; init; }
        public int SubmittedCount { get; init; }
        public int VerifiedCount { get; init; }
        public int RejectedCount { get; init; }
        public int ExpiredCount { get; init; }
        public string SummaryText { get; init; } = string.Empty;
    }
}
