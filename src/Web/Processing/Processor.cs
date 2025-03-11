using Microsoft.EntityFrameworkCore;
using Web.Persistence;

namespace Web.Processing;

public class Processor(IDbContextFactory<BlogsContext> dbContextFactory, IBlogRepository blogRepository) : IProcessor
{
    public async Task ProcessBatchAsync(bool retrieveTagsViaRelation, int batchSize, CancellationToken cancellationToken)
    {
        BlogsContext dbContext = dbContextFactory.CreateDbContext();
        var receivedBooks = await dbContext.Set<Book>()
            .Where(entity => entity.State == ProcessState.Received)
            .OrderBy(entity => entity.Key) // get rid of EF Core warning RowLimitingOperationWithoutOrderByWarning
            .Take(batchSize)
            .TagWith(DbCommandTags.SkipLockedRows) // this tag is used by a dedicated interceptor to allow queue-like processing on PostgreSQL
            .AsTracking()
            .ToListAsync(cancellationToken);

        var tagsToSearchFor = receivedBooks.Select(book => book.Tag).Distinct();
        var blogsWithMatchingTags = await blogRepository.GetBlogsByTagsAsync(retrieveTagsViaRelation, tagsToSearchFor, cancellationToken);

        foreach (Book? book in receivedBooks)
        {
            book.NumberOfBlogsWithSameTag = retrieveTagsViaRelation
                ? blogsWithMatchingTags.Count(blog => blog.TagsViaRelationship.Any(tag => tag.TagName == book.Tag))
                : blogsWithMatchingTags.Count(blog => blog.TagNames.Any(tag => tag == book.Tag));
            book.Author = book.GetObjectFromContent().Author;
            book.State = ProcessState.Processed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}