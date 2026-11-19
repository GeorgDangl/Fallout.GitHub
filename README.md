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

### Uploading a Markdown Package

On WebDocu servers supporting Markdown documentation, optionally upload a Markdown ZIP
in addition to the HTML documentation:

```csharp
WebDocuTasks.WebDocu(s => s
    .SetDocuBaseUrl(DocuBaseUrl)
    .SetDocuApiKey(DocuApiKey)
    .SetSourceDirectory(htmlDirectory)
    .SetVersion("1.2.3")
    .SetMarkdownPackagePath(markdownZipPath));
```

The HTML package is uploaded first, then the Markdown ZIP, then any assets. Without
`SetMarkdownPackagePath`, the existing HTML and asset upload behavior is unchanged.
`SetSkipForVersionConflicts(true)` skips Markdown and assets too when the HTML upload
returns HTTP 409. Markdown upload errors always fail the task and prevent asset uploads.

To add or replace Markdown for an existing HTML version without reuploading HTML or assets:

```csharp
WebDocuTasks.MarkdownPackageUpload(s => s
    .SetDocuBaseUrl(DocuBaseUrl)
    .SetDocuApiKey(DocuApiKey)
    .SetVersion("1.2.3")
    .SetMarkdownPackagePath(markdownZipPath));
```

Both forms require an explicit version and an existing ZIP file. They send `ApiKey`,
`Version`, and `MarkdownPackage` to `API/MarkdownDocuments/Upload` using multipart form data.
The standalone task can retry a failed Markdown upload after HTML was successfully published.
Reuploading replaces the version's entire Markdown corpus, without changing its HTML or assets.

The ZIP should contain UTF-8 `.md` or `.markdown` files with relative paths. Other files
are ignored by the server. Server limits are 10 MiB compressed, 20 MiB decompressed Markdown,
1 MiB per document, 1000 documents, and 2000 ZIP entries. Paths must be unique ignoring case,
at most 300 characters long, and contain no absolute paths or `.` / `..` segments.

## License

[This project is available under the MIT license.](LICENSE.md)

### Markdown documentation

`BuildDocumentation` also creates Markdown documentation in `output/docs-markdown-github`, `output/docs-markdown-webdocu`, with an adjacent ZIP archive. Each bundle starts at `index.md` and contains the same articles and resources selected for the corresponding HTML site, plus Markdown API reference files where applicable. The existing documentation upload publishes the Markdown archive to DanglDocu for the same version, making it available to AI agents through the DanglDocu MCP server.
