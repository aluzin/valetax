using Microsoft.EntityFrameworkCore;
using Valetax.Application.Abstractions;
using Valetax.Domain.Entities;

namespace Valetax.Infrastructure.Persistence;

public sealed class ValetaxDbContext(DbContextOptions<ValetaxDbContext> options) : DbContext(options), IValetaxDbContext
{
    public DbSet<ExceptionJournal> ExceptionJournals => Set<ExceptionJournal>();

    public DbSet<Tree> Trees => Set<Tree>();

    public DbSet<Node> Nodes => Set<Node>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tree>(tree =>
        {
            tree.ToTable("trees");

            tree.HasKey(x => x.Id);

            tree.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);

            tree.HasIndex(x => x.Name)
                .IsUnique();

            tree.HasMany(x => x.Nodes)
                .WithOne(x => x.Tree)
                .HasForeignKey(x => x.TreeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Node>(node =>
        {
            node.ToTable("nodes");

            node.HasKey(x => x.Id);

            node.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);

            node.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            node.HasIndex(x => x.TreeId)
                .HasDatabaseName("UX_nodes_tree_root")
                .IsUnique()
                .HasFilter("\"ParentId\" IS NULL");

            node.HasIndex(x => new { x.TreeId, x.ParentId, x.Name })
                .HasDatabaseName("UX_nodes_tree_parent_name")
                .IsUnique()
                .HasFilter("\"ParentId\" IS NOT NULL");
        });

        modelBuilder.Entity<ExceptionJournal>(journal =>
        {
            journal.ToTable("exception_journals");

            journal.HasKey(x => x.Id);

            journal.Property(x => x.ExceptionType)
                .IsRequired()
                .HasMaxLength(256);

            journal.Property(x => x.Message)
                .IsRequired();

            journal.Property(x => x.Path)
                .HasMaxLength(2048);

            journal.Property(x => x.Method)
                .HasMaxLength(16);

            journal.Property(x => x.QueryParameters)
                .HasColumnType("jsonb");

            journal.Property(x => x.BodyParameters)
                .HasColumnType("jsonb");

            journal.Property(x => x.Text)
                .IsRequired();

            journal.HasIndex(x => x.EventId)
                .IsUnique();

            journal.HasIndex(x => x.CreatedAt);
        });
    }
}
