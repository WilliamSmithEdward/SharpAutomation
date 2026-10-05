# SharpAutomation

[![NuGet version](https://img.shields.io/nuget/v/SharpAutomation)](https://www.nuget.org/packages/SharpAutomation)
[![Downloads](https://img.shields.io/nuget/dt/SharpAutomation)](https://www.nuget.org/packages/SharpAutomation)
[![CI](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/ci.yml)
[![Security](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/security.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/security.yml)
[![Malware scan](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/malware-scan.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/malware-scan.yml)
[![OpenSSF Scorecard](https://img.shields.io/ossf-scorecard/github.com/WilliamSmithEdward/SharpAutomation)](https://scorecard.dev/viewer/?uri=github.com/WilliamSmithEdward/SharpAutomation)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/WilliamSmithEdward/SharpAutomation/blob/main/LICENSE)

SharpAutomation is a small library for automation scripts and scheduled jobs. It runs an action with retries and collects the exceptions it throws, turns exceptions into HTML, JSON or log text, appends entries to log files, and sends HTML mail through an SMTP server.

```
dotnet add package SharpAutomation
```

Everything is in the `SharpAutomation` namespace. The package targets net8.0, net9.0 and net10.0 and has no dependencies. .NET 8 leaves Microsoft support on 2026-11-10, and a later release will drop net8.0.

---

## Retry an action

```csharp
using SharpAutomation;

var errors = new List<Exception>();
int attempts = 0;

bool ok = TryAction.Run(() =>
{
    attempts++;
    if (attempts < 3) throw new TimeoutException($"Attempt {attempts} timed out");
}, retries: 4, waitBetweenTriesSeconds: 1, _exceptionList: errors);

Console.WriteLine(ok);                        // True
Console.WriteLine(attempts);                  // 3
Console.WriteLine(errors.FlattenMessages());
// Attempt 1 timed out
// Attempt 2 timed out
```

`TryAction.RunAsync` does the same on the thread pool and waits with `Task.Delay` instead of blocking the thread:

```csharp
using SharpAutomation;

var errors = new List<Exception>();

bool copied = await TryAction.RunAsync(
    () => File.Copy("report.csv", "archive.csv", overwrite: true),
    retries: 2,
    waitBetweenTriesSeconds: 1,
    _exceptionList: errors);

Console.WriteLine(copied);        // True, or False after three failed attempts
Console.WriteLine(errors.Count);  // 0, or one exception per failed attempt
```

An `async` lambda, or any lambda that returns a task, goes to the `RunAsync` overload for a `Func<Task>`. It awaits each attempt, catches what the task ends with, and takes a cancellation token:

```csharp
using SharpAutomation;

var errors = new List<Exception>();
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

bool saved = await TryAction.RunAsync(async () =>
{
    await File.WriteAllTextAsync("status.txt", "Import finished", timeout.Token);
}, retries: 3, waitBetweenTriesSeconds: 10, _exceptionList: errors, cancellationToken: timeout.Token);

Console.WriteLine(saved);         // True
```

## Work with the exceptions

```csharp
using SharpAutomation;

var errors = new List<Exception>
{
    new TimeoutException("The server did not answer"),
    new IOException("The disk is full"),
    new TimeoutException("The server did not answer again"),
};

Console.WriteLine(errors.ContainsType<TimeoutException>());        // True
Console.WriteLine(errors.ContainsType<Exception>());               // False: the type must match exactly
Console.WriteLine(errors.FilterByType<TimeoutException>().Count);  // 2

foreach (var (typeName, count) in errors.CountByType())
{
    Console.WriteLine($"{typeName}: {count}");
}
// System.TimeoutException: 2
// System.IO.IOException: 1

Console.WriteLine(errors.FlattenMessages());
// The server did not answer
// The disk is full
// The server did not answer again

Console.WriteLine(new IOException("The disk is full").ToJSON());
// [
//   {
//     "Message": "The disk is full",
//     "StackTrace": null,
//     "TypeName": "System.IO.IOException"
//   }
// ]
```

## Write log files

```csharp
using SharpAutomation;

var errors = new List<Exception>();
TryAction.Run(() => File.ReadAllText("missing.txt"), _exceptionList: errors);

await errors.ToLogAsync("errors.log");         // appends one block per exception
await errors[0].ToLogAsync("errors.log");      // the same for a single exception
await Log.AppendEntryAsync("Import finished", "import.log");

Console.WriteLine(File.ReadAllText("import.log"));
// Timestamp: 10/2/2026 3:04:05 PM   (local time, in the current culture)
// Entry: Import finished
//
// ----------------------------------------------------------------------------
```

## Send mail

```csharp
using SharpAutomation;

var errors = new List<Exception>();
TryAction.Run(() => File.ReadAllText("missing.txt"), _exceptionList: errors);

var smtp = new SMTPServerConfiguration("smtp.example.com", "robot@example.com")
{
    Port = 25,          // the default
    EnableSsl = true,   // STARTTLS, with the server's certificate checked
};

var mail = new NotificationConfiguration(
    toAddresses: ["operations@example.com"],
    subject: "Nightly import failed",
    htmlBody: "<p>The nightly import stopped.</p>" + errors.ToHTML(),
    ccAddresses: ["lead@example.com"],
    attachments: ["errors.log"],
    replyTo: ["helpdesk@example.com"]);

mail.SendNotification(smtp);   // the same as Notification.Send(smtp, mail)
```

---

## Retries

- `TryAction.Run(action, retries, waitBetweenTriesSeconds, _exceptionList)` calls `action` up to `retries + 1` times and returns `true` as soon as one call returns. It catches every exception the action throws, of any type, and adds it to `_exceptionList` when you pass one; without a list the exceptions are dropped. It returns `false` when every attempt threw.
- After an attempt that throws, `Run` blocks the thread for `waitBetweenTriesSeconds` seconds with `Thread.Sleep`, and `RunAsync` waits with `Task.Delay`, before the next attempt. There is no wait after the last attempt.
- `RunAsync(Action)` runs each attempt with `Task.Run`, on a thread-pool thread. `RunAsync(Func<Task>)` calls the delegate and awaits its task; the compiler picks it for an `async` lambda. Its `cancellationToken` stops the retries: once it is cancelled no further attempt starts, a wait ends early, and the returned task ends with `OperationCanceledException`, which is not added to the list. Code compiled against 1.0.6.3 that passes an `async` lambda calls the `Action` overload, which does not await it, until it is compiled again.
- `retries` and `waitBetweenTriesSeconds` default to 0, which means one attempt and no wait. A negative value of either, or a wait above 2,147,483 seconds, throws `ArgumentOutOfRangeException` before the action runs, and a null action throws `ArgumentNullException`.

## Exceptions as text

- `ToHTML`, `ToJSON` and `ToLogAsync` work on one exception or a `List<Exception>`, and write the type's full name, the `Message` and the `StackTrace` of each exception. They do not include `InnerException`, the inner exceptions of an `AggregateException`, or `Data`.
- `ToHTML` returns an HTML fragment, a heading and a table, for the body of a mail. It HTML-encodes the type name, message and stack trace, so a message such as `<b>Disk full</b>` shows as written, tags and all, and cannot add markup to the mail.
- `ToJSON` returns an indented JSON array of objects with `Message`, `StackTrace` and `TypeName`. `StackTrace` is `null` for an exception that was never thrown. `System.Text.Json` escapes `<`, `>` and `&` in the strings.
- `FilterByType<T>()` and `ContainsType<T>()` match the exact type: a `FileNotFoundException` is not counted as an `IOException`. `CountByType()` returns a dictionary from each type's full name to its count. `FlattenMessages()` joins the messages with `Environment.NewLine`.
- These methods throw `ArgumentNullException` for a null list or a null exception, and `ArgumentException` for a list that holds a null exception, before they use any of it.

## Log files

- `ToLogAsync(logFilePath)` appends one block per exception: `Timestamp:`, `Exception:`, `Message:` and `StackTrace:` lines, then a line of dashes. An empty list writes nothing. Without a path it appends to `Exceptions.log` in `AppDomain.CurrentDomain.BaseDirectory`, the application's folder.
- `Log.AppendEntryAsync(entry, logFilePath, cancellationToken)` appends a `Timestamp:` line, an `Entry:` line and a line of dashes, and returns a task to await. `Log.WriteEntryAsync(entry, logFilePath)` does the same synchronously: despite its name it returns `void` once the entry is written, and throws to the caller if it cannot be. Without a path both create a new file in the application's folder for each second, named from the local time as `yyyyMMdd_HHmmss.log` with a 24-hour clock, so each call usually writes its own file.
- A line break in an entry, a message or a stack trace is followed by two spaces in the file. Only the library starts a line at the first column, so logged text cannot pass for a separator or another entry.
- A relative path is resolved against the current directory, not the application's folder. The file is created when it does not exist, but its folder is not. Files are written as UTF-8 with a byte order mark.
- Timestamps are the local time, formatted in the current culture.
- Nothing rotates or deletes log files; they grow until you remove them.
- Writes to one file from the same process take turns. The file is shared only with readers while it is written; when another process has it open, a write waits and tries again for about two seconds, then throws `IOException`.

## Mail

- `Notification.Send(smtp, mail)` and `mail.SendNotification(smtp)` send one message through `System.Net.Mail.SmtpClient` to `SMTPServerAddress` on `Port`, 25 unless you set it. There is no setting for a user name and password, so the server has to accept mail from the machine without them.
- With `EnableSsl = true` the connection switches to TLS with STARTTLS before anything is sent. The server's certificate must be trusted by the machine and match `SMTPServerAddress`; a server that does not offer STARTTLS ends the send with `SmtpException`, and a certificate that fails the check with `AuthenticationException`, before any address or content is sent. `EnableSsl` is off by default, as it was in 1.0.6.3, and then the message, its attachments and any exception details in it cross the network unencrypted. Turn it on wherever the server offers STARTTLS.
- The message is HTML (`IsBodyHtml`), from `FromAddress`, to every address in `ToAddresses`, with `CCAddresses` as CC (empty strings are skipped), `ReplyTo` as reply-to addresses, and each path in `Attachments` attached as a file.
- `Send` closes the attachment files and ends the session with `QUIT` before it returns, whether the mail went or not, so the files can be moved or deleted straight away.
- `Send` throws `ArgumentNullException` for a null configuration or a null `ToAddresses`, and `ArgumentException` when `ToAddresses` is empty. An address that is not a mail address throws `FormatException`, an attachment that cannot be read throws the `IOException` from opening it, and a failed send throws `SmtpException`. `SmtpClient` gives up on a server that does not answer after 100 seconds.
- `System.Net.Mail` refuses a subject or an address that holds a line break, with `ArgumentException` or `FormatException`, so mail headers cannot be added through them.
- `NotificationConfiguration` keeps the lists you pass rather than copies, so adding to `ToAddresses` after construction adds a recipient.

## Security

- Exception messages and stack traces can hold file paths, server names, user names, and sometimes connection strings or other secrets. `ToHTML`, `ToJSON` and the log methods copy them unchanged, so send the mail and keep the log files only where those details may be read.
- An attachment path is read with the permissions of the process. Do not build one from input you do not control.

## Attributions

NuGet icon "SharpAutomation.png" designed by user Itim2101 on Freepik Company S.L. https://support.freepik.com/

## License

[MIT](https://github.com/WilliamSmithEdward/SharpAutomation/blob/main/LICENSE)
