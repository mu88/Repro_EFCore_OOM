namespace Web.Processing;

public interface IProcessor
{
    Task ProcessBatchAsync(bool retrieveTagsViaRelation, int batchSize, CancellationToken cancellationToken);
}