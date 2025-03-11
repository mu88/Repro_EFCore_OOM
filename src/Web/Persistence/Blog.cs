namespace Web.Persistence;

public class Blog
{
    public int Key { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<string> TagNames { get; set; } = [];

    public List<Tag> TagsViaRelationship { get; set; } = [];
}