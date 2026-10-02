using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SharpAutomation.Tests;

/// <summary>
/// An SMTP server for the mail tests, in this process, on 127.0.0.1 and a port the system picks.
/// It accepts every message and keeps it, so no test reaches the network or a mailbox. With
/// <c>offerStartTls</c> it offers STARTTLS and answers it with a self-signed certificate made
/// for the run, which a client checking certificates must refuse.
/// </summary>
internal sealed class FakeSmtpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly bool _offerStartTls;
    private readonly List<string> _commands = [];
    private readonly List<string> _messages = [];
    private readonly Task _accepting;

    public FakeSmtpServer(bool offerStartTls = false)
    {
        _offerStartTls = offerStartTls;
        if (offerStartTls) Certificate = SelfSigned();
        _listener.Start();
        _accepting = Task.Run(AcceptAsync);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>The certificate STARTTLS presents, when it is offered.</summary>
    public X509Certificate2? Certificate { get; }

    /// <summary>Each command line received, prefixed "TLS " once the connection is encrypted.</summary>
    public IReadOnlyList<string> Commands { get { lock (_commands) return [.. _commands]; } }

    /// <summary>The DATA of each message accepted, headers and body.</summary>
    public IReadOnlyList<string> Messages { get { lock (_messages) return [.. _messages]; } }

    /// <summary>Waits up to five seconds for a command, since the client may send it after Send returns.</summary>
    public bool Received(string command)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (Commands.Any(c => c.EndsWith(command, StringComparison.OrdinalIgnoreCase))) return true;
            Thread.Sleep(20);
        }
        return false;
    }

    public void Dispose()
    {
        _listener.Stop();
        try { _accepting.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
        Certificate?.Dispose();
    }

    private async Task AcceptAsync()
    {
        while (true)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(); }
            catch (Exception e) when (e is SocketException or ObjectDisposedException) { return; }
            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using var _ = client;
        Stream stream = client.GetStream();
        bool tls = false;
        try
        {
            await WriteAsync(stream, "220 fake.invalid ESMTP");
            var data = new StringBuilder();
            bool inData = false;
            while (await ReadLineAsync(stream) is { } line)
            {
                if (inData)
                {
                    if (line == ".")
                    {
                        inData = false;
                        lock (_messages) _messages.Add(data.ToString());
                        data.Clear();
                        await WriteAsync(stream, "250 accepted");
                    }
                    else data.Append(line).Append("\r\n");
                    continue;
                }
                lock (_commands) _commands.Add((tls ? "TLS " : "") + line);
                string verb = line.Split(' ')[0].ToUpperInvariant();
                switch (verb)
                {
                    case "EHLO":
                        await WriteAsync(stream, _offerStartTls && !tls ? "250-fake.invalid\r\n250 STARTTLS" : "250 fake.invalid");
                        break;
                    case "STARTTLS" when _offerStartTls && !tls:
                        await WriteAsync(stream, "220 ready to start TLS");
                        var ssl = new SslStream(stream);
                        await ssl.AuthenticateAsServerAsync(Certificate!, false, SslProtocols.None, false);
                        stream = ssl;
                        tls = true;
                        break;
                    case "DATA":
                        inData = true;
                        await WriteAsync(stream, "354 end with .");
                        break;
                    case "QUIT":
                        await WriteAsync(stream, "221 bye");
                        return;
                    default:
                        await WriteAsync(stream, "250 ok");
                        break;
                }
            }
        }
        catch (Exception e) when (e is IOException or AuthenticationException or ObjectDisposedException or SocketException)
        {
            // The client went away, or refused the certificate; the test looks at what arrived.
        }
    }

    private static async Task WriteAsync(Stream stream, string reply)
    {
        var bytes = Encoding.ASCII.GetBytes(reply + "\r\n");
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    // One byte at a time, so nothing is read ahead of the STARTTLS handshake.
    private static async Task<string?> ReadLineAsync(Stream stream)
    {
        var line = new List<byte>();
        var one = new byte[1];
        while (true)
        {
            if (await stream.ReadAsync(one) == 0) return line.Count == 0 ? null : Encoding.ASCII.GetString([.. line]);
            if (one[0] == '\n')
            {
                if (line.Count > 0 && line[^1] == '\r') line.RemoveAt(line.Count - 1);
                return Encoding.ASCII.GetString([.. line]);
            }
            line.Add(one[0]);
        }
    }

    private static X509Certificate2 SelfSigned()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=fake.invalid", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));
        // SslStream on Windows needs the key in a form it can use, which a round trip through PFX gives.
        byte[] pfx = created.Export(X509ContentType.Pfx);
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfx, null);
#else
        return new X509Certificate2(pfx);
#endif
    }
}
