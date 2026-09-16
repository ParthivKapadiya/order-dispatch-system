using FluentAssertions;
using ToplandERP.Application.Notifications;
using ToplandERP.Domain.Enums;

namespace ToplandERP.UnitTests.Application;

public class RelativeTimeFormatterTests
{
    [Fact]
    public void Formats_just_now_minutes_hours_and_yesterday()
    {
        var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        RelativeTimeFormatter.ToRelative(now.AddSeconds(-10), now).Should().Be("Just now");
        RelativeTimeFormatter.ToRelative(now.AddMinutes(-5), now).Should().Be("5 minutes ago");
        RelativeTimeFormatter.ToRelative(now.AddHours(-1), now).Should().Be("1 hour ago");
        RelativeTimeFormatter.ToRelative(now.AddDays(-1), now).Should().Be("Yesterday");
    }

    [Fact]
    public void Modification_rejected_message_includes_reason()
    {
        var copy = NotificationMessages.Compose(
            NotificationType.ModificationRejected,
            "GRAVIS-20260915-0001",
            rejectionReason: "Order already prepared for dispatch.");
        copy.Title.Should().Be("Modification Rejected");
        copy.Message.Should().Contain("GRAVIS-20260915-0001");
        copy.Message.Should().Contain("Order already prepared for dispatch.");
    }
}
