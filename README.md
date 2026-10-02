# SharpAutomation

[![NuGet version](https://img.shields.io/nuget/v/SharpAutomation)](https://www.nuget.org/packages/SharpAutomation)
[![Downloads](https://img.shields.io/nuget/dt/SharpAutomation)](https://www.nuget.org/packages/SharpAutomation)
[![CI](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/ci.yml)
[![Security](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/security.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/security.yml)
[![Malware scan](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/malware-scan.yml/badge.svg?branch=main)](https://github.com/WilliamSmithEdward/SharpAutomation/actions/workflows/malware-scan.yml)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/WilliamSmithEdward/SharpAutomation/badge)](https://scorecard.dev/viewer/?uri=github.com/WilliamSmithEdward/SharpAutomation)
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

`RunAsync` takes an `Action`, so pass it synchronous code only. An `async` lambda passed to it becomes an `async void` method; see "Known problems in 1.0.6.3" below.

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
Log.WriteEntryAsync("Import finished", "import.log");

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

var smtp = new SMTPServerConfiguration("smtp.example.com", "robot@example.com");

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
- After each attempt that throws, `Run` blocks the thread for `waitBetweenTriesSeconds` seconds with `Thread.Sleep`, and `RunAsync` waits with `Task.Delay`. Both also wait after the last attempt, before returning `false`.
- `RunAsync` runs each attempt with `Task.Run`, on a thread-pool thread. It has no cancellation token.
- `retries` and `waitBetweenTriesSeconds` default to 0, which means one attempt and no wait.

## Exceptions as text

- `ToHTML`, `ToJSON` and `ToLogAsync` work on one exception or a `List<Exception>`, and write the type's full name, the `Message` and the `StackTrace` of each exception. They do not include `InnerException`, the inner exceptions of an `AggregateException`, or `Data`.
- `ToHTML` returns an HTML fragment, a heading and a table, for the body of a mail. It inserts the type name, message and stack trace as they are, without HTML encoding.
- `ToJSON` returns an indented JSON array of objects with `Message`, `StackTrace` and `TypeName`. `StackTrace` is `null` for an exception that was never thrown. `System.Text.Json` escapes `<`, `>` and `&` in the strings.
- `FilterByType<T>()` and `ContainsType<T>()` match the exact type: a `FileNotFoundException` is not counted as an `IOException`. `CountByType()` returns a dictionary from each type's full name to its count. `FlattenMessages()` joins the messages with `Environment.NewLine`.
- These methods throw `ArgumentNullException` for a null list, except `ToHTML`, which throws `NullReferenceException`.

## Log files

- `ToLogAsync(logFilePath)` appends one block per exception: `Timestamp:`, `Exception:`, `Message:` and `StackTrace:` lines, then a line of dashes. An empty list writes nothing. Without a path it appends to `Exceptions.log` in `AppDomain.CurrentDomain.BaseDirectory`, the application's folder.
- `Log.WriteEntryAsync(entry, logFilePath)` appends a `Timestamp:` line, an `Entry:` line and a line of dashes. Without a path it creates a new file in the application's folder for each second, named from the local time as `yyyyMMdd_hhmmss.log`, so each call usually writes its own file.
- A relative path is resolved against the current directory, not the application's folder. The file is created when it does not exist, but its folder is not. Files are written as UTF-8 with a byte order mark.
- Timestamps are the local time, formatted in the current culture.
- Nothing rotates or deletes log files; they grow until you remove them.
- The file is opened for writing and shared only with readers. On Windows, a second write to the same file while the first still has it open, from this process or another, throws `IOException`.
- `Log.WriteEntryAsync` returns `void`, so it cannot be awaited and an exception it throws is not returned to you; see "Known problems in 1.0.6.3" below.

## Mail

- `Notification.Send(smtp, mail)` and `mail.SendNotification(smtp)` send one message through `System.Net.Mail.SmtpClient` to `SMTPServerAddress` on port 25. There is no setting for the port, TLS or a user name and password: the message goes unencrypted and unauthenticated, so it suits a relay on a network you trust.
- The message is HTML (`IsBodyHtml`), from `FromAddress`, to every address in `ToAddresses`, with `CCAddresses` as CC (empty strings are skipped), `ReplyTo` as reply-to addresses, and each path in `Attachments` attached as a file.
- `Send` throws `ArgumentException` when `ToAddresses` is empty. An address that is not a mail address throws `FormatException`, an attachment that cannot be read throws the `IOException` from opening it, and a failed send throws `SmtpException`. `SmtpClient` gives up on a server that does not answer after 100 seconds.
- `System.Net.Mail` refuses a subject or an address that holds a line break, with `ArgumentException` or `FormatException`, so mail headers cannot be added through them.
- `NotificationConfiguration` keeps the lists you pass rather than copies, so adding to `ToAddresses` after construction adds a recipient.

## Security

- Exception messages and stack traces can hold file paths, server names, user names, and sometimes connection strings or other secrets. `ToHTML`, `ToJSON` and the log methods copy them unchanged, so send the mail and keep the log files only where those details may be read.
- `ToHTML` does not encode what it inserts. A message that holds HTML, such as one that quotes input from a file or a web response, becomes part of the mail's HTML.
- A line break in a logged entry or message starts a new line in the log file, so text that you log can look like another entry.
- An attachment path is read with the permissions of the process. Do not build one from input you do not control.

## Known problems in 1.0.6.3

These are fixed in the next release.

- `ToHTML` does not HTML-encode the exception's type name, message or stack trace, and closes each exception's table rows once more than it opens them.
- Mail cannot use TLS or a port other than 25.
- `Notification.Send` does not dispose the message or the `SmtpClient`, so attachment files stay open, and cannot be deleted on Windows, until the garbage collector closes them, and the connection to the server is dropped without a `QUIT`.
- `Log.WriteEntryAsync` is `async void`. An exception from it, such as `DirectoryNotFoundException` for a missing folder, is thrown on the thread pool and ends the process. A long entry may still be being written when it returns.
- On Windows, two writes to the same log file at once, from `Log.WriteEntryAsync` or `ToLogAsync` in one process, throw `IOException`, which ends the process for `Log.WriteEntryAsync`.
- A line break in a logged message or entry can make text look like a separate log entry.
- The default log file name uses a 12-hour clock with no AM or PM: a file written at 1:05 PM is named as if written at 1:05 AM, so the names do not sort by time.
- An `async` lambda passed to `TryAction.RunAsync` becomes `async void`: `RunAsync` returns `true` when the lambda reaches its first `await`, does not wait for it to finish, and an exception it throws later ends the process instead of reaching the exception list.
- `TryAction.Run` and `RunAsync` wait `waitBetweenTriesSeconds` after the last failed attempt too.
- A negative `retries` returns `false` without calling the action. A negative `waitBetweenTriesSeconds` throws `ArgumentOutOfRangeException` from `Run` or `RunAsync` after the first failed attempt, and one above 2,147,483 overflows when converted to milliseconds, so it throws the same or waits the wrong time.
- `ToHTML` on a null list, and `Notification.Send` with a null configuration or a null `ToAddresses`, throw `NullReferenceException`.

## Attributions

NuGet icon "SharpAutomation.png" designed by user Itim2101 on Freepik Company S.L. https://support.freepik.com/

## License

[MIT](https://github.com/WilliamSmithEdward/SharpAutomation/blob/main/LICENSE)
