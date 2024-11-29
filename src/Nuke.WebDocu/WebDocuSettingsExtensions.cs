using JetBrains.Annotations;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nuke.WebDocu
{
    [PublicAPI]
    [ExcludeFromCodeCoverage]
    public static class WebDocuSettingsExtensions
    {
        [Pure]
        public static WebDocuSettings SetSourceDirectory(this WebDocuSettings toolSettings, string sourceDirectory)
            => toolSettings.Modify(o => o.Set(() => toolSettings.SourceDirectory, sourceDirectory));

        [Pure]
        public static WebDocuSettings SetDocuApiKey(this WebDocuSettings toolSettings, string docuApiKey)
            => toolSettings.Modify(o => o.Set(() => toolSettings.DocuApiKey, docuApiKey));

        [Pure]
        public static WebDocuSettings SetDocuBaseUrl(this WebDocuSettings toolSettings, string docuBaseUrl)
            => toolSettings.Modify(o => o.Set(() => toolSettings.DocuBaseUrl, docuBaseUrl));

        [Pure]
        public static WebDocuSettings SetVersion(this WebDocuSettings toolSettings, string version)
            => toolSettings.Modify(o => o.Set(() => toolSettings.Version, version));

        [Pure]
        public static WebDocuSettings SetMarkdownChangelog(this WebDocuSettings toolSettings, string markdownChangelog)
            => toolSettings.Modify(o => o.Set(() => toolSettings.MarkdownChangelog, markdownChangelog));

        [Pure]
        public static WebDocuSettings SetAssetFilePaths(this WebDocuSettings toolSettings, string[] assetFilePaths)
            => toolSettings.Modify(o => o.Set(() => toolSettings.AssetFilePaths, assetFilePaths));

        [Pure]
        public static WebDocuSettings SetSkipForVersionConflicts(this WebDocuSettings toolSettings, bool skipForVersionConflicts)
            => toolSettings.Modify(o => o.Set(() => toolSettings.SkipForVersionConflicts, skipForVersionConflicts));
    }
}
