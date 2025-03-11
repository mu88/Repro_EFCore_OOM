using Web.Persistence;

namespace Web.Processing;

public interface IBlogRepository
{
    Task<List<Blog>> GetBlogsByTagsAsync(bool includeTagsRelation, IEnumerable<string> tags, CancellationToken cancellationToken);
}