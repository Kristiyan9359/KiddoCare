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

    public virtual DbSet<AbsenceRequest> AbsenceRequests { get; set; } = null!;

    public virtual DbSet<ConsentRequest> ConsentRequests { get; set; } = null!;

    public virtual DbSet<ChildDocument> ChildDocuments { get; set; } = null!;

    public virtual DbSet<Conversation> Conversations { get; set; } = null!;

    public virtual DbSet<ChatMessage> ChatMessages { get; set; } = null!;

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

        builder.Entity<Conversation>()
            .HasOne(c => c.Child)
            .WithMany()
            .HasForeignKey(c => c.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasOne(c => c.ParentUser)
            .WithMany()
            .HasForeignKey(c => c.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasOne(c => c.TeacherUser)
            .WithMany()
            .HasForeignKey(c => c.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasOne(c => c.AdminUser)
            .WithMany()
            .HasForeignKey(c => c.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasIndex(c => new { c.Type, c.ChildId, c.ParentUserId, c.TeacherUserId, c.AdminUserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Entity<ChatMessage>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatMessage>()
            .HasOne(m => m.SenderUser)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
