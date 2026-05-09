namespace KahootClone.Hubs;

public record OptionDto(int Index, string Text);
public record QuestionPayload(int Index, int Total, string Text, List<OptionDto> Options, int TimeLimitSec);
public record ScoreEntry(string Nickname, int Total);
public record LeaderEntry(int Rank, string Nickname, int Score);
