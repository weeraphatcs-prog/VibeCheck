using Microsoft.AspNetCore.SignalR;
using KahootClone.Models;
using KahootClone.Services;

namespace KahootClone.Hubs;

public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly TimerService _timerService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(IGameService gameService, TimerService timerService, IHubContext<GameHub> hubContext)
    {
        _gameService = gameService;
        _timerService = timerService;
        _hubContext = hubContext;
    }

    // Host: create session for an existing quiz
    public async Task HostCreateSession(Guid quizId)
    {
        GameSession session;
        try { session = _gameService.CreateSession(quizId); }
        catch (ArgumentException ex) { await SendError(ex.Message); return; }

        _gameService.RegisterHost(session.Pin, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, session.Pin);
        await Clients.Caller.SendAsync("SessionCreated", session.Pin, session.Quiz.Title, session.Quiz.Id);
    }

    // Host: start the game (moves from Lobby → first question)
    public async Task StartGame(string pin)
    {
        if (!IsHost(pin)) { await SendError("Not authorized"); return; }
        var session = _gameService.StartGame(pin);
        if (session == null) { await SendError("Cannot start game"); return; }

        await _hubContext.Clients.Group(pin).SendAsync("GameStarted");
        await SendQuestionToGroup(pin, session);
    }

    // Host: advance game state (ShowAnswers→Leaderboard, Leaderboard→next question or Finished)
    public async Task NextStep(string pin)
    {
        if (!IsHost(pin)) { await SendError("Not authorized"); return; }
        var session = _gameService.NextStep(pin);
        if (session == null) return;

        switch (session.Phase)
        {
            case GamePhase.ShowQuestion:
                await SendQuestionToGroup(pin, session);
                break;
            case GamePhase.Leaderboard:
                await BroadcastLeaderboard(pin, session, finished: false);
                break;
            case GamePhase.Finished:
                _timerService.Cancel(pin);
                await BroadcastLeaderboard(pin, session, finished: true);
                break;
        }
    }

    // Player: join a session in lobby
    public async Task JoinGame(string pin, string nickname)
    {
        var result = _gameService.JoinSession(pin, Context.ConnectionId, nickname);
        if (!result.Success) { await SendError(result.Error!); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, pin);
        var session = _gameService.GetSession(pin)!;
        await Clients.Caller.SendAsync("JoinedGame", pin, nickname);
        await _hubContext.Clients.Group(pin).SendAsync("PlayerJoined", nickname, session.Players.Count);
    }

    // Player: submit answer
    public async Task SubmitAnswer(string pin, int optionIndex)
    {
        var result = _gameService.SubmitAnswer(pin, Context.ConnectionId, optionIndex);
        if (!result.Success) { await SendError(result.Error!); return; }

        await Clients.Caller.SendAsync("AnswerAccepted");

        if (_gameService.AllPlayersAnswered(pin))
        {
            _timerService.Cancel(pin);
            await EndQuestionAndBroadcast(pin);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var pin = _gameService.FindPinByConnection(Context.ConnectionId);
        if (pin != null)
        {
            var session = _gameService.GetSession(pin);
            if (session?.HostConnectionId == Context.ConnectionId)
            {
                _timerService.Cancel(pin);
                _gameService.EndGame(pin);
                await _hubContext.Clients.Group(pin).SendAsync("HostLeft");
            }
            else if (session != null && session.Players.TryGetValue(Context.ConnectionId, out var player))
            {
                _gameService.RemovePlayer(Context.ConnectionId, pin);
                await _hubContext.Clients.Group(pin).SendAsync("PlayerLeft", player.Nickname, session.Players.Count);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    // --- helpers ---

    private bool IsHost(string pin)
    {
        var session = _gameService.GetSession(pin);
        return session?.HostConnectionId == Context.ConnectionId;
    }

    private async Task SendQuestionToGroup(string pin, GameSession session)
    {
        var q = session.Quiz.Questions[session.CurrentIndex];
        var payload = new
        {
            index = session.CurrentIndex,
            total = session.Quiz.Questions.Count,
            text = q.Text,
            options = q.Options.Select((o, i) => new { index = i, text = o.Text }).ToList(),
            timeLimitSec = q.TimeLimitSec,
        };
        await _hubContext.Clients.Group(pin).SendAsync("QuestionStarted", payload);

        _timerService.StartTimer(pin, q.TimeLimitSec,
            onTick: async secsLeft =>
            {
                session.SecondsLeft = secsLeft;
                await _hubContext.Clients.Group(pin).SendAsync("TimerTick", secsLeft);
            },
            onExpired: () => EndQuestionAndBroadcast(pin));
    }

    private async Task EndQuestionAndBroadcast(string pin)
    {
        var session = _gameService.EndQuestion(pin);
        if (session == null) return; // already transitioned (race guard)

        var q = session.Quiz.Questions[session.CurrentIndex];
        int correctIndex = q.Options.FindIndex(o => o.IsCorrect);
        var scores = session.Players.Values
            .OrderByDescending(p => p.TotalScore)
            .Select(p => new { nickname = p.Nickname, total = p.TotalScore })
            .ToList();

        await _hubContext.Clients.Group(pin).SendAsync("QuestionEnded", correctIndex, scores);
    }

    private async Task BroadcastLeaderboard(string pin, GameSession session, bool finished)
    {
        var leaderboard = session.Players.Values
            .OrderByDescending(p => p.TotalScore)
            .Select((p, i) => new { rank = i + 1, nickname = p.Nickname, score = p.TotalScore })
            .ToList();

        var ev = finished ? "GameFinished" : "LeaderboardUpdate";
        await _hubContext.Clients.Group(pin).SendAsync(ev, leaderboard);
    }

    private Task SendError(string message) =>
        Clients.Caller.SendAsync("Error", message);
}
