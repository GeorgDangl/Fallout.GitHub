using Fallout.Common;
using Fallout.Common.Git;
using Fallout.Common.IO;
using Fallout.Common.ProjectModel;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.AzureKeyVault;
using Fallout.Common.Tools.DotNet;
using Fallout.Common.Tools.GitVersion;
using Fallout.Common.Tools.Teams;
using Fallout.Common.Utilities.Collections;
using Fallout.GitHub;
using Fallout.WebDocu;
using System;
using System.IO;
using System.Linq;
using static Fallout.CodeGeneration.CodeGenerator;
using static Fallout.Common.ChangeLog.ChangelogTasks;
using static Fallout.Common.IO.Globbing;
using static Fallout.Common.Tools.DotNet.DotNetTasks;
using static Fallout.GitHub.ChangeLogExtensions;
using static Fallout.GitHub.GitHubTasks;
using static Fallout.WebDocu.WebDocuTasks;

class Build : FalloutBuild
{
    // Console application entry. Also defines the default target.
    public static int Main() => Execute<Build>(x => x.Compile);

    [AzureKeyVaultConfiguration(
        BaseUrlParameterName = nameof(KeyVaultBaseUrl),
        ClientIdParameterName = nameof(KeyVaultClientId),
        ClientSecretParameterName = nameof(KeyVaultClientSecret),
        TenantIdParameterName = nameof(KeyVaultTenantId))]
    readonly AzureKeyVaultConfiguration KeyVaultSettings;

    [Parameter] string KeyVaultBaseUrl;
    [Parameter] string KeyVaultClientId;
    [Parameter] string KeyVaultClientSecret;
    [Parameter] string KeyVaultTenantId;
    [GitVersion] readonly GitVersion GitVersion;
    [GitRepository] readonly GitRepository GitRepository;

    [Parameter] readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [AzureKeyVaultSecret] string DocuBaseUrl;
    [AzureKeyVaultSecret] string GitHubAuthenticationToken;
    [AzureKeyVaultSecret] string DanglPublicFeedSource;
    [AzureKeyVaultSecret] string FeedzAccessToken;
    [AzureKeyVaultSecret("FalloutGitHub-DocuApiKey")] string FalloutGitHubDocuApiKey;
    [AzureKeyVaultSecret("FalloutWebDocu-DocuApiKey")] string FalloutWebDocuDocuApiKey;
    [AzureKeyVaultSecret] string NuGetApiKey;
    [AzureKeyVaultSecret] readonly string DanglCiCdTeamsWebhookUrl;

    [Solution("Fallout.GitHub.sln")] readonly Solution Solution;
    AbsolutePath SolutionDirectory => Solution.Directory;
    AbsolutePath OutputDirectory => SolutionDirectory / "output";
    AbsolutePath SourceDirectory => SolutionDirectory / "src";

    string FalloutGitHubDocFxFile => SolutionDirectory / "docs" / "GitHub" / "docfx_GitHub.json";
    string FalloutWebDocuDocFxFile => SolutionDirectory / "docs" / "WebDocu" / "docfx_WebDocu.json";

    string FalloutGitHubChangeLogFile => RootDirectory / "CHANGELOG_GitHub.md";
    string FalloutWebDocuChangeLogFile => RootDirectory / "CHANGELOG_WebDocu.md";

    protected override void OnTargetFailed(string target)
    {
        if (IsServerBuild)
        {
            SendTeamsMessage("Build Failed", $"Target {target} failed for Fallout.GitHub & Fallout.WebDocu, " +
                        $"Branch: {GitRepository.Branch}", true);
        }
    }

