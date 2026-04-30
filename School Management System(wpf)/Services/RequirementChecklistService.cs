using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class RequirementChecklistService
    {
        private static readonly Regex StatusTokenRegex = new(@"\[STATUS:(?<status>[A-Z_]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] RequiredRequirementCatalog =
        {
            "Birth Certificate",
            "Good Moral Certificate",
            "Form 137 / Permanent Record",
            "Transfer Credentials",
            "Medical Certificate",
            "Parent/Guardian Consent"
        };

        public IReadOnlyList<string> GetRequiredRequirements()
        {
            return RequiredRequirementCatalog;
        }

        public RequirementChecklistSnapshot BuildForStudent(long studentId, IEnumerable<StudentRequirement> allRequirements)
        {
            var studentRequirements = (allRequirements ?? Enumerable.Empty<StudentRequirement>())
                .Where(x => x.StudentId == studentId)
                .ToList();

            var byNormalizedName = studentRequirements
                .GroupBy(x => NormalizeName(x.RequirementName))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id).First());

            var items = new List<RequirementChecklistItem>();
            foreach (var requiredName in RequiredRequirementCatalog)
            {
                var key = NormalizeName(requiredName);
                var entity = byNormalizedName.TryGetValue(key, out var found) ? found : null;
                items.Add(ToChecklistItem(requiredName, entity, isRequired: true));
            }

            var requiredKeys = RequiredRequirementCatalog.Select(NormalizeName).ToHashSet();
            var customItems = studentRequirements
                .Where(x => !requiredKeys.Contains(NormalizeName(x.RequirementName)))
                .OrderBy(x => x.RequirementName)
                .Select(x => ToChecklistItem(x.RequirementName, x, isRequired: false))
                .ToList();
            items.AddRange(customItems);

            var missingCount = items.Count(x => x.IsRequired && x.Status == RequirementChecklistStatus.MISSING);
            var submittedCount = items.Count(x => x.Status == RequirementChecklistStatus.SUBMITTED);
            var verifiedCount = items.Count(x => x.Status == RequirementChecklistStatus.VERIFIED);
            var rejectedCount = items.Count(x => x.Status == RequirementChecklistStatus.REJECTED);
            var expiredCount = items.Count(x => x.Status == RequirementChecklistStatus.EXPIRED);

            return new RequirementChecklistSnapshot
            {
                Items = items,
                RequiredCount = RequiredRequirementCatalog.Length,
                MissingCount = missingCount,
                SubmittedCount = submittedCount,
                VerifiedCount = verifiedCount,
                RejectedCount = rejectedCount,
                ExpiredCount = expiredCount,
                SummaryText = $"{RequiredRequirementCatalog.Length} required | {missingCount} missing | {verifiedCount} verified"
            };
        }

        public RequirementChecklistStatus ResolveStatus(StudentRequirement? entity)
        {
            if (entity == null)
            {
                return RequirementChecklistStatus.MISSING;
            }

            var tagged = ParseStatusTag(entity.Notes);
            if (tagged.HasValue)
            {
                return tagged.Value;
            }

            if (entity.IsSubmitted)
            {
                var notes = (entity.Notes ?? string.Empty).Trim();
                if (notes.IndexOf("verified", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return RequirementChecklistStatus.VERIFIED;
                }

                return RequirementChecklistStatus.SUBMITTED;
            }

            return RequirementChecklistStatus.MISSING;
        }

        public string BuildPersistedNotes(string? userNotes, RequirementChecklistStatus status)
        {
            var clean = StripStatusTag(userNotes);
            var statusToken = $"[STATUS:{status}]";
            if (string.IsNullOrWhiteSpace(clean))
            {
                return statusToken;
            }

            return $"{statusToken} {clean.Trim()}";
        }

        public string StripStatusTag(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return string.Empty;
            }

            return StatusTokenRegex.Replace(notes, string.Empty).Trim();
        }

        private RequirementChecklistItem ToChecklistItem(string displayName, StudentRequirement? entity, bool isRequired)
        {
            return new RequirementChecklistItem
            {
                RequirementId = entity?.Id,
                RequirementName = displayName,
                IsRequired = isRequired,
                Status = ResolveStatus(entity),
                SubmittedAt = entity?.SubmittedAt,
                Notes = StripStatusTag(entity?.Notes)
            };
        }

        private static RequirementChecklistStatus? ParseStatusTag(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            var match = StatusTokenRegex.Match(notes);
            if (!match.Success)
            {
                return null;
            }

            var raw = match.Groups["status"].Value.Trim();
            return Enum.TryParse<RequirementChecklistStatus>(raw, true, out var parsed) ? parsed : null;
        }

        private static string NormalizeName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }
    }
}
