using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.ReadModels;

namespace OrderService.Infrastructure.Data;

/// <summary>
/// Production-grade DbContext configured dynamically at runtime
/// using the ConnectionStringResolver based on the Request Context.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entities (Primary/Write DB)
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CustomerId).IsRequired();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
        });
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(250).IsRequired();
            entity.HasIndex(e => e.MessageId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(250).IsRequired();
            entity.Property(e => e.ProviderName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.ExchangeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RoutingKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => new { e.Status, e.OccurredOnUtc });
        });
    }
}
