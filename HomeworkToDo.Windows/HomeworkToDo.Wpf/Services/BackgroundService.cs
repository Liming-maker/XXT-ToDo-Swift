using HomeworkToDo.Core.Services;

namespace HomeworkToDo.Wpf.Services;

public class BackgroundService
{
    private readonly Func<Task> _refreshAction;
    private CancellationTokenSource? _cts;

    public BackgroundService(Func<Task> refreshAction)
    {
        _refreshAction = refreshAction;
    }

    public void Start(TimeSpan interval)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(interval, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunLoopAsync(TimeSpan interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(interval, ct);
                await _refreshAction();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Silently retry on next interval
            }
        }
    }
}
