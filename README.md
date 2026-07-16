# Fallout.GitHub & Fallout.WebDocu

[![Build Status](https://jenkins.dangl.me/buildStatus/icon?job=GeorgDangl%2FFallout.GitHub%2Fdevelop)](https://jenkins.dangl.me/job/GeorgDangl/job/Fallout.GitHub/job/develop/)

**Dangl.Fallout.GitHub**  
![NuGet](https://img.shields.io/nuget/v/Dangl.Fallout.GitHub.svg)
[![MyGet](https://img.shields.io/myget/dangl/v/Dangl.Fallout.GitHub.svg)]()  
**Dangl.Fallout.WebDocu**  
![NuGet](https://img.shields.io/nuget/v/Dangl.Fallout.WebDocu.svg)
[![MyGet](https://img.shields.io/myget/dangl/v/Dangl.Fallout.WebDocu.svg)]()

> This repository contains both **Fallout.GitHub** and **Fallout.WebDocu**.

# Fallout.GitHub

This plugin provides some methods to work with GitHub repositories
in [Fallout](https://github.com/Fallout-build/Fallout).

Currently supported:
  * **PublishRelease** to create GitHub releases.
  * **CreatePullRequest**
  * **GetReleases**
  * **GetRepository**

[Link to documentation](https://docs.dangl-it.com/Projects/Fallout.GitHub).

[Changelog](./Changelog_WebDocu.md)

## CI Builds

All builds are available on MyGet:

    https://www.myget.org/F/dangl/api/v2
    https://www.myget.org/F/dangl/api/v3/index.json

## Example

    using static Fallout.GitHub.GitHubTasks;
    using static Fallout.GitHub.ChangeLogExtensions;
    using static Fallout.Common.ChangeLog.ChangelogTasks;

    Target PublishGitHubRelease => _ => _
        .DependsOn(Pack)
        .Requires(() => GitHubAuthenticationToken)
        .OnlyWhen(() => GitVersion.BranchName.Equals("master") || GitVersion.BranchName.Equals("origin/master"))
        .Executes<Task>(async () =>
        {
            // You'll create a tag for the release, e.g. v1.0.0
            var releaseTag = $"v{GitVersion.MajorMinorPatch}";

            // We also want to fill the changelog with the latest release notes,
            // so we're just reading from a markdown file containing the changelog.
            // Not providing the second, optional parameter gives the latest section
            var changeLogSectionEntries = Fallout.Common.ChangeLog.ExtractChangelogSectionNotes(ChangeLogFile);
            var latestChangeLog = changeLogSectionEntries
                .Aggregate((c, n) => c + Environment.NewLine + n);
            var completeChangeLog = $"## {releaseTag}" + Environment.NewLine + latestChangeLog;

            var repositoryInfo = GetGitHubRepositoryInfo(GitRepository);

            await PublishRelease(x => x
                // We can optionally upload artifacts to the release, for example by finding
                // all nupkg files in the output directory
                .SetArtifactPaths(GlobFiles(OutputDirectory, "*.nupkg").NotEmpty().ToArray())
                .SetCommitSha(GitVersion.Sha)
                .SetReleaseNotes(completeChangeLog)
                .SetRepositoryName(repositoryInfo.repositoryName)
                .SetRepositoryOwner(repositoryInfo.gitHubOwner)
                .SetTag(releaseTag)
                .SetToken(GitHubAuthenticationToken)
            );
        });

# Fallout.WebDocu

This plugin provides a task to upload documentation packages to [WebDocu sites](https://github.com/GeorgDangl/WebDocu).
It's written for the [Fallout](https://github.com/Fallout-build/Fallout) system.

[Link to documentation](https://docs.dangl-it.com/Projects/Fallout.WebDocu).

[Changelog](./Changelog_WebDocu.md)

## CI Builds

All builds are available on MyGet:

    https://www.myget.org/F/dangl/api/v2
    https://www.myget.org/F/dangl/api/v3/index.json

## Example

When publishing to WebDocu, you have to include the version of the docs.

### Getting the Version from Generated NuGet Packages

```
Target UploadDocumentation => _ => _
    .DependsOn(BuildDocumentation)
    .Requires(() => DocuApiKey)
    .Requires(() => DocuBaseUrl)
    .Executes(() =>
    {
        WebDocuTasks.WebDocu(s =>
        {
            var packageVersion = GlobFiles(OutputDirectory, "*.nupkg").NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .Select(Path.GetFileName)
                .Select(x => WebDocuTasks.GetVersionFromNuGetPackageFilename(x, "Fallout.WebDeploy"))
                .First();

            return s.SetDocuBaseUrl(DocuBaseUrl)
                    .SetDocuApiKey(DocuApiKey)
                    .SetSourceDirectory(OutputDirectory / "docs")
                    .SetVersion(packageVersion);
        });
    });
```

### Getting the Version from GitVersion

```
Target UploadDocumentation => _ => _
    .DependsOn(Push) // To have a relation between pushed package version and published docs version
    .DependsOn(BuildDocumentation)
    .Requires(() => DocuApiKey)
    .Requires(() => DocuApiEndpoint)
    .Executes(() =>
    {
        WebDocuTasks.WebDocu(s => s.SetDocuApiEndpoint(DocuApiEndpoint)
            .SetDocuApiKey(DocuApiKey)
            .SetSourceDirectory(OutputDirectory / "docs")
            .SetVersion(GitVersion.NuGetVersion));
    });
```

The `DocuApiEndpoint` should look like this:

    https://docs.dangl-it.com/API/Projects/Upload

## License

[This project is available under the MIT license.](LICENSE.md)