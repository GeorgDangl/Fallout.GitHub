using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Fallout.Common.CI.Jenkins;
using Fallout.Common.Tooling;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Fallout.Common.IO.Globbing;

namespace Fallout.WebDocu
{
    public static class WebDocuTasks
    {
        public static void WebDocu(Configure<WebDocuSettings> configurator)
        {
            var settings = configurator.InvokeSafe(new WebDocuSettings());

            // Create zip package
            var tempPath = Path.GetTempFileName();
            File.Delete(tempPath);
            FixGitUrlsIfInJenkinsJob(settings.SourceDirectory);
            ZipFile.CreateFromDirectory(settings.SourceDirectory, tempPath);

            // Upload package to docs
            UploadToDanglDocu(tempPath, settings)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            File.Delete(tempPath);
        }

        public static string GetVersionFromNuGetPackageFilename(string nuGetPackagePath, string projectName)
        {
            return Path.GetFileName(nuGetPackagePath)
                .Replace(".nupkg", string.Empty)
                .Replace(projectName + ".", string.Empty);
        }

        public static void AssetFileUpload(Configure<WebDocuSettings> configurator)
        {
            var settings = configurator.InvokeSafe(new WebDocuSettings());

            foreach (var assetFile in settings.AssetFilePaths)
            {
                UploadAssetFile(assetFile, settings)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
            }
        }

        static async Task UploadToDanglDocu(string zipPackage, WebDocuSettings settings)
        {
            using (var docsStream = File.OpenRead(zipPackage))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, settings.DocuBaseUrl.TrimEnd('/') + "/API/Projects/Upload");
                var requestContent = new MultipartFormDataContent();
                requestContent.Add(new StringContent(settings.DocuApiKey), "ApiKey");
                requestContent.Add(new StringContent(settings.Version), "Version");
                if (!string.IsNullOrWhiteSpace(settings.MarkdownChangelog))
                {
                    requestContent.Add(new StringContent(settings.MarkdownChangelog), "MarkdownChangelog");
                }

                requestContent.Add(new StreamContent(docsStream), "ProjectPackage", "docs.zip");
                request.Content = requestContent;
                var response = await new HttpClient().SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict
                        && settings.SkipForVersionConflicts)
                    {
                        Serilog.Log.Debug($"WebDocu return Http status 409 - Conflict. This means that there is likely already an existing" +
                            $" combination of version and project present. The settings are enabled to skip this." +
                            $" Asserts will also not be uploaded.");
                        return;
                    }

                    throw new Exception("Upload failed with status code: " + response.StatusCode + Environment.NewLine + await response.Content.ReadAsStringAsync());
                }
            }

            if (settings.AssetFilePaths?.Any() == true)
            {
                foreach (var assetFilePath in settings.AssetFilePaths)
                {
                    await UploadAssetFile(assetFilePath, settings);
                }
            }
        }

        static async Task UploadAssetFile(string assetFilePath, WebDocuSettings settings)
        {
            Serilog.Log.Debug($"Uploading asset {assetFilePath}");

            if (!await UploadAssetFileViaSas(assetFilePath, settings))
            {
                await UploadAssetFileDirectly(assetFilePath, settings);
            }
            else
            {
                Serilog.Log.Debug("File was uploaded via direct SAS upload to Azure Blob Storage");
            }
        }

        static async Task<bool> UploadAssetFileViaSas(string assetFilePath, WebDocuSettings settings)
        {
            var httpClient = new HttpClient();
            using (var assetStream = File.OpenRead(assetFilePath))
            {
                var fileName = NormalizeFilename(assetFilePath);
                var sasRequest = new HttpRequestMessage(HttpMethod.Post, settings.DocuBaseUrl.TrimEnd('/')
                    + $"/API/ProjectAssets/SASUpload?apiKey={HttpUtility.UrlEncode(settings.DocuApiKey)}&version={HttpUtility.UrlEncode(settings.Version)}");
                var requestContent = new StringContent(JsonConvert.SerializeObject(new
                {
                    FileName = fileName,
                    FileSizeInBytes = assetStream.Length
                }), Encoding.UTF8, "application/json");
                sasRequest.Content = requestContent;
                var sasResponse = await httpClient.SendAsync(sasRequest);
                if (!sasResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                var sasResponseJson = JObject.Parse(await sasResponse.Content.ReadAsStringAsync());
                var sasLink = sasResponseJson["UploadLink"]?.ToString() ?? sasResponseJson["uploadLink"]?.ToString();
                if (string.IsNullOrWhiteSpace(sasLink))
                {
                    return false;
                }

                var sasBlobClient = new BlobClient(new Uri(sasLink));
                var uploadResponse = await sasBlobClient.UploadAsync(assetStream);
                return uploadResponse.GetRawResponse().Status >= 200 && uploadResponse.GetRawResponse().Status <= 299;
            }
        }

        static async Task UploadAssetFileDirectly(string assetFilePath, WebDocuSettings settings)
        {
            using (var assetStream = File.OpenRead(assetFilePath))
            {
                var fileName = NormalizeFilename(assetFilePath);
                var request = new HttpRequestMessage(HttpMethod.Post, settings.DocuBaseUrl.TrimEnd('/') + "/API/ProjectAssets/Upload");
                var requestContent = new MultipartFormDataContent();
                requestContent.Add(new StringContent(settings.DocuApiKey), "ApiKey");
                requestContent.Add(new StringContent(settings.Version), "Version");
                requestContent.Add(new StreamContent(assetStream), "AssetFile", fileName);
                request.Content = requestContent;
                var response = await new HttpClient().SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Upload failed with status code: " + response.StatusCode + Environment.NewLine + await response.Content.ReadAsStringAsync());
                }
            }
        }

        static string NormalizeFilename(string originalFilename)
        {
            if (string.IsNullOrWhiteSpace(originalFilename))
            {
                return string.Empty;
            }

            var filename = Path.GetFileName(originalFilename).Trim();
            return filename;
        }

        static void FixGitUrlsIfInJenkinsJob(string sourceDirectory)
        {
            var jenkinsInstance = Jenkins.Instance as Jenkins;
            if (jenkinsInstance == null)
            {
                Serilog.Log.Write(Serilog.Events.LogEventLevel.Information, "Not inside a Jenkins job, \"View Source\" links will not be changed");
                return;
            }
            Serilog.Log.Write(Serilog.Events.LogEventLevel.Information, "Inside a Jenkins job, \"View Source\" links will be changed to point to the commit hash");

            // In Jenkins, the Git branch is something like "origin/dev", which should
            // only be "dev" to generate correct urls.
            // It's just replaced by the actual commit hash as to preserve the version context

            foreach (var htmlFile in GlobFiles(sourceDirectory, "**/*.html"))
            {
                var originalContent = File.ReadAllText(htmlFile);
                var correctedText = originalContent
                    .Replace($"blob/{jenkinsInstance.GitBranch}", $"blob/{jenkinsInstance.GitCommit}")
                    .Replace($"blob/heads/{jenkinsInstance.GitBranch}", $"blob/{jenkinsInstance.GitCommit}");
                File.WriteAllText(htmlFile, correctedText);
            }
        }
    }
}
