namespace Web.Processing;

public interface IProcessingScheduler
{
    public int LevelOfParallelism { get; set; }

    public int BatchSize { get; set; }

    public bool Enabled { get; set; }

    public bool RetrieveTagsViaRelation { get; set; }

    Task ProcessAsync(CancellationToken cancellationToken = default);
}