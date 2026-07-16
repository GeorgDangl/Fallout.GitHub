using Fallout.Common.Tooling;
using Xunit;

namespace Fallout.WebDocu.Tests
{
    public class WebDocuTasksTests
    {
        [Fact]
        public void ExtractVersion()
        {
            var packagePath = @"C:\\Documents\Project.1.2.3-beta4.nupkg";
            var version = WebDocuTasks.GetVersionFromNuGetPackageFilename(packagePath, "Project");
            Assert.Equal("1.2.3-beta4", version);
        }

        [Fact]
        public void CanBuildSettings()
        {
            var settings = new WebDocuSettings();

            var wow1 = settings.Modify(o => o.Set(() => settings.DocuBaseUrl, "https://dangl.com"));

            settings = settings.SetDocuBaseUrl("https://dangl.com");
            Assert.Equal("https://dangl.com", settings.DocuBaseUrl);
        }
    }
}
