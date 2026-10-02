# Changelog

Each release's notes. The Publish workflow takes the section for the
version it releases as the GitHub release's body, so a section is written
here before the version is tagged.

The sections up to 1.0.6.3 were gathered from the nuget.org version history,
with the UTC date the nuget.org catalog records for each upload. Neither
nuget.org nor the READMEs carried release notes for them. Versions 1.0.0 to
1.0.6.1 are unlisted on nuget.org; 1.0.6.2 and 1.0.6.3 are listed. 1.0.0 to
1.0.6.2 target net7.0, and 1.0.6.3 targets net8.0.

## [2.0.0] - 2026-10-02

Mail can use STARTTLS and any port, and closes its attachment files and the session when it is done. `ToHTML` encodes what it puts into a mail. Log writes finish before they return, take turns on a shared file, wait briefly for a file another process holds, and cannot be forged by line breaks in logged text. `TryAction.RunAsync` awaits `async` lambdas and can be cancelled. Some of the fixes change what callers see, hence the major version. The library has no package dependencies.

### Breaking changes

* `ToHTML` HTML-encodes each exception's type name, message and stack trace. A message that holds markup on purpose, such as `<b>Disk full</b>`, now shows its tags as text in the mail.
* `TryAction.Run` and `RunAsync` throw `ArgumentOutOfRangeException` before calling the action for a negative `retries`, which used to return `false` without calling it, and for a negative `waitBetweenTriesSeconds` or one above 2,147,483, which used to throw only after a failed attempt, or wait the wrong time, and returned `true` when the first attempt succeeded. `Run(null)` throws `ArgumentNullException` instead of returning `false`.
* An `async` lambda passed to `TryAction.RunAsync` binds, when the calling code is compiled again, to the new overload for a `Func<Task>`, so the call waits for the lambda and collects what it throws. It used to return `true` at the lambda's first `await`.
* In a log file, every line after the first of a multi-line entry, message or stack trace starts with two spaces.
* The default file name of `Log.WriteEntryAsync` uses a 24-hour clock and the invariant culture: 1:05:09 PM gives `yyyyMMdd_130509.log`, not `yyyyMMdd_010509.log`.

### Fixes

* `ToHTML` put exception text into the mail's HTML as it was, so a message quoting outside input could add markup. Each exception's table rows are now balanced too; there was one `</tr>` too many.
* Mail always went to port 25 without TLS, so the message, its attachments and the exception details in it crossed the network in clear text, even to a server offering STARTTLS.
* `Notification.Send` left each attachment file open until the garbage collector closed it, so on Windows the file could not be moved or deleted afterwards, and ended the session without `QUIT`. Both are disposed now, on failure as well.
* `Log.WriteEntryAsync` was `async void`: it could return before a long entry was written, and an exception from the write ended the process. It writes before it returns and throws to the caller, keeping its signature.
* Two writes to one log file at the same time threw `IOException` on Windows. Writes from one process take turns, and a write waits up to about two seconds for a file another process holds.
* A line break in logged text started a line of its own, so text from outside could pass for a separate log entry.
* The default log file name used a 12-hour clock with no AM or PM.
* `TryAction.Run` and `RunAsync` waited after the last failed attempt as well. They return at once.
* Null arguments threw `NullReferenceException` from `ToHTML`, the single-exception `ToJSON` and `ToLogAsync`, and `Notification.Send`; they throw `ArgumentNullException` naming the parameter. The other exception list methods named LINQ's `source` instead of `exceptions`. A list holding a null exception throws `ArgumentException` before it is used.
* A clean clone did not build: the packed readme, `nugetREADME.md`, was ignored and never committed.
* The build is deterministic, so the same commit gives the same dll.

### Additions

* The package targets net8.0, net9.0 and net10.0. .NET 8 leaves support on 2026-11-10, and a later release will drop net8.0.
* `SMTPServerConfiguration.Port` (default 25) and `SMTPServerConfiguration.EnableSsl` (default false). With `EnableSsl` the connection switches to TLS with STARTTLS, and the server's certificate is checked, before any address or content is sent.
* `TryAction.RunAsync(Func<Task> action, int retries, int waitBetweenTriesSeconds, List<Exception>? _exceptionList, CancellationToken cancellationToken)` awaits each attempt and stops on cancellation.
* `Log.AppendEntryAsync(string entry, string logFilePath, CancellationToken cancellationToken)` writes an entry as a task to await.
* The READMEs are rewritten against the code, with samples that compile and run, and SECURITY.md says what the library reads, writes and reaches and how to use it safely.
* The package is built in CI from the tagged commit, tested on all three frameworks, scanned for vulnerabilities and malware, and published through nuget.org trusted publishing. The GitHub release carries the package's signed build provenance.

## [1.0.6.3] - 2024-12-13

No notes were recorded.

## [1.0.6.2] - 2023-09-29

No notes were recorded.

## [1.0.6.1] - 2023-09-29

No notes were recorded.

## [1.0.6] - 2023-09-29

No notes were recorded.

## [1.0.5] - 2023-09-27

No notes were recorded.

## [1.0.4] - 2023-09-27

No notes were recorded.

## [1.0.3] - 2023-09-26

No notes were recorded.

## [1.0.2] - 2023-09-26

No notes were recorded.

## [1.0.1] - 2023-09-26

No notes were recorded.

## [1.0.0] - 2023-09-26

No notes were recorded.
