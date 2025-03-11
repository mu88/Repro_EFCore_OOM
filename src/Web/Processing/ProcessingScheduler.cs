namespace Web.Processing;

public class ProcessingScheduler(IServiceProvider serviceProvider, ILogger<ProcessingScheduler> logger) : IProcessingScheduler
{
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            if (!Enabled)
            {
                await SleepAsync(cancellationToken);
                continue;
            }

            try
            {
                logger.LogInformation(
                    "Start processing / LevelOfParallelism: {LevelOfParallelism} / BatchSize: {BatchSize} / RetrieveTagsViaRelation: {RetrieveTagsViaRelation}",
                    LevelOfParallelism, BatchSize, RetrieveTagsViaRelation);
                using IServiceScope serviceScope = serviceProvider.CreateScope();
                var bulkProcessor = serviceScope.ServiceProvider.GetRequiredService<IProcessor>();

                List<Task> processingTasks = [];
                for (var i = 0; i < LevelOfParallelism; i++) processingTasks.Add(bulkProcessor.ProcessBatchAsync(RetrieveTagsViaRelation, BatchSize, cancellationToken));

                await Task.WhenAll(processingTasks);

                logger.LogDebug("Stop processing");
                await SleepAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error processing");
                await SleepAsync(cancellationToken);
            }
        }
    }

    public int LevelOfParallelism { get; set; } = 10;

    public int BatchSize { get; set; } = 50;

    public bool Enabled { get; set; } = false;

    public bool RetrieveTagsViaRelation { get; set; } = true;

    private static async Task SleepAsync(CancellationToken cancellationToken) => await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
}