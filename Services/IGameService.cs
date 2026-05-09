using KahootClone.Models;

namespace KahootClone.Services;

public record JoinResult(bool Success, string? Error, Player? Player);
public record AnswerResult(bool Success, string? Error);

public record QuizQuestionDto(
    string Text,
    List<QuizOption> Options,
    int TimeLimitSec = 20,
    int Points = 1000);

public interface IGameService
{
    Quiz CreateQuiz(string title, IEnumerable<QuizQuestionDto> questions);
    IEnumerable<Quiz> GetAllQuizzes();
    Quiz? GetQuiz(Guid id);
    bool DeleteQuiz(Guid id);

    GameSession CreateSession(Guid quizId);
    GameSession? GetSession(string pin);
    bool RemoveSession(string pin);
    IEnumerable<GameSession> GetAllSessions();

    JoinResult JoinSession(string pin, string connectionId, string nickname);
    void RemovePlayer(string connectionId, string pin);
    AnswerResult SubmitAnswer(string pin, string connectionId, int optionIndex);

    GameSession? StartGame(string pin);
    GameSession? NextStep(string pin);
    GameSession? EndGame(string pin);
}
