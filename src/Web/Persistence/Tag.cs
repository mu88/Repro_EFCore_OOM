namespace Web.Persistence;

public class Tag
{
    public int Key { get; set; }

    public string TagName { get; set; } = string.Empty;

    public Blog Blog { get; set; } = null!;

    public int BlogKey { get; set; }
}