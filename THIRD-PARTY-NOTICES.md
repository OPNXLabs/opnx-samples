# Third-Party Notices

This repository contains sample applications that use separately licensed OPNX and third-party components.

The OPNX Samples license applies only to OPNX-owned sample code and documentation. Dependencies, native runtimes, and source-derived components remain subject to their own licenses and notices.

## OPNX Packages

### OPNX.Lib

- Version: `0.1.0-preview.20260716.1`
- Repository: [OPNXLabs/opnx-lib](https://github.com/OPNXLabs/opnx-lib)
- License: OPNX source-available license
- Used by: all sample projects

### OPNX.UI.WPF

- Version: `0.1.0-preview.20260716.1`
- Repository: [OPNXLabs/opnx-ui](https://github.com/OPNXLabs/opnx-ui)
- License: OPNX source-available license
- Used by: `OPNX.Samples.RtspMultiLiveViewer`, `OPNX.Samples.PlaybackTimeline`

Users must review the license and third-party notices distributed with each OPNX package. Publishing these samples does not expand the rights granted by those packages.

## Version Scope

The versions above match `Directory.Build.props` and the sample project references. Current Lib/UI repository sources have newer APIs, FFmpeg bindings, and WebRTC dependencies that are not automatically included in these pinned packages. Updating samples requires matching API migrations and native-runtime preparation, not just replacing the version strings.

If upgraded to the current Lib SDK, preserve ASP.NET Core notices and distinguish the MIT DataChannelDotnet wrapper from native libdatachannel (MPL-2.0) and OpenSSL 3 (Apache-2.0). The notices delivered by the exact selected packages remain authoritative.

## FFmpeg Native Libraries

Some samples can interoperate with FFmpeg native shared libraries through OPNX.Lib and FFmpeg.AutoGen.

- Project: [https://ffmpeg.org/](https://ffmpeg.org/)
- License: depends on the selected build and enabled components

FFmpeg native binaries are not included in this repository. Anyone obtaining, bundling, redistributing, or deploying FFmpeg binaries is responsible for complying with the license terms that apply to the selected build. FFmpeg is generally available under `LGPL-2.1-or-later`, while some configurations may cause GPL or nonfree terms to apply.

References:

- [FFmpeg legal information](https://ffmpeg.org/legal.html)
- [FFmpeg downloads](https://ffmpeg.org/download.html)

## Transitive Dependencies

OPNX.Lib and OPNX.UI.WPF bring additional NuGet and native dependencies. Their package contents and third-party notices are the authoritative source for the exact versions and applicable license texts.
