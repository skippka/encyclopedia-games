namespace GameEncyclopedia;

public class GameItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public int ReleaseYear { get; set; }
    public string Genre { get; set; } = "";
    public string CoverPath { get; set; } = "";
    public string MediaPath { get; set; } = "";
    public string HtmlPath { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
}
