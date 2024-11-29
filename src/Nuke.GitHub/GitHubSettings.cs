using System;
using Nuke.Common.Tooling;

namespace Nuke.GitHub
{
    public partial class GitHubSettings
    {
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                throw new ArgumentException("A GitHub token is required for authentication in the GitHub API.");
            }
        }
    }
}
