namespace HomepageSC;

public class Widget
{
    public Widget(string type, string url, string? key, string? username, string? password)
    {
        Type = type;
        Url = url;
        Key = key;
        Username = username;
        Password = password;
    }

    public string Type { get; }
    public string Url { get; }
    public string? Key { get; }
    public string? Username { get; }
    public string? Password { get; }
}