    private void SendTeamsMessage(string title, string message, bool isError)
    {
        if (!string.IsNullOrWhiteSpace(DanglCiCdTeamsWebhookUrl))
        {
            try
            {
                var themeColor = isError ? "f44336" : "00acc1";
                TeamsTasks
                    .SendTeamsMessage(m => m
                        .SetTitle(title)
                        .SetText(message)
                        .SetThemeColor(themeColor),
                        DanglCiCdTeamsWebhookUrl);
            }
            catch (Exception e)
            {
                Serilog.Log.Error("Failed to send Teams message: " + e);
            }
        }
    }

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(new DotNetRestoreSettings());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(x => x
                .SetNoBuild(true)
                .SetProjectFile(RootDirectory / "test" / "Fallout.WebDocu.Tests")
                .SetTestAdapterPath(".")
                .CombineWith(c => new[] { "net8.0" }
                    .Select(framework => c.SetFramework(framework).SetLoggers($"xunit;LogFilePath={OutputDirectory / $"tests-{framework}.xml"}"))
                ), degreeOfParallelism: Environment.ProcessorCount, completeOnFailure: true);
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var packData = new[]
            {
                new
                {
                    Changelog = FalloutGitHubChangeLogFile,
                    Title = "GitHub for Fallout - www.dangl-it.com",
                    Project = SourceDirectory / "Fallout.GitHub"
                },
                new
                {
                    Changelog = FalloutWebDocuChangeLogFile,
                    Title = "WebDocu for Fallout - www.dangl-it.com",
                    Project = SourceDirectory / "Fallout.WebDocu"
                }
            };

