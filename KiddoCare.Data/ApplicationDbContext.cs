namespace KiddoCare.Data;

using KiddoCare.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public virtual DbSet<KindergartenGroup> KindergartenGroups { get; set; } = null!;

    public virtual DbSet<Child> Children { get; set; } = null!;

    public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;

    public virtual DbSet<Event> Events { get; set; } = null!;

    public virtual DbSet<ParentProfile> ParentProfiles { get; set; } = null!;

    public virtual DbSet<TeacherProfile> TeacherProfiles { get; set; } = null!;

    public virtual DbSet<Announcement> Announcements { get; set; } = null!;

    public virtual DbSet<DailyReport> DailyReports { get; set; } = null!;

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MedicalRecord>()
            .HasOne(m => m.Child)
            .WithMany(c => c.MedicalRecords)
            .HasForeignKey(m => m.ChildId);

        builder.Entity<MedicalRecord>()
            .HasIndex(m => m.ChildId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<DailyReport>()
           .HasIndex(r => new { r.ChildId, r.ReportDate })
           .IsUnique()
           .HasFilter("[IsDeleted] = 0");
    }
}