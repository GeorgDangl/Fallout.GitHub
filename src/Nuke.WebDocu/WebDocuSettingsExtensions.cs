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
        {
            toolSettings.SourceDirectory = sourceDirectory;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetDocuApiKey(this WebDocuSettings toolSettings, string docuApiKey)
        {
            toolSettings.DocuApiKey = docuApiKey;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetDocuBaseUrl(this WebDocuSettings toolSettings, string docuBaseUrl)
        {
            toolSettings.DocuBaseUrl = docuBaseUrl;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetVersion(this WebDocuSettings toolSettings, string version)
        {
            toolSettings.Version = version;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetMarkdownChangelog(this WebDocuSettings toolSettings, string markdownChangelog)
        {
            toolSettings.MarkdownChangelog = markdownChangelog;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetAssetFilePaths(this WebDocuSettings toolSettings, string[] assetFilePaths)
        {
            toolSettings.AssetFilePaths = assetFilePaths;
            return toolSettings;
        }

        [Pure]
        public static WebDocuSettings SetSkipForVersionConflicts(this WebDocuSettings toolSettings, bool skipForVersionConflicts)
        {
            toolSettings.SkipForVersionConflicts = skipForVersionConflicts;
            return toolSettings;
        }
    }
}
