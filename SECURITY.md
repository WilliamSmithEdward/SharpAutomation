# Security policy

## Reporting a vulnerability

Report a vulnerability privately, not in a public issue or pull request:
[open a private report](https://github.com/WilliamSmithEdward/SharpAutomation/security/advisories/new).
Only the maintainer sees it. Include the SharpAutomation version, the .NET
version and operating system, the method involved, and the smallest code
that shows it, with server names, addresses, credentials and private data
removed.

A confirmed vulnerability is fixed in a release on nuget.org, and the
advisory is published with it, crediting you unless you ask otherwise.

## Supported versions

Only the latest release on nuget.org receives security fixes. Older
releases are not maintained separately; update when a fix ships.

## Scope

SharpAutomation is a library. It opens no port, listens for nothing and
starts no process. What it reads, writes and reaches:

- **Network.** `Notification.Send` and `SendNotification` connect to the
  SMTP server and port named in `SMTPServerConfiguration`, through
  `System.Net.Mail.SmtpClient`, and send one message, over STARTTLS when
  `EnableSsl` is set. Nothing else in the library uses the network.
- **Files read.** `Notification.Send` reads each file named in
  `NotificationConfiguration.Attachments`, to attach it. Nothing else is
  read.
- **Files written.** `Log.WriteEntryAsync`, `Log.AppendEntryAsync` and
  `ToLogAsync` append to the file the caller names, or, without a name, to
  a file in `AppDomain.CurrentDomain.BaseDirectory`: `Exceptions.log` for
  `ToLogAsync`, a file named from the local time for the `Log` methods.
  Nothing else is written.
- **Code run.** `TryAction.Run` and `RunAsync` call the delegate the caller
  passes, and nothing else.

Every path, address and message comes from the calling program. Input that
makes the library read or write a file other than the ones above, send mail
to an address the caller did not give, add a mail header, or run code
counts as a vulnerability. So does mail, or any address in it, sent
without TLS when `EnableSsl` is set, or to a server whose certificate the
machine does not trust.

### Mail

Set `EnableSsl = true` on `SMTPServerConfiguration` wherever the server
offers STARTTLS. The connection is then encrypted before any address or
content is sent, and a server without STARTTLS, or with a certificate the
machine does not trust or that does not match the server's name, is
refused. `EnableSsl` is off by default, and version 1.0.6.3 had no such
setting: without it the message, its attachments and any exception details
in it cross the network in clear text, so use that only with a relay on a
network you trust. The library has no setting for a user name and
password. `System.Net.Mail` refuses a line break in a subject or an
address, so headers cannot be added through them.

### Exception text in mail and logs

Exception messages and stack traces can hold file paths, server and user
names, and sometimes connection strings or other secrets. `ToHTML`,
`ToJSON` and the log methods copy them unchanged, so send the mail and keep
the log files only where those details may be read. `ToHTML` HTML-encodes
what it inserts; in 1.0.6.3 it did not, so there a message that quotes
input you do not control can put markup into the mail.

### Log files

Nothing rotates or deletes the log files. Point them at a folder the
process may write and others may not, and remove old files yourself. A
line break in logged text is followed by two spaces in the file, so text
from outside cannot pass for a separate entry; in 1.0.6.3 it could.

## How the code is checked

Three workflows check every pull request and every push to `main`, and
their gates decide whether a change can merge: **CI passed**,
**Security passed** and **Malware scan passed**. A gate passes only when
every job before it did, and any unexpected finding fails it, whatever its
severity. Security also runs weekly, so new queries, rules and advisories
reach code that has not changed, and Malware scan runs daily, so new
signatures and rules reach files that have not changed.

- **Code:** CodeQL with GitHub's security-extended queries, for C#
  (extracted from a Release build of net8.0, net9.0 and net10.0) and GitHub
  Actions, and Semgrep with the default, C#, security-audit, secrets and
  GitHub Actions rule sets. Semgrep scans the library, the workflows that
  build and publish it, and the scripts in `scripts/security` that judge the
  scans. A `nosemgrep` comment cannot hide a finding. Results go to the
  repository's code scanning.
- **Workflows:** zizmor audits the GitHub Actions workflows; a finding fails
  Security.
- **Dependencies:** `dotnet list package --vulnerable --include-transitive`
  checks the packages the `packages.lock.json` files resolve, after the same
  locked restore CI builds with, and any known vulnerability fails Security.
  The library itself has no package dependencies.
- **Malware:** ClamAV, with signatures freshclam fetches and verifies on
  every run, and YARA-X, with the YARA Forge rules pinned to a release and
  its SHA-256, scan every file the commit holds and the .nupkg built from it
  with the locked restore, both as the archive and unpacked. On a release
  they scan the very .nupkg that is published. YARA-X runs YARA Forge's full
  rule set. A scan error fails the report as a match does.
- **OpenSSF Scorecard** rates the repository's security practices on every
  change to `main` and weekly, and the README badge shows the result.
  Its Code-Review and Contributors checks assume more than one
  maintainer, such as a second person approving every change, so a
  single-maintainer project cannot score full marks on them. Its Fuzzing
  check finds no C# fuzzer short of OSS-Fuzz or ClusterFuzzLite, so this
  repository has no fuzz workflow; the library parses no input it does not
  control.

## Accepted findings

A finding is fixed, or accepted with a written reason in
[.github/security/accepted.toml](https://github.com/WilliamSmithEdward/SharpAutomation/blob/main/.github/security/accepted.toml)
for CodeQL and Semgrep, or
[.github/security/malware-accepted.toml](https://github.com/WilliamSmithEdward/SharpAutomation/blob/main/.github/security/malware-accepted.toml)
for ClamAV and YARA-X. A finding entry matches on the tool, the rule and
the file, and in accepted.toml also the text of the flagged line, so an
edited line needs another review; a notice entry, for a warning a tool
raises about its own scan, matches on the tool, the warning and text the
message contains. An entry that no longer matches fails the report. zizmor
keeps its exceptions in `.github/zizmor.yml` or inline beside the line they
excuse, each with its reason.

The current entries: there are none in accepted.toml or
malware-accepted.toml. zizmor's `self-repository` and `superfluous-actions`
rules are turned off in `.github/zizmor.yml`, each with its reason and when
it comes back.

## Pinning and updates

Everything the workflows run is pinned: actions to full commit SHAs,
runners to named OS releases, scanner images to digests, Python tools to
hash-locked lock files, NuGet packages to `packages.lock.json` files,
restored in locked mode, the YARA Forge rules to a release and its SHA-256,
and the YARA-X engine to a release and its SHA-256. `global.json` sets the
.NET SDK's floor at 10.0.400 and lets it roll forward to a newer feature
band, so CI builds with the newest .NET 10 SDK. ClamAV's signatures change
too often to pin, so freshclam fetches and verifies them on every run.

Dependabot proposes updates to the GitHub Actions, the Semgrep and ClamAV
images, the hash-locked files in `.github/requirements`, the NuGet packages
and the .NET SDK in `global.json` once a version is a week old, and at once
for a security advisory. The Update YARA rules workflow proposes new YARA
pins in `.github/security/yara.json` each week. A minor or patch update,
and the YARA pull request, merges itself once CI, Security and Malware scan
pass; a third-party major version waits for review.

## Releases

A pushed `vX.Y.Z` tag builds the .nupkg with the locked restore, checks
that the tag matches the package version (`PackageVersion` in the csproj),
and runs Security and Malware scan on the tagged commit and that package.
Nothing is published unless all of them pass. The package goes to
nuget.org through trusted publishing, so no long-lived API key exists to
leak. The GitHub release, titled with the tag, carries the .nupkg,
`SharpAutomation-<version>-security-report.md` and
`SharpAutomation-<version>-malware-report.md` beside the scan results they
were made from, and the provenance bundle, with the version's section of
`CHANGELOG.md` as its notes. Started by hand, the Publish workflow is always
a dry run and publishes nothing.

### Verifying a download

Releases after 1.0.6.3 carry a GitHub build provenance attestation for the
.nupkg CI built. Check the copy attached to the GitHub release:

```
gh attestation verify SharpAutomation.<version>.nupkg --owner WilliamSmithEdward
```

The output names the commit and workflow run that built the file. The
signed bundle is also attached to the release as
`SharpAutomation-<version>.sigstore.json`, so the check works without asking
GitHub for it: add `--bundle SharpAutomation-<version>.sigstore.json`.

nuget.org adds its own repository signature to every package it serves,
which changes the file, so the copy from nuget.org does not match the
attestation. Check that copy's signature with `dotnet nuget verify`.

## Repository settings

<!-- repo-standards:begin security-settings. Copied from WilliamSmithEdward/repo-standards, templates/security/settings-block.md. Change it there; the weekly rescan fails a copy that differs. -->
- `main` accepts changes only through a pull request that passes
  **CI passed**, **Security passed** and **Malware scan passed**. The
  ruleset has no bypass, for the owner either, and refuses force-pushes and
  deleting the branch.
- A `v*` release tag cannot be moved or deleted once pushed, except by a
  repository admin.
- A workflow that uses an action not pinned to a full commit SHA fails to
  run. Workflow tokens are read-only unless a job is granted more for
  itself.
- Secret scanning with push protection, Dependabot alerts and security
  updates, and private vulnerability reporting are on.
<!-- repo-standards:end -->
