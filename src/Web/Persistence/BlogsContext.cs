using Microsoft.EntityFrameworkCore;

namespace Web.Persistence;

public class BlogsContext(DbContextOptions<BlogsContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; } = null!;

    public DbSet<Blog> Blogs { get; set; } = null!;

    public DbSet<Tag> Tags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tag>().HasKey(tag => tag.Key);
        modelBuilder.Entity<Tag>().HasIndex(tag => tag.Key);
        modelBuilder.Entity<Tag>().HasIndex(tag => new { tag.TagName, tag.BlogKey });

        modelBuilder.Entity<Book>().HasKey(book => book.Key);
        modelBuilder.Entity<Book>().HasIndex(book => book.Key);
        modelBuilder.Entity<Book>().HasIndex(book => book.Tag);
        modelBuilder.Entity<Book>().HasIndex(book => book.State);

        modelBuilder.Entity<Blog>().HasKey(blog => blog.Key);
        modelBuilder.Entity<Blog>().HasIndex(blog => blog.Key);
        modelBuilder.Entity<Blog>().HasIndex(blog => blog.TagNames).HasMethod("gin"); // 🍸
        modelBuilder
            .Entity<Blog>()
            .HasMany(blog => blog.TagsViaRelationship)
            .WithOne(tagOfBlog => tagOfBlog.Blog)
            .HasForeignKey(tagOfBlog => tagOfBlog.BlogKey);
    }
}