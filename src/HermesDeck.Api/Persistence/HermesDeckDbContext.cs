using HermesDeck.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HermesDeck.Api.Persistence;

/// <summary>
/// EF Core database context for the Hermes Control Deck persistent entities, backed by PostgreSQL.
/// </summary>
public class HermesDeckDbContext : DbContext
{
    public HermesDeckDbContext(DbContextOptions<HermesDeckDbContext> options)
        : base(options)
    {
    }

    public DbSet<TelegramUser> TelegramUsers => Set<TelegramUser>();

    public DbSet<HermesIdentity> HermesIdentities => Set<HermesIdentity>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();

    public DbSet<TimelineEvent> TimelineEvents => Set<TimelineEvent>();

    public DbSet<ToolCall> ToolCalls => Set<ToolCall>();

    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    public DbSet<Panel> Panels => Set<Panel>();

    public DbSet<PanelIntent> PanelIntents => Set<PanelIntent>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostgreSQL-native column types (text[]/jsonb) are only applied on a relational
        // provider so the InMemory provider used in tests does not reject them.
        var isRelational = Database.IsRelational();

        modelBuilder.Entity<TelegramUser>(entity =>
        {
            entity.HasKey(e => e.TelegramUserId);
        });

        modelBuilder.Entity<HermesIdentity>(entity =>
        {
            entity.HasKey(e => e.IdentityId);
            entity.HasOne<TelegramUser>()
                .WithOne()
                .HasForeignKey<HermesIdentity>(e => e.TelegramUserId)
                .OnDelete(DeleteBehavior.Restrict);
            if (isRelational)
            {
                entity.Property(e => e.Roles).HasColumnType("text[]");
                entity.Property(e => e.Permissions).HasColumnType("text[]");
            }
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.HasOne<HermesIdentity>()
                .WithMany()
                .HasForeignKey(e => e.IdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);
            entity.HasOne<HermesIdentity>()
                .WithMany()
                .HasForeignKey(e => e.IdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentRun>(entity =>
        {
            entity.HasKey(e => e.RunId);
            entity.HasOne<Conversation>()
                .WithMany()
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimelineEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.HasOne<AgentRun>()
                .WithMany()
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
            if (isRelational)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
            else
            {
                // The InMemory provider cannot map IReadOnlyDictionary natively; round-trip it
                // through JSON so test runs can persist TimelineEvent rows.
                entity.Property(e => e.Metadata).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                        v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
            }
        });

        modelBuilder.Entity<ToolCall>(entity =>
        {
            entity.HasKey(e => e.ToolCallId);
            entity.HasOne<AgentRun>()
                .WithMany()
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(e => e.ApprovalId);
            entity.HasOne<AgentRun>()
                .WithMany()
                .HasForeignKey(e => e.RunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ToolCall>()
                .WithMany()
                .HasForeignKey(e => e.ToolCallId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.HasKey(e => e.DecisionId);
            entity.HasOne<ApprovalRequest>()
                .WithOne()
                .HasForeignKey<ApprovalDecision>(e => e.ApprovalId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<HermesIdentity>()
                .WithMany()
                .HasForeignKey(e => e.IdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Panel>(entity =>
        {
            entity.HasKey(e => e.PanelId);
            if (isRelational)
            {
                entity.Property(e => e.AllowedActions).HasColumnType("text[]");
            }
        });

        modelBuilder.Entity<PanelIntent>(entity =>
        {
            entity.HasKey(e => e.IntentId);
            entity.HasOne<Panel>()
                .WithMany()
                .HasForeignKey(e => e.PanelId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<HermesIdentity>()
                .WithMany()
                .HasForeignKey(e => e.IdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.HasOne<HermesIdentity>()
                .WithMany()
                .HasForeignKey(e => e.IdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
