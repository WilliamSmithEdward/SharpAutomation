using System.Net;
using System.Net.Mail;

namespace SharpAutomation.Tests;

[CollectionDefinition(nameof(MailTransportTests), DisableParallelization = true)]
public sealed class MailTransportCollection;

// Not run beside other tests: one test sets the process-wide certificate callback.
[Collection(nameof(MailTransportTests))]
public class MailTransportTests
{
    private static NotificationConfiguration Mail() =>
        new(["operations@example.com"], "Nightly import failed", "<p>The import stopped.</p>");

    [Fact]
    public void The_default_port_is_25_and_TLS_is_off()
    {
        var smtp = new SMTPServerConfiguration("smtp.example.invalid", "robot@example.com");

        Assert.Equal(25, smtp.Port);
        Assert.False(smtp.EnableSsl);
    }

    [Fact]
    public void Send_uses_the_configured_port()
    {
        using var server = new FakeSmtpServer();
        var smtp = new SMTPServerConfiguration("127.0.0.1", "robot@example.com") { Port = server.Port };

        Notification.Send(smtp, Mail());

        var message = Assert.Single(server.Messages);
        Assert.Contains("Subject: Nightly import failed", message);
        Assert.Contains("MAIL FROM:<robot@example.com>", server.Commands);
        Assert.Contains("RCPT TO:<operations@example.com>", server.Commands);
        Assert.DoesNotContain(server.Commands, c => c.StartsWith("STARTTLS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void With_TLS_on_an_untrusted_certificate_is_refused_before_anything_is_sent()
    {
        using var server = new FakeSmtpServer(offerStartTls: true);
        var smtp = new SMTPServerConfiguration("127.0.0.1", "robot@example.com") { Port = server.Port, EnableSsl = true };

        Assert.Throws<System.Security.Authentication.AuthenticationException>(() => Mail().SendNotification(smtp));

        Assert.Contains("STARTTLS", server.Commands);
        Assert.DoesNotContain(server.Commands, c => c.Contains("MAIL FROM", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(server.Messages);
    }

    [Fact]
    public void With_TLS_on_a_server_without_STARTTLS_is_refused_before_anything_is_sent()
    {
        using var server = new FakeSmtpServer(offerStartTls: false);
        var smtp = new SMTPServerConfiguration("127.0.0.1", "robot@example.com") { Port = server.Port, EnableSsl = true };

        Assert.Throws<SmtpException>(() => Mail().SendNotification(smtp));

        Assert.DoesNotContain(server.Commands, c => c.Contains("MAIL FROM", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(server.Messages);
    }

    [Fact]
    public void With_TLS_on_the_message_goes_over_the_encrypted_connection()
    {
        using var server = new FakeSmtpServer(offerStartTls: true);
        var smtp = new SMTPServerConfiguration("127.0.0.1", "robot@example.com") { Port = server.Port, EnableSsl = true };
        string thumbprint = server.Certificate!.Thumbprint;

#pragma warning disable SYSLIB0014 // SmtpClient takes its certificate check from ServicePointManager.
        var previous = ServicePointManager.ServerCertificateValidationCallback;
        // Trusts this test's own certificate and nothing else.
        ServicePointManager.ServerCertificateValidationCallback = (_, certificate, _, _) =>
            certificate is not null && certificate.GetCertHashString() == thumbprint;
        try
        {
            Mail().SendNotification(smtp);
        }
        finally
        {
            ServicePointManager.ServerCertificateValidationCallback = previous;
        }
#pragma warning restore SYSLIB0014

        Assert.Single(server.Messages);
        Assert.Contains("STARTTLS", server.Commands);
        Assert.Contains("TLS MAIL FROM:<robot@example.com>", server.Commands);
        Assert.DoesNotContain("MAIL FROM:<robot@example.com>", server.Commands);
    }
}
