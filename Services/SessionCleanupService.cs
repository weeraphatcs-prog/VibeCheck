using KahootClone.Models;

namespace KahootClone.Services;

public class SessionCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(8);

    private readonly IGameService _gameService;
    private readonly TimerService _timerService;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(IGameService gameService, TimerService timerService, ILogger<SessionCleanupService> logger)
    {
        _gameService = gameService;
        _timerService = timerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            var cutoff = DateTime.UtcNow - MaxAge;
            var toRemove = _gameService.GetAllSessions()
                .Where(s => s.Phase == GamePhase.Finished || s.LastActivityAt < cutoff)
                .ToList();
            foreach (var session in toRemove)
            {
                _timerService.Cancel(session.Pin);
                _gameService.RemoveSession(session.Pin);
                _logger.LogInformation("Cleaned up session {Pin}", session.Pin);
            }
        }
    }
}
