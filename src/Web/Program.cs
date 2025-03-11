using Microsoft.EntityFrameworkCore;
using Web;
using Web.Persistence;
using Web.Processing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<BlogsContext>(options => options
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    .UseNpgsql(builder.Configuration.GetConnectionString("BlogsContext"))
    // .EnableSensitiveDataLogging()
    // .LogTo(Console.WriteLine, LogLevel.Information)
    .AddInterceptors(new SkipLockedRowsQueryCommandInterceptor()));
builder.Services.AddScoped<IProcessor, Processor>();
builder.Services.AddSingleton<IProcessingScheduler, ProcessingScheduler>();
builder.Services.AddSingleton<Seeder>();
builder.Services.AddScoped<IBlogRepository, BlogRepository>();
builder.Services.AddHostedService<ProcessingBackgroundService>();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));

app.MapGet("/seed",
    async (Seeder seeder, int numberOfBooksToSeed = 5000, int numberOfBlogsToSeed = 500, int lengthOfBookContentToSeed = 2000, int numberOfTagsToSeed = 100000,
        int numberOfTagsPerBlog = 1000) =>
    {
        await seeder.Seed(numberOfTagsToSeed, numberOfBooksToSeed, lengthOfBookContentToSeed, numberOfBlogsToSeed, numberOfTagsPerBlog);
        return Results.Ok();
    });

app.MapGet("/processBatch",
    async (IProcessor processor, CancellationToken cancellationToken, int batchSize = 50, bool retrieveTagsViaRelation = true) =>
    {
        await processor.ProcessBatchAsync(retrieveTagsViaRelation, batchSize, cancellationToken);
        return Results.Ok();
    });

app.MapGet("/resetBooks", async (IDbContextFactory<BlogsContext> dbContextFactory, int numberOfBooksToReset = 1) =>
{
    await using BlogsContext dbContext = dbContextFactory.CreateDbContext();
    await dbContext.Books
        .Take(numberOfBooksToReset)
        .OrderBy(book => book.Key) // get rid of EF Core warning RowLimitingOperationWithoutOrderByWarning
        .ExecuteUpdateAsync(book => book.SetProperty(e => e.State, ProcessState.Received));
    return Results.Ok();
});

app.MapGet("/enableBackgroundProcessing", (IProcessingScheduler scheduler) => Task.FromResult(scheduler.Enabled = true));
app.MapGet("/disableBackgroundProcessing", (IProcessingScheduler scheduler) => Task.FromResult(scheduler.Enabled = false));
app.MapGet("/configureBatchSizeForBackgroundProcessing", (IProcessingScheduler scheduler, int batchSize) => Task.FromResult(scheduler.BatchSize = batchSize));
app.MapGet("/configureParallelismForBackgroundProcessing",
    (IProcessingScheduler scheduler, int levelOfParallelism) => Task.FromResult(scheduler.LevelOfParallelism = levelOfParallelism));
app.MapGet("/configureRetrieveTagsViaRelationForBackgroundProcessing",
    (IProcessingScheduler scheduler, bool retrieveTagsViaRelation) => Task.FromResult(scheduler.RetrieveTagsViaRelation = retrieveTagsViaRelation));

app.Run();