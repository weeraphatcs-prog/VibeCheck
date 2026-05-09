namespace KahootClone.Models;

public class Player
{
    public string ConnectionId { get; init; } = "";
    public string Nickname { get; set; } = "";
    public int TotalScore { get; set; } = 0;
    public bool HasAnswered { get; set; } = false;
}
