namespace SharpAutomation.Tests;

public class NotificationTests
{
    // Every address and server here is reserved for examples or documentation: nothing is sent.
    private static readonly SMTPServerConfiguration Smtp = new("smtp.example.invalid", "robot@example.com");

    [Fact]
    public void Send_refuses_a_mail_without_recipients_before_connecting()
    {
        var mail = new NotificationConfiguration([], "Subject", "<p>Body</p>");

        var error = Assert.Throws<ArgumentException>(() => Notification.Send(Smtp, mail));
        Assert.Contains("toAddresses", error.Message);
        Assert.Throws<ArgumentException>(() => mail.SendNotification(Smtp));
    }

    [Fact]
    public void Configuration_keeps_the_lists_it_is_given()
    {
        List<string> to = ["operations@example.com"];
        var mail = new NotificationConfiguration(to, "Subject", "<p>Body</p>");

        to.Add("lead@example.com");

        Assert.Same(to, mail.ToAddresses);
        Assert.Equal(2, mail.ToAddresses.Count);
        Assert.Empty(mail.CCAddresses);
        Assert.Empty(mail.Attachments);
        Assert.Empty(mail.ReplyTo);
    }
}
