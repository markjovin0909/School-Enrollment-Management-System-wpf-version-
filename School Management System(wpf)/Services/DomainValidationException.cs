using System;
using System.Collections.Generic;
using System.Linq;

namespace School_Management_System.Services
{
    internal sealed class DomainValidationException : Exception
    {
        public DomainValidationException(string message)
            : this(message, null)
        {
        }

        public DomainValidationException(string message, IEnumerable<string>? details)
            : base(message)
        {
            Details = details?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        public IReadOnlyList<string> Details { get; }
    }
}
