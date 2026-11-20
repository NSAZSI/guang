using AdPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdPulse.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<News> News => Set<News>();
    public DbSet<NewsTag> NewsTags => Set<NewsTag>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<ViewLog> ViewLogs => Set<ViewLog>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.Property(x => x.UserName).HasMaxLength(64).IsRequired();
            e.Property(x => x.Role).HasMaxLength(16).IsRequired();
        });

        b.Entity<Category>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(32).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(64).IsRequired();
        });

        b.Entity<Tag>(e =>
        {
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(32).IsRequired();
        });

        b.Entity<News>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.AiStatus).HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.PublishedAt);
            e.HasOne(x => x.Category).WithMany(c => c.News).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<NewsTag>(e =>
        {
            e.HasKey(x => new { x.NewsId, x.TagId });
            e.HasOne(x => x.News).WithMany(n => n.NewsTags).HasForeignKey(x => x.NewsId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.NewsTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Banner>(e =>
        {
            e.HasOne(x => x.LinkNews).WithMany().HasForeignKey(x => x.LinkNewsId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ViewLog>(e => e.HasIndex(x => x.ViewedAt));
        b.Entity<OperationLog>(e => e.HasIndex(x => x.CreatedAt));
    }
}