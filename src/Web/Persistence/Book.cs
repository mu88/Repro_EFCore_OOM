using Newtonsoft.Json;
using Web.Models;

namespace Web.Persistence;

public class Book
{
    public int Key { get; set; }

    public string Author { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Tag { get; set; } = string.Empty;

    public ProcessState State { get; set; }

    public int NumberOfBlogsWithSameTag { get; set; }

    public string Content { get; set; } = string.Empty;

    public BookDto GetObjectFromContent() =>
        JsonConvert.DeserializeObject<BookDto>(Content) ?? throw new ArgumentException($"JSON string of type {nameof(BookDto)} can not be deserialized.");

    public void SetContentFromObject(BookDto bookDto) =>
        Content = JsonConvert.SerializeObject(bookDto) ?? throw new ArgumentException($"Object of type {nameof(BookDto)} can not be serialized.");
}