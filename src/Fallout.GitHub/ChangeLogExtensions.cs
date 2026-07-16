using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fallout.GitHub
{
    public static class ChangeLogExtensions
    {
        public static string GetCompleteChangeLog(string changeLogFile)
        {
            var fileContent = File.ReadAllText(changeLogFile);
            var lines = Regex.Split(fileContent, "\r\n?|\n");
            var changeLogLines = lines
                .SkipWhile(l => !l.StartsWith("##"));
            var stringBuilder = new StringBuilder();
            foreach (var changeLogLine in changeLogLines)
            {
                stringBuilder.AppendLine(changeLogLine);
            }

            var releaseNotes = stringBuilder.ToString();
            return releaseNotes;
        }

        public static string EscapeStringPropertyForMsBuild(this string source)
        {
            // TODO Check MSBuild docs -> Is this just a regular Url escaping?
            return source
                .Replace(";", "%3B")
                .Replace(",", "%2C")
                .Replace(" ", "%20")
                .Replace("\r", "%0D")
                .Replace("\n", "%0A")
                .Replace("\"", "%22");
        }
    }
}