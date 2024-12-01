
using JetBrains.Annotations;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools;
using Nuke.Common.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Nuke.GitHub;

#region GitHubReleaseSettings
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public partial class GitHubReleaseSettings : GitHubSettings
{
    /// <summary>Optional file paths for files that should be appended to a release</summary>
    public string[] ArtifactPaths => Get<string[]>(() => ArtifactPaths);
    /// <summary>The message for the GitHub release</summary>
    public string ReleaseNotes => Get<string>(() => ReleaseNotes);
    /// <summary>The tag that should be used for the release, e.g. "v1.0.0"</summary>
    public string Tag => Get<string>(() => Tag);
    /// <summary>The name of the release. If ommited, the value of <see cref="Tag"/> is used</summary>
    public string Name => Get<string>(() => Name);
    /// <summary>The commit SHA on which to create the release</summary>
    public string CommitSha => Get<string>(() => CommitSha);
    /// <summary>Whether this is a pre-release</summary>
    public bool? Prerelease => Get<bool?>(() => Prerelease);
}
#endregion
#region GitHubPullRequestSettings
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public partial class GitHubPullRequestSettings : GitHubSettings
{
    /// <summary>The name of the branch you want the changes pulled into</summary>
    public string Base => Get<string>(() => Base);
    /// <summary>The name of the branch where your changes are implemented</summary>
    public string Head => Get<string>(() => Head);
    /// <summary>The title of the pull request</summary>
    public string Title => Get<string>(() => Title);
    /// <summary>The optional contents of the pull request</summary>
    public string Body => Get<string>(() => Body);
}
#endregion
#region GitHubSettings
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public partial class GitHubSettings : ToolOptions
{
    /// <summary>The account under which the repository is hosted</summary>
    public string RepositoryOwner => Get<string>(() => RepositoryOwner);
    /// <summary>The name of the repository</summary>
    public string RepositoryName => Get<string>(() => RepositoryName);
    /// <summary>The Token for the GitHub API</summary>
    public string Token => Get<string>(() => Token);
    /// <summary>The URL for GitHub Enterprise</summary>
    public string Url => Get<string>(() => Url);
}
#endregion
#region GitHubReleaseSettingsExtensions
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static partial class GitHubReleaseSettingsExtensions
{
    #region ArtifactPaths
    /// <inheritdoc cref="GitHubReleaseSettings.ArtifactPaths"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.ArtifactPaths))]
    public static T SetArtifactPaths<T>(this T o, string[] v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.ArtifactPaths, v));
    /// <inheritdoc cref="GitHubReleaseSettings.ArtifactPaths"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.ArtifactPaths))]
    public static T ResetArtifactPaths<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.ArtifactPaths));
    #endregion
    #region ReleaseNotes
    /// <inheritdoc cref="GitHubReleaseSettings.ReleaseNotes"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.ReleaseNotes))]
    public static T SetReleaseNotes<T>(this T o, string v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.ReleaseNotes, v));
    /// <inheritdoc cref="GitHubReleaseSettings.ReleaseNotes"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.ReleaseNotes))]
    public static T ResetReleaseNotes<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.ReleaseNotes));
    #endregion
    #region Tag
    /// <inheritdoc cref="GitHubReleaseSettings.Tag"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Tag))]
    public static T SetTag<T>(this T o, string v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Tag, v));
    /// <inheritdoc cref="GitHubReleaseSettings.Tag"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Tag))]
    public static T ResetTag<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.Tag));
    #endregion
    #region Name
    /// <inheritdoc cref="GitHubReleaseSettings.Name"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Name))]
    public static T SetName<T>(this T o, string v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Name, v));
    /// <inheritdoc cref="GitHubReleaseSettings.Name"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Name))]
    public static T ResetName<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.Name));
    #endregion
    #region CommitSha
    /// <inheritdoc cref="GitHubReleaseSettings.CommitSha"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.CommitSha))]
    public static T SetCommitSha<T>(this T o, string v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.CommitSha, v));
    /// <inheritdoc cref="GitHubReleaseSettings.CommitSha"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.CommitSha))]
    public static T ResetCommitSha<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.CommitSha));
    #endregion
    #region Prerelease
    /// <inheritdoc cref="GitHubReleaseSettings.Prerelease"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Prerelease))]
    public static T SetPrerelease<T>(this T o, bool? v) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Prerelease, v));
    /// <inheritdoc cref="GitHubReleaseSettings.Prerelease"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Prerelease))]
    public static T ResetPrerelease<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Remove(() => o.Prerelease));
    /// <inheritdoc cref="GitHubReleaseSettings.Prerelease"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Prerelease))]
    public static T EnablePrerelease<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Prerelease, true));
    /// <inheritdoc cref="GitHubReleaseSettings.Prerelease"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Prerelease))]
    public static T DisablePrerelease<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Prerelease, false));
    /// <inheritdoc cref="GitHubReleaseSettings.Prerelease"/>
    [Pure] [Builder(Type = typeof(GitHubReleaseSettings), Property = nameof(GitHubReleaseSettings.Prerelease))]
    public static T TogglePrerelease<T>(this T o) where T : GitHubReleaseSettings => o.Modify(b => b.Set(() => o.Prerelease, !o.Prerelease));
    #endregion
}
#endregion
#region GitHubPullRequestSettingsExtensions
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static partial class GitHubPullRequestSettingsExtensions
{
    #region Base
    /// <inheritdoc cref="GitHubPullRequestSettings.Base"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Base))]
    public static T SetBase<T>(this T o, string v) where T : GitHubPullRequestSettings => o.Modify(b => b.Set(() => o.Base, v));
    /// <inheritdoc cref="GitHubPullRequestSettings.Base"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Base))]
    public static T ResetBase<T>(this T o) where T : GitHubPullRequestSettings => o.Modify(b => b.Remove(() => o.Base));
    #endregion
    #region Head
    /// <inheritdoc cref="GitHubPullRequestSettings.Head"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Head))]
    public static T SetHead<T>(this T o, string v) where T : GitHubPullRequestSettings => o.Modify(b => b.Set(() => o.Head, v));
    /// <inheritdoc cref="GitHubPullRequestSettings.Head"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Head))]
    public static T ResetHead<T>(this T o) where T : GitHubPullRequestSettings => o.Modify(b => b.Remove(() => o.Head));
    #endregion
    #region Title
    /// <inheritdoc cref="GitHubPullRequestSettings.Title"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Title))]
    public static T SetTitle<T>(this T o, string v) where T : GitHubPullRequestSettings => o.Modify(b => b.Set(() => o.Title, v));
    /// <inheritdoc cref="GitHubPullRequestSettings.Title"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Title))]
    public static T ResetTitle<T>(this T o) where T : GitHubPullRequestSettings => o.Modify(b => b.Remove(() => o.Title));
    #endregion
    #region Body
    /// <inheritdoc cref="GitHubPullRequestSettings.Body"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Body))]
    public static T SetBody<T>(this T o, string v) where T : GitHubPullRequestSettings => o.Modify(b => b.Set(() => o.Body, v));
    /// <inheritdoc cref="GitHubPullRequestSettings.Body"/>
    [Pure] [Builder(Type = typeof(GitHubPullRequestSettings), Property = nameof(GitHubPullRequestSettings.Body))]
    public static T ResetBody<T>(this T o) where T : GitHubPullRequestSettings => o.Modify(b => b.Remove(() => o.Body));
    #endregion
}
#endregion
#region GitHubSettingsExtensions
/// <summary>Used within <see cref="GitHubTasks"/>.</summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public static partial class GitHubSettingsExtensions
{
    #region RepositoryOwner
    /// <inheritdoc cref="GitHubSettings.RepositoryOwner"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.RepositoryOwner))]
    public static T SetRepositoryOwner<T>(this T o, string v) where T : GitHubSettings => o.Modify(b => b.Set(() => o.RepositoryOwner, v));
    /// <inheritdoc cref="GitHubSettings.RepositoryOwner"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.RepositoryOwner))]
    public static T ResetRepositoryOwner<T>(this T o) where T : GitHubSettings => o.Modify(b => b.Remove(() => o.RepositoryOwner));
    #endregion
    #region RepositoryName
    /// <inheritdoc cref="GitHubSettings.RepositoryName"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.RepositoryName))]
    public static T SetRepositoryName<T>(this T o, string v) where T : GitHubSettings => o.Modify(b => b.Set(() => o.RepositoryName, v));
    /// <inheritdoc cref="GitHubSettings.RepositoryName"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.RepositoryName))]
    public static T ResetRepositoryName<T>(this T o) where T : GitHubSettings => o.Modify(b => b.Remove(() => o.RepositoryName));
    #endregion
    #region Token
    /// <inheritdoc cref="GitHubSettings.Token"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.Token))]
    public static T SetToken<T>(this T o, string v) where T : GitHubSettings => o.Modify(b => b.Set(() => o.Token, v));
    /// <inheritdoc cref="GitHubSettings.Token"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.Token))]
    public static T ResetToken<T>(this T o) where T : GitHubSettings => o.Modify(b => b.Remove(() => o.Token));
    #endregion
    #region Url
    /// <inheritdoc cref="GitHubSettings.Url"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.Url))]
    public static T SetUrl<T>(this T o, string v) where T : GitHubSettings => o.Modify(b => b.Set(() => o.Url, v));
    /// <inheritdoc cref="GitHubSettings.Url"/>
    [Pure] [Builder(Type = typeof(GitHubSettings), Property = nameof(GitHubSettings.Url))]
    public static T ResetUrl<T>(this T o) where T : GitHubSettings => o.Modify(b => b.Remove(() => o.Url));
    #endregion
}
#endregion