            foreach (var packInfo in packData)
            {
                var changeLog = GetCompleteChangeLog(packInfo.Changelog)
                    .EscapeStringPropertyForMsBuild();

                DotNetPack(x => x
                    .SetConfiguration(Configuration)
                    .SetProject(packInfo.Project)
                    .SetPackageReleaseNotes(changeLog)
                    .SetTitle(packInfo.Title)
                    .EnableNoBuild()
                    .SetOutputDirectory(OutputDirectory)
                    .SetVersion(GitVersion.NuGetVersion));
            }
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => Configuration == "Release")
        .OnlyWhenDynamic(() => IsOnBranch("master") || IsOnBranch("develop"))
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(DanglPublicFeedSource))
            {
                Assert.Fail(nameof(DanglPublicFeedSource) + " is required");
            }

            if (string.IsNullOrWhiteSpace(FeedzAccessToken))
            {
                Assert.Fail(nameof(FeedzAccessToken) + " is required");
            }

            var packages = GlobFiles(OutputDirectory, "*.nupkg")
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ToList();
            Assert.NotEmpty(packages);
            packages.ForEach(x =>
            {
                DotNetNuGetPush(s => s
                    .EnableSkipDuplicate()
                    .SetTargetPath(x)
                    .SetSource(DanglPublicFeedSource)
                    .SetApiKey(FeedzAccessToken));
            });

            if (GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
            {
                // Stable releases are published to NuGet
                packages.ForEach(x => DotNetNuGetPush(s => s
                    .SetTargetPath(x)
                    .SetSource("https://api.nuget.org/v3/index.json")
                    .SetApiKey(NuGetApiKey)));

                SendTeamsMessage("New Release", $"New release available for Fallout.GitHub & Fallout.WebDocu: {GitVersion.NuGetVersion}", false);
            }
        });

    Target BuildDocFxMetadata => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var docFxPath = NuGetToolPathResolver.GetPackageExecutable("docfx", "tools/net8.0/any/docfx.dll");
            DotNet($"{docFxPath} metadata {FalloutGitHubDocFxFile}");
            DotNet($"{docFxPath} metadata {FalloutWebDocuDocFxFile}");
        });

    Target BuildDocumentation => _ => _
        .DependsOn(Clean)
        .DependsOn(BuildDocFxMetadata)
        .Executes(() =>
        {
            var configs = new[]
            {
                FalloutGitHubDocFxFile,
                FalloutWebDocuDocFxFile
            };

            foreach (var config in configs)
            {
                var docsPath = (AbsolutePath)Path.GetDirectoryName(config);
                Serilog.Log.Information(docsPath);

                // Using README.md as index.md
                if (File.Exists(docsPath / "index.md"))
                {
                    File.Delete(docsPath / "index.md");
                }

                File.Copy(SolutionDirectory / "README.md", docsPath / "index.md", overwrite: true);
                File.Copy(SolutionDirectory / "LICENSE.md", docsPath / "LICENSE.md", overwrite: true);
                File.Copy(SolutionDirectory / "app-logo.png", docsPath / "app-logo.png", overwrite: true);
                File.Copy(FalloutGitHubChangeLogFile, docsPath / "CHANGELOG_GitHub.md", overwrite: true);
                File.Copy(FalloutWebDocuChangeLogFile, docsPath / "CHANGELOG_WebDocu.md", overwrite: true);

                var docFxPath = NuGetToolPathResolver.GetPackageExecutable("docfx", "tools/net8.0/any/docfx.dll");
                DotNet($"{docFxPath} {config}");

                File.Delete(docsPath / "LICENSE.md");
                File.Delete(docsPath / "index.md");
                File.Delete(docsPath / "app-logo.png");
                File.Delete(docsPath / "CHANGELOG_GitHub.md");
                File.Delete(docsPath / "CHANGELOG_WebDocu.md");
                Directory.Delete(docsPath / "api", true);
            }
        });

    Target UploadDocumentation => _ => _
        .DependsOn(Push) // To have a relation between pushed package version and published docs version
        .DependsOn(BuildDocumentation)
        .OnlyWhenDynamic(() => IsOnBranch("master") || IsOnBranch("develop"))
        .Executes(() =>
        {
            if (string.IsNullOrWhiteSpace(DocuBaseUrl))
            {
                Assert.Fail(nameof(DocuBaseUrl) + " is required");
            }

            if (string.IsNullOrWhiteSpace(FalloutGitHubDocuApiKey))
            {
                Assert.Fail(nameof(FalloutGitHubDocuApiKey) + " is required");
            }

            if (string.IsNullOrWhiteSpace(FalloutWebDocuDocuApiKey))
            {
                Assert.Fail(nameof(FalloutWebDocuDocuApiKey) + " is required");
            }

            WebDocu(s => s
                .SetSkipForVersionConflicts(true)
                .SetMarkdownChangelog(File.ReadAllText(FalloutGitHubChangeLogFile))
                .SetDocuBaseUrl(DocuBaseUrl)
                .SetDocuApiKey(FalloutGitHubDocuApiKey)
                .SetSourceDirectory(OutputDirectory / "docs_github")
                .SetVersion(GitVersion.NuGetVersion));

            WebDocu(s => s
                .SetSkipForVersionConflicts(true)
                .SetMarkdownChangelog(File.ReadAllText(FalloutWebDocuChangeLogFile))
                .SetDocuBaseUrl(DocuBaseUrl)
                .SetDocuApiKey(FalloutWebDocuDocuApiKey)
                .SetSourceDirectory(OutputDirectory / "docs_webdocu")
                .SetVersion(GitVersion.NuGetVersion));
        });

    Target PublishGitHubRelease => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsOnBranch("master"))
        .Executes(async () =>
        {
            if (string.IsNullOrWhiteSpace(GitHubAuthenticationToken))
            {
                Assert.Fail(nameof(GitHubAuthenticationToken) + " is required");
            }

            var releaseTag = $"v{GitVersion.MajorMinorPatch}";

            var gitHubChangeLogSectionEntries = ExtractChangelogSectionNotes(FalloutGitHubChangeLogFile)
                .Aggregate((c, n) => c + Environment.NewLine + n);
            var webDocuChangeLogSectionEntries = ExtractChangelogSectionNotes(FalloutWebDocuChangeLogFile)
                .Aggregate((c, n) => c + Environment.NewLine + n);
            var completeChangeLog = $"## {releaseTag}" + Environment.NewLine;
            completeChangeLog += "## Fallout.GitHub" + Environment.NewLine + gitHubChangeLogSectionEntries + Environment.NewLine;
            completeChangeLog += "## Fallout.WebDocu" + Environment.NewLine + webDocuChangeLogSectionEntries;

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);

            var packages = GlobFiles(OutputDirectory, "*.nupkg").ToArray();
            Assert.NotEmpty(packages);

            await PublishRelease(x => x
                    .SetArtifactPaths(packages)
                    .SetCommitSha(GitVersion.Sha)
                    .SetReleaseNotes(completeChangeLog)
                    .SetRepositoryName(repositoryInfo.repositoryName)
                    .SetRepositoryOwner(repositoryInfo.gitHubOwner)
                    .SetTag(releaseTag)
                    .SetToken(GitHubAuthenticationToken)
                );
        });

    Target Generate => _ => _
        .Executes(() =>
        {
            GenerateCodeFromDirectory(RootDirectory / "src" / "Fallout.GitHub" / "MetaData",
                outputFileProvider: x => RootDirectory / "src" / "Fallout.GitHub" / "GitHubTasks.Generated.cs",
                namespaceProvider: x => "Fallout.GitHub");
        });

    private bool IsOnBranch(string branchName)
    {
        return GitVersion.BranchName.Equals(branchName) || GitVersion.BranchName.Equals($"origin/{branchName}");
    }
}
