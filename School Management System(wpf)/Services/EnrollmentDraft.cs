namespace School_Management_System.Services
{
    internal sealed class EnrollmentDraft
    {
        public long SchoolYearId { get; set; }
        public long StudentId { get; set; }
        public long SectionId { get; set; }
        public long CurriculumId { get; set; }
        public string? EnrollmentType { get; set; }
        public string? Notes { get; set; }
    }
}
