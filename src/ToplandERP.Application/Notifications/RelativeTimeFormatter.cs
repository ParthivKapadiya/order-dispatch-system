namespace ToplandERP.Application.Notifications;

public static class RelativeTimeFormatter
{
    public static string ToRelative(DateTime utcTimestamp, DateTime? utcNow = null)
    {
        var timestamp = utcTimestamp.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc)
            : utcTimestamp.ToUniversalTime();
        var now = utcNow ?? DateTime.UtcNow;
        if (now.Kind == DateTimeKind.Unspecified)
        {
            now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        }
        else
        {
            now = now.ToUniversalTime();
        }

        var elapsed = now - timestamp;
        if (elapsed.TotalSeconds < 45)
        {
            return "Just now";
        }

        if (elapsed.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes));
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }

        if (elapsed.TotalHours < 24)
        {
            var hours = Math.Max(1, (int)Math.Floor(elapsed.TotalHours));
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        if (timestamp.Date == now.Date.AddDays(-1))
        {
            return "Yesterday";
        }

        if (elapsed.TotalDays < 7)
        {
            var days = Math.Max(2, (int)Math.Floor(elapsed.TotalDays));
            return $"{days} days ago";
        }

        return timestamp.ToString("dd MMM yyyy");
    }
}
