namespace KahootClone.Models;

public record QuizOption(string Text, bool IsCorrect);

public class QuizQuestion
{
    public string Text { get; set; } = "";
    public List<QuizOption> Options { get; init; } = new();
    public int TimeLimitSec { get; set; } = 20;
    public int Points { get; set; } = 1000;
}

public class Quiz
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public List<QuizQuestion> Questions { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
