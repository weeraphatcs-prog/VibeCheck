using System.Collections.Concurrent;

namespace KahootClone.Models;

public enum GamePhase
{
    Lobby,
    ShowQuestion,
    ShowAnswers,
    Leaderboard,
    Finished,
}

public class GameSession
{
    public string Pin { get; init; } = "";
    public Quiz Quiz { get; init; } = null!;
    public GamePhase Phase { get; set; } = GamePhase.Lobby;
    public int CurrentIndex { get; set; } = -1;
    public int SecondsLeft { get; set; }
    public string? HostConnectionId { get; set; }
    public DateTime QuestionStartedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public ConcurrentDictionary<string, Player> Players { get; } = new();

    // key = connectionId, value = (questionIndex, optionIndex)
    public ConcurrentDictionary<string, (int QuestionIndex, int OptionIndex)> Answers { get; } = new();
}
