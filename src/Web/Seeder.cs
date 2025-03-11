using Microsoft.EntityFrameworkCore;
using Web.Models;
using Web.Persistence;

namespace Web;

public class Seeder(IDbContextFactory<BlogsContext> dbContextFactory, ILogger<Seeder> logger)
{
    public async Task Seed(int numberOfTagsToSeed, int numberOfBooksToSeed, int lengthOfBookContentToSeed, int numberOfBlogsToSeed, int numberOfTagsPerBlog)
    {
        var random = new Random();

        var tags = CreateRandomTags(random, numberOfTagsToSeed);

        await RecreateDatabaseIfNecessary();

        // work in small batches to avoid OOM
        await SeedBooksAsync(numberOfBooksToSeed, lengthOfBookContentToSeed, 1000, random, tags);
        await SeedBlogsAsync(numberOfBlogsToSeed, 1000, random, tags, numberOfTagsPerBlog);
        await SeedRelationshipsBetweenBlogsAndTagsAsync(await GetBlogKeysAsync(), 200, random, tags, numberOfTagsPerBlog);

        // Make sure indices are all up-to-date
        await dbContextFactory.CreateDbContext().Database.ExecuteSqlRawAsync("ANALYZE VERBOSE;");

        logger.LogInformation("Seeding finished");
    }

    private string CreateRandomTag(Random random, int maximumValue) => $"Tag_{random.Next(maximumValue)}";

    private List<string> CreateRandomTags(Random random, int maximumValue) =>
        Enumerable.Range(0, maximumValue).Select(_ => CreateRandomTag(random, maximumValue)).ToList();

    private async Task RecreateDatabaseIfNecessary()
    {
        await using BlogsContext blogsContext = dbContextFactory.CreateDbContext();
        await blogsContext.Database.EnsureDeletedAsync();
        await blogsContext.Database.EnsureCreatedAsync();

        logger.LogDebug("Database recreated");
    }

    private async Task SeedBooksAsync(int numberOfBooksToSeed, int lengthOfBookContentToSeed, int seedingBatchSize, Random random, List<string> tags)
    {
        for (var i = 0; i < numberOfBooksToSeed; i += seedingBatchSize)
        {
            await using BlogsContext dbContext = dbContextFactory.CreateDbContext();
            dbContext.Books.AddRange(
                Enumerable
                    .Range(i, Math.Min(seedingBatchSize, numberOfBooksToSeed - i))
                    .Select(counter => CreateRandomBook(counter, lengthOfBookContentToSeed, random, tags)));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogDebug("Added {NumberOfBooksSeeded} of {NumberOfBooksToSeed} books to the database", i + seedingBatchSize, numberOfBooksToSeed);
        }
    }

    private Book CreateRandomBook(int counter, int lengthOfBookContentToSeed, Random random, List<string> tags)
    {
        var title = $"Book {counter}";
        var book = new Book { Title = title, State = ProcessState.Received, Tag = GetRandomTag(random, tags) };
        book.SetContentFromObject(new BookDto($"Jane Doe {counter}", title, CreateRandomContent(lengthOfBookContentToSeed, random)));

        return book;
    }

    private string GetRandomTag(Random random, List<string> tags) => tags[random.Next(tags.Count)];

    private string CreateRandomContent(int lengthOfContentToSeed, Random random)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, lengthOfContentToSeed).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private Blog CreateRandomBlog(int counter, Random random, List<string> tags, int numberOfTagsPerBlog) =>
        new()
        {
            Title = $"Blog {counter}",
            TagNames = Enumerable.Range(0, numberOfTagsPerBlog).Select(_ => GetRandomTag(random, tags)).Distinct().ToList()
        };

    private async Task SeedBlogsAsync(int numberOfBlogsToSeed, int seedingBatchSize, Random random, List<string> tags, int numberOfTagsPerBlog)
    {
        for (var i = 0; i < numberOfBlogsToSeed; i += seedingBatchSize)
        {
            await using BlogsContext dbContext = dbContextFactory.CreateDbContext();
            dbContext.Blogs.AddRange(
                Enumerable
                    .Range(i, Math.Min(seedingBatchSize, numberOfBlogsToSeed - i))
                    .Select(counter => CreateRandomBlog(counter, random, tags, numberOfTagsPerBlog)));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogDebug("Added {NumberOfBlogsSeeded} of {NumberOfBlogsToSeed} blogs to the database", i + seedingBatchSize, numberOfBlogsToSeed);
        }
    }

    private async Task SeedRelationshipsBetweenBlogsAndTagsAsync(List<int> blogKeys, int seedingBatchSize, Random random, List<string> tags, int numberOfTagsPerBlog)
    {
        for (var i = 0; i < blogKeys.Count; i += seedingBatchSize)
        {
            await using BlogsContext dbContext = dbContextFactory.CreateDbContext();
            dbContext.Tags.AddRange(Enumerable
                .Range(i, Math.Min(seedingBatchSize, blogKeys.Count - i))
                .SelectMany(
                    counter => Enumerable.Range(0, numberOfTagsPerBlog).Select(_ => new Tag { BlogKey = blogKeys[counter], TagName = GetRandomTag(random, tags) })));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            logger.LogDebug(
                "Added relationships between {NumberOfBlogsSeeded} of {NumberOfBlogsToSeed} blogs and {NumberOfTagsSeeded} tags to the database",
                i + seedingBatchSize, blogKeys.Count, numberOfTagsPerBlog);
        }

        await dbContextFactory.CreateDbContext().Database.ExecuteSqlRawAsync("UPDATE \"Blogs\" " +
                                                                             "SET \"TagNames\" = q.\"TagNames\" " +
                                                                             "FROM (SELECT \"BlogKey\", array_agg(\"TagName\") AS \"TagNames\" FROM \"Tags\" GROUP BY \"BlogKey\") AS q " +
                                                                             "WHERE \"Key\" = q.\"BlogKey\";");
    }

    private async Task<List<int>> GetBlogKeysAsync()
    {
        await using BlogsContext dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Blogs.Select(book => book.Key).ToListAsync();
    }
}