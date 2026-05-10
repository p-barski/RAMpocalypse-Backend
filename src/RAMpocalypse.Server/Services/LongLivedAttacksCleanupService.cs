namespace RAMpocalypse.Server.Services;

public class LongLivedAttacksCleanupService(ILongLivedAttacksCleaner cleaner) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(2000);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                cleaner.PruneExpiredAttacks();
            }
        }
        catch (OperationCanceledException) { }
    }
}
