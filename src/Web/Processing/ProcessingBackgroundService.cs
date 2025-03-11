namespace Web.Processing;

public class ProcessingBackgroundService(IProcessingScheduler processingScheduler) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => processingScheduler.ProcessAsync(stoppingToken);
}