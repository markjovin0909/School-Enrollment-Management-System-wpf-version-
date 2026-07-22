using System.Collections.Generic;
using System.Linq;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal static class SchoolYearSelectionHelper
    {
        public static SchoolYear? ResolveActive(IEnumerable<SchoolYear> schoolYears, SchoolYearService schoolYearService)
        {
            var years = schoolYears?.ToList() ?? new List<SchoolYear>();
            if (years.Count == 0)
            {
                return schoolYearService.GetActiveSchoolYear();
            }

            var active = schoolYearService.GetActiveSchoolYear();
            if (active != null)
            {
                var match = years.FirstOrDefault(y => y.Id == active.Id);
                if (match != null)
                {
                    return match;
                }
            }

            return years.FirstOrDefault(y => y.Status == SchoolYearStatus.ACTIVE) ?? years.FirstOrDefault();
        }

        public static long? ResolveActiveId(IEnumerable<SchoolYear> schoolYears, SchoolYearService schoolYearService)
            => ResolveActive(schoolYears, schoolYearService)?.Id;
    }
}