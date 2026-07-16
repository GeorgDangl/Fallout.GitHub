using System.Diagnostics.CodeAnalysis;

namespace Fallout.WebDocu
{
    [ExcludeFromCodeCoverage]
    public static class WebDocuSettingsExtensions
    {
        public static WebDocuSettings SetSourceDirectory(this WebDocuSettings toolSettings, string sourceDirectory)
        {
            toolSettings.SourceDirectory = sourceDirectory;
            return toolSettings;
        }

        public static WebDocuSettings SetDocuApiKey(this WebDocuSettings toolSettings, string docuApiKey)
        {
            toolSettings.DocuApiKey = docuApiKey;
            return toolSettings;
        }

        public static WebDocuSettings SetDocuBaseUrl(this WebDocuSettings toolSettings, string docuBaseUrl)
        {
            toolSettings.DocuBaseUrl = docuBaseUrl;
            return toolSettings;
        }

        public static WebDocuSettings SetVersion(this WebDocuSettings toolSettings, string version)
        {
            toolSettings.Version = version;
            return toolSettings;
        }

        public static WebDocuSettings SetMarkdownChangelog(this WebDocuSettings toolSettings, string markdownChangelog)
        {
            toolSettings.MarkdownChangelog = markdownChangelog;
            return toolSettings;
        }

        public static WebDocuSettings SetAssetFilePaths(this WebDocuSettings toolSettings, string[] assetFilePaths)
        {
            toolSettings.AssetFilePaths = assetFilePaths;
            return toolSettings;
        }

        public static WebDocuSettings SetSkipForVersionConflicts(this WebDocuSettings toolSettings, bool skipForVersionConflicts)
        {
            toolSettings.SkipForVersionConflicts = skipForVersionConflicts;
            return toolSettings;
        }
    }
}
