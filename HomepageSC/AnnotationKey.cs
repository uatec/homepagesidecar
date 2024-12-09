namespace HomepageSC;

public static class AnnotationKey
{
    public static string Enable(string? name = null) => CreateAnnotation("enable", name);
    public static string Group(string? name = null) => CreateAnnotation("group", name);
    public static string WidgetType(string? name = null) => CreateAnnotation("widget_type", name);
    public static string WidgetSecret(string? name = null) => CreateAnnotation("widget_secret", name);
    public static string WidgetUrl(string? name = null) => CreateAnnotation("widget_url", name);
    public static string WidgetUsernamePasswordSecret(string? name = null) => CreateAnnotation("widget_username_password_secret", name);
    public static string Target(string? name = null) => CreateAnnotation("target", name);
    public static string Description(string? name = null) => CreateAnnotation("description", name);
    public static string Icon(string? name = null) => CreateAnnotation("icon", name);
    public static string Healthcheck(string? name = null) => CreateAnnotation("healthCheck", name);
    public static string AppName(string? name = null) => CreateAnnotation("appName", name);

    private static string CreateAnnotation(string label, string? name = null) => name is not null ? Path.Combine(Base, name, label) : Path.Combine(Base, label);
    private const string Base = "homepagesc.io";
}