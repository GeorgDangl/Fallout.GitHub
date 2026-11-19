using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fallout.WebDocu.Tests
{
    public class MarkdownPackageUploadTests
    {
        [Fact]
        public void UploadsMarkdownAfterHtmlAndBeforeAssets()
        {
            using var server = new UploadServer();
            using var files = new DocumentationFiles();

            WebDocuTasks.WebDocu(s => Configure(s, server, files)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)
                .SetAssetFilePaths(new[] { files.AssetPath }));

            var requests = server.Requests.ToArray();
            Assert.Equal(new[] { "/API/Projects/Upload", "/API/MarkdownDocuments/Upload",
                "/API/ProjectAssets/SASUpload", "/API/ProjectAssets/Upload" }, requests.Select(r => r.Path));
            AssertMarkdownRequest(requests[1], files);
        }

        [Fact]
        public void HtmlOnlyUploadDoesNotUploadMarkdown()
        {
            using var server = new UploadServer();
            using var files = new DocumentationFiles();

            WebDocuTasks.WebDocu(s => Configure(s, server, files));

            Assert.Equal("/API/Projects/Upload", Assert.Single(server.Requests).Path);
        }

        [Fact]
        public void StandaloneUploadOnlyUploadsMarkdown()
        {
            using var server = new UploadServer();
            using var files = new DocumentationFiles();

            WebDocuTasks.MarkdownPackageUpload(s => Configure(s, server, files)
                .SetSourceDirectory(null)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)
                .SetAssetFilePaths(new[] { files.AssetPath }));

            AssertMarkdownRequest(Assert.Single(server.Requests), files);
        }

        [Fact]
        public void SkippedHtmlConflictSkipsMarkdownAndAssets()
        {
            using var server = new UploadServer(htmlStatus: HttpStatusCode.Conflict);
            using var files = new DocumentationFiles();

            WebDocuTasks.WebDocu(s => Configure(s, server, files)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)
                .SetAssetFilePaths(new[] { files.AssetPath })
                .SetSkipForVersionConflicts(true));

            Assert.Equal("/API/Projects/Upload", Assert.Single(server.Requests).Path);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Conflict)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.RequestEntityTooLarge)]
        public void MarkdownFailureIsReportedAndSkipsAssets(HttpStatusCode status)
        {
            using var server = new UploadServer(markdownStatus: status);
            using var files = new DocumentationFiles();

            var exception = Assert.Throws<Exception>(() => WebDocuTasks.WebDocu(s => Configure(s, server, files)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)
                .SetAssetFilePaths(new[] { files.AssetPath })
                .SetSkipForVersionConflicts(true)));

            Assert.Contains("Markdown upload failed with status code: " + status, exception.Message);
            Assert.Contains("upload rejected", exception.Message);
            Assert.Equal(new[] { "/API/Projects/Upload", "/API/MarkdownDocuments/Upload" },
                server.Requests.Select(r => r.Path));
        }

        [Fact]
        public void HtmlFailurePreventsMarkdownUpload()
        {
            using var server = new UploadServer(htmlStatus: HttpStatusCode.Conflict);
            using var files = new DocumentationFiles();

            Assert.Throws<Exception>(() => WebDocuTasks.WebDocu(s => Configure(s, server, files)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)));

            Assert.Equal("/API/Projects/Upload", Assert.Single(server.Requests).Path);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MarkdownRequiresExplicitVersionBeforeAnyUpload(bool standalone)
        {
            using var server = new UploadServer();
            using var files = new DocumentationFiles();
            var settings = Configure(new WebDocuSettings(), server, files)
                .SetMarkdownPackagePath(files.MarkdownPackagePath)
                .SetVersion(" ");

            var exception = Assert.Throws<ArgumentException>(() =>
            {
                if (standalone)
                {
                    WebDocuTasks.MarkdownPackageUpload(s => settings);
                }
                else
                {
                    WebDocuTasks.WebDocu(s => settings);
                }
            });

            Assert.Equal("Version", exception.ParamName);
            Assert.Empty(server.Requests);
        }

        [Fact]
        public void StandaloneRequiresPackagePath()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                WebDocuTasks.MarkdownPackageUpload(s => s.SetVersion("1.2.3")));

            Assert.Equal("MarkdownPackagePath", exception.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingMarkdownPackageFailsBeforeAnyUpload(bool standalone)
        {
            using var server = new UploadServer();
            using var files = new DocumentationFiles();
            var settings = Configure(new WebDocuSettings(), server, files)
                .SetMarkdownPackagePath(Path.Combine(files.SourceDirectory, "missing.zip"));

            Assert.Throws<FileNotFoundException>(() =>
            {
                if (standalone)
                {
                    WebDocuTasks.MarkdownPackageUpload(s => settings);
                }
                else
                {
                    WebDocuTasks.WebDocu(s => settings);
                }
            });

            Assert.Empty(server.Requests);
        }

        static WebDocuSettings Configure(WebDocuSettings settings, UploadServer server, DocumentationFiles files)
        {
            return settings.SetDocuBaseUrl(server.BaseUrl)
                .SetDocuApiKey("test-upload-key")
                .SetVersion("1.2.3-beta4")
                .SetSourceDirectory(files.SourceDirectory);
        }

        static void AssertMarkdownRequest(UploadRequest request, DocumentationFiles files)
        {
            Assert.Equal("POST", request.Method);
            Assert.Equal("/API/MarkdownDocuments/Upload", request.Path);
            Assert.StartsWith("multipart/form-data; boundary=", request.ContentType);
            Assert.Contains("name=ApiKey", request.Body.Replace("\"", string.Empty));
            Assert.Contains("test-upload-key", request.Body);
            Assert.Contains("name=Version", request.Body.Replace("\"", string.Empty));
            Assert.Contains("1.2.3-beta4", request.Body);
            Assert.Contains("name=MarkdownPackage", request.Body.Replace("\"", string.Empty));
            Assert.Contains("filename=markdown.zip", request.Body.Replace("\"", string.Empty));
            Assert.Contains(Encoding.Latin1.GetString(File.ReadAllBytes(files.MarkdownPackagePath)), request.Body);
        }

        sealed class DocumentationFiles : IDisposable
        {
            readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            public string SourceDirectory { get; }
            public string MarkdownPackagePath { get; }
            public string AssetPath { get; }

            public DocumentationFiles()
            {
                SourceDirectory = Path.Combine(_directory, "html");
                Directory.CreateDirectory(SourceDirectory);
                File.WriteAllText(Path.Combine(SourceDirectory, "index.html"), "<h1>Documentation</h1>");
                MarkdownPackagePath = Path.Combine(_directory, "markdown.zip");
                using (var zip = ZipFile.Open(MarkdownPackagePath, ZipArchiveMode.Create))
                using (var writer = new StreamWriter(zip.CreateEntry("guide/index.md").Open()))
                {
                    writer.Write("# Documentation");
                }
                AssetPath = Path.Combine(_directory, "asset.txt");
                File.WriteAllText(AssetPath, "asset");
            }

            public void Dispose() => Directory.Delete(_directory, true);
        }

        sealed record UploadRequest(string Path, string Method, string ContentType, string Body);

        sealed class UploadServer : IDisposable
        {
            readonly HttpListener _listener = new HttpListener();
            readonly Task _requestsTask;
            readonly HttpStatusCode _htmlStatus;
            readonly HttpStatusCode _markdownStatus;
            public string BaseUrl { get; }
            public ConcurrentQueue<UploadRequest> Requests { get; } = new ConcurrentQueue<UploadRequest>();

            public UploadServer(HttpStatusCode htmlStatus = HttpStatusCode.OK, HttpStatusCode markdownStatus = HttpStatusCode.OK)
            {
                _htmlStatus = htmlStatus;
                _markdownStatus = markdownStatus;
                var portListener = new TcpListener(IPAddress.Loopback, 0);
                portListener.Start();
                var port = ((IPEndPoint)portListener.LocalEndpoint).Port;
                portListener.Stop();
                BaseUrl = $"http://127.0.0.1:{port}/";
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();
                _requestsTask = Task.Run(HandleRequests);
            }

            async Task HandleRequests()
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        var context = await _listener.GetContextAsync();
                        using var body = new MemoryStream();
                        await context.Request.InputStream.CopyToAsync(body);
                        var path = context.Request.Url.AbsolutePath;
                        Requests.Enqueue(new UploadRequest(path, context.Request.HttpMethod, context.Request.ContentType,
                            Encoding.Latin1.GetString(body.ToArray())));
                        var status = path == "/API/Projects/Upload" ? _htmlStatus
                            : path == "/API/MarkdownDocuments/Upload" ? _markdownStatus
                            : path == "/API/ProjectAssets/SASUpload" ? HttpStatusCode.NotFound
                            : HttpStatusCode.OK;
                        context.Response.StatusCode = (int)status;
                        var response = Encoding.UTF8.GetBytes((int)status >= 400 ? "upload rejected" : "{}");
                        context.Response.ContentLength64 = response.Length;
                        await context.Response.OutputStream.WriteAsync(response);
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException) when (!_listener.IsListening)
                {
                }
                catch (ObjectDisposedException) when (!_listener.IsListening)
                {
                }
            }

            public void Dispose()
            {
                _listener.Close();
                _requestsTask.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
        }
    }
}
