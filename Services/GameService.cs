using System.Collections.Concurrent;
using KahootClone.Models;

namespace KahootClone.Services;

public class GameService : IGameService
{
    private readonly ConcurrentDictionary<Guid, Quiz> _quizzes = new();
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public Quiz CreateQuiz(string title, IEnumerable<QuizQuestionDto> questions)
    {
        var questionList = questions.ToList();
        if (!questionList.Any())
            throw new ArgumentException("Quiz must have at least 1 question");
        if (title.Length > GameConstants.MaxQuizTitleLength)
            throw new ArgumentException($"Title max {GameConstants.MaxQuizTitleLength} chars");

        foreach (var q in questionList)
        {
            if (q.Options.Count(o => o.IsCorrect) != 1)
                throw new ArgumentException($"Question '{q.Text}' must have exactly 1 correct answer");
            if (q.Options.Count < GameConstants.MinOptionsPerQuestion || q.Options.Count > GameConstants.MaxOptionsPerQuestion)
                throw new ArgumentException($"Question must have {GameConstants.MinOptionsPerQuestion}-{GameConstants.MaxOptionsPerQuestion} options");
        }

        var quiz = new Quiz
        {
            Title = title,
            Questions = questionList.Select(q => new QuizQuestion
            {
                Text = q.Text,
                Options = q.Options,
                TimeLimitSec = q.TimeLimitSec,
                Points = q.Points,
            }).ToList(),
        };
        _quizzes[quiz.Id] = quiz;
        return quiz;
    }

    public IEnumerable<Quiz> GetAllQuizzes() => _quizzes.Values;
    public Quiz? GetQuiz(Guid id) => _quizzes.GetValueOrDefault(id);
    public bool DeleteQuiz(Guid id) => _quizzes.TryRemove(id, out _);

    public GameSession CreateSession(Guid quizId)
    {
        var quiz = GetQuiz(quizId) ?? throw new ArgumentException("Quiz not found");
        var session = new GameSession { Pin = GeneratePin(), Quiz = quiz };
        _sessions[session.Pin] = session;
        return session;
    }

    public GameSession? GetSession(string pin) => _sessions.GetValueOrDefault(pin);
    public bool RemoveSession(string pin) => _sessions.TryRemove(pin, out _);
    public IEnumerable<GameSession> GetAllSessions() => _sessions.Values;

    public JoinResult JoinSession(string pin, string connectionId, string nickname)
    {
        var session = GetSession(pin);
        if (session == null) return new(false, "Game not found", null);
        if (session.Phase != GamePhase.Lobby) return new(false, "Game already in progress", null);

        var nick = nickname?.Trim() ?? "";
        if (nick.Length < GameConstants.MinNicknameLength || nick.Length > GameConstants.MaxNicknameLength)
            return new(false, "Nickname must be 1–20 characters", null);
        if (!nick.All(c => char.IsLetterOrDigit(c) || c == ' '))
            return new(false, "Nickname: letters and numbers only", null);
        if (session.Players.Values.Any(p => p.Nickname == nick))
            return new(false, "Nickname already taken", null);
        if (session.Players.Count >= GameConstants.MaxPlayersPerSession)
            return new(false, "Game is full", null);

        var player = new Player { ConnectionId = connectionId, Nickname = nick };
        session.Players[connectionId] = player;
        return new(true, null, player);
    }

    public void RemovePlayer(string connectionId, string pin)
    {
        var session = GetSession(pin);
        session?.Players.TryRemove(connectionId, out _);
    }

    public AnswerResult SubmitAnswer(string pin, string connectionId, int optionIndex)
    {
        var session = GetSession(pin);
        if (session == null) return new(false, "Game not found");
        if (session.Phase != GamePhase.ShowQuestion) return new(false, "Not accepting answers now");
        if (!session.Players.TryGetValue(connectionId, out var player)) return new(false, "Player not found");
        if (player.HasAnswered) return new(false, "Already answered");

        var currentQ = session.Quiz.Questions[session.CurrentIndex];
        if (optionIndex < 0 || optionIndex >= currentQ.Options.Count) return new(false, "Invalid option");

        player.HasAnswered = true;
        session.Answers[connectionId] = (session.CurrentIndex, optionIndex);
        return new(true, null);
    }

    public GameSession? StartGame(string pin)
    {
        var session = GetSession(pin);
        if (session == null || session.Phase != GamePhase.Lobby) return null;
        session.CurrentIndex = 0;
        session.Phase = GamePhase.ShowQuestion;
        session.QuestionStartedAt = DateTime.UtcNow;
        session.LastActivityAt = DateTime.UtcNow;
        return session;
    }

    public GameSession? NextStep(string pin)
    {
        var session = GetSession(pin);
        if (session == null) return null;

        session.LastActivityAt = DateTime.UtcNow;

        switch (session.Phase)
        {
            case GamePhase.ShowAnswers:
                session.Phase = GamePhase.Leaderboard;
                break;
            case GamePhase.Leaderboard:
                if (session.CurrentIndex + 1 >= session.Quiz.Questions.Count)
                {
                    session.Phase = GamePhase.Finished;
                }
                else
                {
                    session.CurrentIndex++;
                    session.Phase = GamePhase.ShowQuestion;
                    session.QuestionStartedAt = DateTime.UtcNow;
                    ResetAnswers(session);
                }
                break;
        }
        return session;
    }

    public GameSession? EndGame(string pin)
    {
        var session = GetSession(pin);
        if (session == null) return null;
        session.Phase = GamePhase.Finished;
        session.LastActivityAt = DateTime.UtcNow;
        return session;
    }

    public int CalcScore(Player player, QuizQuestion q, GameSession session)
    {
        if (!session.Answers.TryGetValue(player.ConnectionId, out var ans)) return 0;
        if (ans.QuestionIndex != session.CurrentIndex) return 0;
        if (!q.Options[ans.OptionIndex].IsCorrect) return 0;
        double elapsed = (DateTime.UtcNow - session.QuestionStartedAt).TotalSeconds;
        double ratio = Math.Max(0, 1 - (elapsed / q.TimeLimitSec));
        return (int)(q.Points * (0.5 + 0.5 * ratio));
    }

    private void ResetAnswers(GameSession session)
    {
        session.Answers.Clear();
        foreach (var p in session.Players.Values)
            p.HasAnswered = false;
    }

    private string GeneratePin()
    {
        for (int i = 0; i < 20; i++)
        {
            var pin = Random.Shared.Next(100000, 999999).ToString();
            if (!_sessions.ContainsKey(pin)) return pin;
        }
        throw new InvalidOperationException("Cannot generate unique PIN");
    }
}
