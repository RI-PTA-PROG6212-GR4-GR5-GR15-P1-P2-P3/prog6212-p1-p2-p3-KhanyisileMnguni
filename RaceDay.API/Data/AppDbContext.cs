using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    // Mirrors the Part 1 SQL script: same table names, lengths, keys and unique constraints.
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Organiser>(e =>
        {
            e.ToTable("Organiser");
            e.HasKey(x => x.OrganiserID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Password).HasMaxLength(200).IsRequired();
            e.Property(x => x.OrganisationName).HasMaxLength(150);
        });

        b.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.HasKey(x => x.ParticipantID);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Password).HasMaxLength(200).IsRequired();
            e.Property(x => x.EmergencyContact).HasMaxLength(100);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Event");
            e.HasKey(x => x.EventID);
            e.Property(x => x.EventName).HasMaxLength(100).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasOne(x => x.Organiser).WithMany(o => o.Events).HasForeignKey(x => x.OrganiserID);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.HasKey(x => x.CategoryID);
            e.Property(x => x.CategoryName).HasMaxLength(50).IsRequired();
            e.Property(x => x.DistanceKM).HasPrecision(5, 2);
            e.Property(x => x.EntryFee).HasPrecision(8, 2);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories).HasForeignKey(x => x.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment");
            e.HasKey(x => x.EnrolmentID);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(p => p.Enrolments).HasForeignKey(x => x.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments).HasForeignKey(x => x.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Result");
            e.HasKey(x => x.ResultID);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.EnrolmentID).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result).HasForeignKey<Result>(x => x.EnrolmentID);
        });
    }
}