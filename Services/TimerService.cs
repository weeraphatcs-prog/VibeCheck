using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace KahootClone.Services;

public class TimerService
{
    private readonly ILogger<TimerService> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    public TimerService(ILogger<TimerService> logger) => _logger = logger;

    public void StartTimer(string pin, int seconds, Func<int, Task> onTick, Func<Task> onExpired)
    {
        Cancel(pin);
        var cts = new CancellationTokenSource();
        _timers[pin] = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                for (int i = seconds; i >= 0; i--)
                {
                    if (cts.Token.IsCancellationRequested) return;
                    await onTick(i);
                    if (i > 0) await Task.Delay(1000, cts.Token);
                }
                if (!cts.Token.IsCancellationRequested)
                    await onExpired();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timer error for PIN {Pin}", pin);
                Cancel(pin);
            }
        }, cts.Token);
    }

    public void Cancel(string pin)
    {
        if (_timers.TryRemove(pin, out var cts)) cts.Cancel();
    }
}
