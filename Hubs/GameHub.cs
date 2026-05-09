using Microsoft.AspNetCore.SignalR;
using KahootClone.Models;
using KahootClone.Services;

namespace KahootClone.Hubs;

// Phase 3: Full implementation
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly TimerService _timerService;

    public GameHub(IGameService gameService, TimerService timerService)
    {
        _gameService = gameService;
        _timerService = timerService;
    }

    private Task SendError(string message) =>
        Clients.Caller.SendAsync("Error", message);
}
