using Microsoft.EntityFrameworkCore;
using Web.Persistence;

namespace Web.Processing;

public class BlogRepository : IBlogRepository
{
    private readonly IDbContextFactory<BlogsContext> _dbContextFactory;

    public BlogRepository(IDbContextFactory<BlogsContext> dbContextFactory) => _dbContextFactory = dbContextFactory;

    public async Task<List<Blog>> GetBlogsByTagsAsync(bool includeTagsRelation, IEnumerable<string> tags, CancellationToken cancellationToken)
    {
        await using BlogsContext dbContext = _dbContextFactory.CreateDbContext();
        return includeTagsRelation
            ? await dbContext.Blogs
                .Include(blog => blog.TagsViaRelationship)
                .Where(blog => blog.TagsViaRelationship.Any(tag => tags.Contains(tag.TagName)))
                .ToListAsync(cancellationToken)
            : await dbContext.Blogs
                .Where(blog => blog.TagsViaRelationship.Any(tag => tags.Contains(tag.TagName)))
                .ToListAsync(cancellationToken);
    }
}