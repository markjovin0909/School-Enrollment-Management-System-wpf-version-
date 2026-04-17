using System;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Configuration;
using School_Management_System.Models;

namespace School_Management_System.Data
{
    internal class AppDbContext : DbContext
    {
        internal static Action<DbContextOptionsBuilder>? TestOptionsConfigurator { get; set; }

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<SchoolYear> SchoolYears { get; set; }
        public DbSet<GradeLevel> GradeLevels { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Curriculum> Curricula { get; set; }
        public DbSet<CurriculumSubject> CurriculumSubjects { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<ClassOffering> ClassOfferings { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SchoolSetting> SchoolSettings { get; set; }
        public DbSet<StudentRequirement> StudentRequirements { get; set; }
        public DbSet<ArchiveRecord> ArchiveRecords { get; set; }
        public DbSet<EnrollmentStateTransition> EnrollmentStateTransitions { get; set; }
        public DbSet<GovernedOperationLog> GovernedOperationLogs { get; set; }
        public DbSet<ExceptionQueueItem> ExceptionQueueItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().Property(e => e.Role).HasConversion<string>();
            modelBuilder.Entity<User>().Property(e => e.Status).HasConversion<string>();

            modelBuilder.Entity<Teacher>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Student>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Student>().Property(e => e.Sex).HasConversion<string>();

            modelBuilder.Entity<SchoolYear>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<ClassOffering>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Enrollment>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<Enrollment>().Property(e => e.ApprovalStatus).HasConversion<string>();
            modelBuilder.Entity<ClassStudent>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<EnrollmentStateTransition>().Property(e => e.PreviousStatus).HasConversion<string>();
            modelBuilder.Entity<EnrollmentStateTransition>().Property(e => e.NewStatus).HasConversion<string>();
            modelBuilder.Entity<EnrollmentStateTransition>().Property(e => e.PreviousApprovalStatus).HasConversion<string>();
            modelBuilder.Entity<EnrollmentStateTransition>().Property(e => e.NewApprovalStatus).HasConversion<string>();
            modelBuilder.Entity<EnrollmentStateTransition>().Property(e => e.TriggerAction).HasConversion<string>();
            modelBuilder.Entity<GovernedOperationLog>().Property(e => e.Status).HasConversion<string>();
            modelBuilder.Entity<ExceptionQueueItem>().Property(e => e.Severity).HasConversion<string>();
            modelBuilder.Entity<ExceptionQueueItem>().Property(e => e.Status).HasConversion<string>();

            modelBuilder.Entity<StudentRequirement>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentRequirement>()
                .HasOne(e => e.VerifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Student>()
                .HasOne(e => e.PreferredGradeLevel)
                .WithMany()
                .HasForeignKey(e => e.PreferredGradeLevelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Student>()
                .HasOne(e => e.PreferredCurriculum)
                .WithMany()
                .HasForeignKey(e => e.PreferredCurriculumId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ArchiveRecord>()
                .HasOne(e => e.DeletedByUser)
                .WithMany()
                .HasForeignKey(e => e.DeletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ArchiveRecord>()
                .HasOne(e => e.RestoredByUser)
                .WithMany()
                .HasForeignKey(e => e.RestoredByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            var testConfigurator = TestOptionsConfigurator;
            if (testConfigurator != null)
            {
                testConfigurator(optionsBuilder);
                return;
            }

            // Use configuration manager to get connection string based on active environment
            try
            {
                var connectionString = DatabaseConfig.GetConnectionString();
                optionsBuilder.UseMySQL(connectionString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to initialize database configuration. " +
                    "Please verify App.config settings. See inner exception for details.",
                    ex);
            }
        }
    }
}
