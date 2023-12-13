namespace Dodkin.Service;

using Delivery;

sealed class Worker : BackgroundService
{
    private readonly Messenger messenger;
    private readonly ILogger<Worker> log;

    public Worker(Messenger messenger, ILogger<Worker> log)
    {
        this.messenger = messenger;
        this.log = log;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return this.messenger.ProcessAsync(stoppingToken);
    }
}