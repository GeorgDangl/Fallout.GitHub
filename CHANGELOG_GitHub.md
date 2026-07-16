# Changelog

All notable changes to **Fallout.GitHub** are documented here.

## v8.0.0:
- Migrate to Fallout

# v7.0.0:
- Update NUKE to v9
- The minimum required .NET version is now 8.0

## v6.0.0:
- Update NUKE to v8

## v5.0.1:
- Update logging to indicate if publishing a release is skipped due to it already existing

## v5.0.0:
- Update NUKE to v7
- Version alignment with **Fallout.WebDocu**

## v3.0.0:
- Update NUKE to v6.0.1

## v2.0.0:
- Update NUKE to v5.0.0

## v1.6.2:
- Update internal dependencies and regenerate code

## v1.6.1:
- Add support for custom urls to support GitHub Enterprise

## v1.6.0:
- Update NUKE to 0.24.1

## v1.5.0:
- Update NUKE to 0.19.0

## v1.4.0:
- Update NUKE to 0.18.0

## v1.3.5:
- Add handling to escape quotation mark `"` to `ChangeLogExtensions`

## v1.3.3:
- Return from `CreatePullRequest` if the specified one already exists instead of waiting for the Octokit API to fail

## v1.3.2:
- Add optional configuration to specify the name of the GitHub release

## v1.3.1:
- Invoke `AssertValid()` on settings before using them to fail early in case of invalid settings
- `PublishRelease` will output some status messages to the `Logger`

## v1.3.0:
- Add CreatePullRequest, GetReleases and GetRepository tasks
- Make `PublishRelease` invocation consistent with other NUKE tools

## v1.2.1:
- Add option to mark releases as `Prerelease`

## v1.2.0:
- Update to NUKE 0.6.0

## v1.1.0:
- Update to NUKE 0.4.0

## v1.0.2
- Use the official NUKE logo for the package

## v1.0.1
- Refactor code to follow NUKE tooling recommendations
- Provide `Fallout.GitHub.ChangeLogExtensions`. This will likely be merged with NUKE itself
  or be distributed in a separate package in the future

## v1.0.0
- Initial Release
- Supports `PublishRelease`

