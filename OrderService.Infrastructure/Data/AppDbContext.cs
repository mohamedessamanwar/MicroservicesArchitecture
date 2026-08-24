using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OrderService.Application.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.ReadModels;
using Micro.Shared.Persistence;

namespace OrderService.Infrastructure.Data;

/// <summary>
/// Production-grade DbContext configured dynamically at runtime
/// using the ConnectionStringResolver based on the Request Context.
/// </summary>
public class AppDbContext : DbContext, IOrderDbContext
{
    private readonly ConnectionStringResolver _connectionStringResolver;

    public AppDbContext(DbContextOptions<AppDbContext> options, ConnectionStringResolver connectionStringResolver)
        : base(options)
    {
        _connectionStringResolver = connectionStringResolver;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(cancellationToken);
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<RuntimeMetricSnapshotRecord> RuntimeMetricSnapshots => Set<RuntimeMetricSnapshotRecord>();
    public DbSet<SpikeReportRecord> SpikeReports => Set<SpikeReportRecord>();
    
    public DbSet<Saga> Sagas => Set<Saga>();
    public DbSet<SagaStep> SagaSteps => Set<SagaStep>();

    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();

    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("order");

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(e => e.IdempotencyKey);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(128);
            entity.Property(e => e.RequestName).HasMaxLength(128).IsRequired();
        });

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

            entity.HasMany(e => e.OrderDetails)
                .WithOne(od => od.Order)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
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

        modelBuilder.Entity<RuntimeMetricSnapshotRecord>(entity =>
        {
            entity.ToTable("RuntimeMetricSnapshots");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CapturedAtUtc);
        });

        modelBuilder.Entity<SpikeReportRecord>(entity =>
        {
            entity.ToTable("SpikeReports");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reasons).IsRequired();
            entity.HasIndex(e => e.DetectedAtUtc);
            entity.HasOne(e => e.Snapshot)
                .WithMany()
                .HasForeignKey(e => e.RuntimeMetricSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Saga>(entity =>
        {
            entity.ToTable("Sagas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BusinessId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrentStep).HasMaxLength(100).IsRequired();
            
            entity.HasIndex(e => e.CorrelationId).IsUnique(); // Idempotency key must be unique
        });

        modelBuilder.Entity<SagaStep>(entity =>
        {
            entity.ToTable("SagaSteps");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StepName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(e => e.CompensationStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
            
            entity.HasOne(e => e.Saga)
                .WithMany(s => s.Steps)
                .HasForeignKey(e => e.SagaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
