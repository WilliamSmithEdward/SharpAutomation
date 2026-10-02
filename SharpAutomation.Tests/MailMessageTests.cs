namespace SharpAutomation.Tests;

// What Send does with the message, the attachments and the connection, against FakeSmtpServer.
public class MailMessageTests
{
    private static SMTPServerConfiguration Smtp(FakeSmtpServer server) =>
        new("127.0.0.1", "robot@example.com") { Port = server.Port };

    [Fact]
    public void Send_delivers_every_part_of_the_mail()
    {
        using var folder = new TempFolder();
        string attachment = folder.File("errors.log");
        File.WriteAllText(attachment, "attached text");
        using var server = new FakeSmtpServer();
        var mail = new NotificationConfiguration(
            ["operations@example.com"], "Nightly import failed", "<p>The import stopped.</p>",
            ccAddresses: ["lead@example.com", ""], attachments: [attachment], replyTo: ["helpdesk@example.com"]);

        Notification.Send(Smtp(server), mail);

        string message = Assert.Single(server.Messages);
        Assert.Contains("RCPT TO:<operations@example.com>", server.Commands);
        Assert.Contains("RCPT TO:<lead@example.com>", server.Commands);
        Assert.Contains("Subject: Nightly import failed", message);
        Assert.Contains("Reply-To: helpdesk@example.com", message);
        Assert.Contains("Content-Type: text/html", message);
        Assert.Contains("name=errors.log", message);
    }

    [Fact]
    public void Send_closes_the_connection_with_QUIT()
    {
        using var server = new FakeSmtpServer();

        new NotificationConfiguration(["operations@example.com"], "Subject", "<p>Body</p>").SendNotification(Smtp(server));

        Assert.True(server.Received("QUIT"));
    }

    [Fact]
    public void Send_releases_the_attachment_files()
    {
        using var folder = new TempFolder();
        string attachment = folder.File("errors.log");
        File.WriteAllText(attachment, "attached text");
        using var server = new FakeSmtpServer();
        var mail = new NotificationConfiguration(["operations@example.com"], "Subject", "<p>Body</p>", attachments: [attachment]);

        Notification.Send(Smtp(server), mail);

        // Fails while anything in the process still has the file open.
        using (new FileStream(attachment, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        File.Delete(attachment);
        Assert.False(File.Exists(attachment));
    }

    [Fact]
    public void Send_releases_the_attachment_files_when_sending_fails()
    {
        using var folder = new TempFolder();
        string attachment = folder.File("errors.log");
        File.WriteAllText(attachment, "attached text");
        var server = new FakeSmtpServer();
        var smtp = Smtp(server);
        server.Dispose();   // nothing listens on the port now, so the send fails
        var mail = new NotificationConfiguration(["operations@example.com"], "Subject", "<p>Body</p>", attachments: [attachment]);

        Assert.Throws<System.Net.Mail.SmtpException>(() => Notification.Send(smtp, mail));

        using (new FileStream(attachment, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
    }

    [Fact]
    public void A_null_argument_throws_ArgumentNullException_naming_it()
    {
        var smtp = new SMTPServerConfiguration("smtp.example.invalid", "robot@example.com");
        var mail = new NotificationConfiguration(["operations@example.com"], "Subject", "<p>Body</p>");

        Assert.Equal("smtpConfiguration", Assert.Throws<ArgumentNullException>(() => Notification.Send(null!, mail)).ParamName);
        Assert.Equal("notificationConfiguration", Assert.Throws<ArgumentNullException>(() => Notification.Send(smtp, null!)).ParamName);
        Assert.Equal("toAddresses", Assert.Throws<ArgumentNullException>(
            () => Notification.Send(smtp, new NotificationConfiguration(null!, "Subject", "<p>Body</p>"))).ParamName);
    }
}
