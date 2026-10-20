using System.Text.Json;
using CareerSyncAI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CareerSyncAI.Data;

public class CareerSyncDbContext : DbContext
{
    public CareerSyncDbContext(DbContextOptions<CareerSyncDbContext> options) : base(options) { }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<CV> CVs => Set<CV>();
    public DbSet<JobDescription> JobDescriptions => Set<JobDescription>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<SkillDictionaryTerm> SkillDictionaryTerms => Set<SkillDictionaryTerm>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<MatchingWeights> MatchingWeights => Set<MatchingWeights>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        void ConfigureStringList(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<List<string>> prop)
        {
            prop.HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new())
                .Metadata.SetValueComparer(stringListComparer);
        }

        modelBuilder.Entity<CV>(e =>
        {
            ConfigureStringList(e.Property(c => c.ExtractedSkills));
            ConfigureStringList(e.Property(c => c.ExtractedQualifications));
        });

        modelBuilder.Entity<JobDescription>(e =>
        {
            ConfigureStringList(e.Property(j => j.RequiredSkills));
            ConfigureStringList(e.Property(j => j.RequiredQualifications));
        });

        modelBuilder.Entity<MatchResult>(e =>
        {
            ConfigureStringList(e.Property(m => m.MatchedSkills));
            ConfigureStringList(e.Property(m => m.GapSkills));
        });

        modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
    }
}
