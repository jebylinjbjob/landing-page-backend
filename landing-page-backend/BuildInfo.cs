using System.Diagnostics;
using System.Text;

namespace landing_page_backend;

public static class BuildInfo
{
    private static readonly Lazy<GitMeta> _meta = new(GetGitMeta, isThreadSafe: true);

    public static string SwaggerDescription => _meta.Value.SwaggerDescription;

    private static GitMeta GetGitMeta()
    {
        try
        {
            // Preferred: use GitInfo build-time generated constants.
            // This works even when `.git` / git executable is not available at runtime.
            var gitInfoMeta = TryGetGitInfoMeta();
            if (gitInfoMeta is not null)
                return gitInfoMeta;

            // Prefer calling git from likely working directories; git will locate the repo root itself.
            // (This avoids relying on `.git` being present at build/publish time.)
            var candidates = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var candidate in candidates)
            {
                foreach (var workingDir in EnumerateUpwardDirectories(candidate, maxDepth: 30))
                {
                    var commitShort = RunGit(workingDir, "rev-parse --short HEAD");
                    if (string.IsNullOrWhiteSpace(commitShort))
                        continue;

                    var commitFull = RunGit(workingDir, "rev-parse HEAD") ?? "unknown";
                    var branch = RunGit(workingDir, "rev-parse --abbrev-ref HEAD") ?? "unknown";

                    // author | iso8601 date (commit time) | subject
                    var log = RunGit(workingDir, "log -1 --pretty=format:%an|%cI|%s");
                    string author = "unknown";
                    string dateUtcIso = "unknown";
                    string message = "unknown";

                    if (!string.IsNullOrWhiteSpace(log))
                    {
                        var parts = log.Split('|', 3);
                        if (parts.Length > 0) author = parts[0].Trim();
                        if (parts.Length > 1) dateUtcIso = parts[1].Trim();
                        if (parts.Length > 2) message = parts[2].Trim();
                    }

                    return new GitMeta(
                        GitCommitShort: commitShort.Trim(),
                        GitCommitFull: commitFull.Trim(),
                        GitBranch: branch.Trim(),
                        LastCommitAuthor: author,
                        LastCommitMessage: message,
                        LastCommitDateUtcIso: dateUtcIso,
                        DiscoveryCurrentDir: Directory.GetCurrentDirectory(),
                        DiscoveryBaseDir: AppContext.BaseDirectory
                    );
                }
            }

            return GitMeta.Unknown(
                DiscoveryCurrentDir: Directory.GetCurrentDirectory(),
                DiscoveryBaseDir: AppContext.BaseDirectory
            );
        }
        catch
        {
            return GitMeta.Unknown(
                DiscoveryCurrentDir: Directory.GetCurrentDirectory(),
                DiscoveryBaseDir: AppContext.BaseDirectory
            );
        }
    }

    private static GitMeta? TryGetGitInfoMeta()
    {
        try
        {
            var commitFull = global::ThisAssembly.Git.Commit;
            if (string.IsNullOrWhiteSpace(commitFull) || string.Equals(commitFull, "unknown", StringComparison.OrdinalIgnoreCase))
                return null;

            var commitShort = commitFull.Length > 7 ? commitFull.Substring(0, 7) : commitFull;
            var commitDateUtcIso = global::ThisAssembly.Git.CommitDate;
            var branch = global::ThisAssembly.Git.Branch;

            return new GitMeta(
                GitCommitShort: commitShort,
                GitCommitFull: commitFull,
                GitBranch: string.IsNullOrWhiteSpace(branch) ? "unknown" : branch,
                LastCommitAuthor: "unknown",
                LastCommitMessage: "unknown",
                LastCommitDateUtcIso: string.IsNullOrWhiteSpace(commitDateUtcIso) ? "unknown" : commitDateUtcIso,
                DiscoveryCurrentDir: "unknown",
                DiscoveryBaseDir: "unknown"
            );
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateUpwardDirectories(string startDirectory, int maxDepth)
    {
        var current = startDirectory;
        for (var i = 0; i < maxDepth && !string.IsNullOrWhiteSpace(current); i++)
        {
            yield return current;

            try
            {
                var parent = Directory.GetParent(current);
                if (parent is null)
                    yield break;

                current = parent.FullName;
            }
            catch
            {
                yield break;
            }
        }
    }

    private static string? RunGit(string workingDirectory, string arguments)
    {
        // Keep it fast to avoid blocking swagger page load.
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = new Process { StartInfo = psi };
        try
        {
            if (!proc.Start())
                return null;

            // Timeout: 2 seconds
            if (!proc.WaitForExit(2000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return null;
            }

            if (proc.ExitCode != 0)
                return null;

            return proc.StandardOutput.ReadToEnd().Trim();
        }
        catch
        {
            return null;
        }
    }

    private sealed record GitMeta(
        string GitCommitShort,
        string GitCommitFull,
        string GitBranch,
        string LastCommitAuthor,
        string LastCommitMessage,
        string LastCommitDateUtcIso,
        string DiscoveryCurrentDir,
        string DiscoveryBaseDir)
    {
        public string SwaggerDescription
        {
            get
            {
                var author = LastCommitAuthor ?? "unknown";
                var message = LastCommitMessage ?? "unknown";
                var dateUtc = LastCommitDateUtcIso ?? "unknown";
                string baseText;
                if (string.Equals(author, "unknown", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(message, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    baseText = $@"Current commit: {GitCommitShort}<br/>
                               Last commit date: {dateUtc}";
                }
                else
                {
                    baseText = $@"Current commit: {GitCommitShort}<br/>
                               Last commit: {author} - {message} ({dateUtc})";
                }

                // Develop_note:note 會在 Program.cs 透過 Configuration 取得並在這裡後綴呈現

                if (string.Equals(GitCommitShort, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    return baseText + "<br/>" +
                           "Git discovery: CurrentDir=" + (DiscoveryCurrentDir ?? "unknown") + "; BaseDir=" + (DiscoveryBaseDir ?? "unknown");
                }

                return baseText;
            }
        }

        public static GitMeta Unknown(string DiscoveryCurrentDir, string DiscoveryBaseDir) =>
            new(
                GitCommitShort: "unknown",
                GitCommitFull: "unknown",
                GitBranch: "unknown",
                LastCommitAuthor: "unknown",
                LastCommitMessage: "unknown",
                LastCommitDateUtcIso: "unknown",
                DiscoveryCurrentDir: DiscoveryCurrentDir,
                DiscoveryBaseDir: DiscoveryBaseDir
            );
    }
}

