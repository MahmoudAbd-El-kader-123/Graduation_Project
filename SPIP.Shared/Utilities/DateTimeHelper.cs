namespace SPIP.Shared.Utilities;

public static class DateTimeHelper
{
    public static DateTime UtcNow() => DateTime.UtcNow;

    public static string ToDisplayFormat(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
}
