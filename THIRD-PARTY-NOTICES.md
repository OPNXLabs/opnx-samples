# Third-Party Notices

This repository contains sample applications that use separately licensed OPNX and third-party components.

The OPNX Samples license applies only to OPNX-owned sample code and documentation. Dependencies, native runtimes, and source-derived components remain subject to their own licenses and notices.

## OPNX Packages

### OPNX.Lib

- Version: `0.1.0-preview.20261106.3`
- Repository: [OPNXLabs/opnx-lib](https://github.com/OPNXLabs/opnx-lib)
- License: OPNX source-available license
- Used by: all sample projects

### OPNX.UI.WPF

- Version: `0.1.0-preview.20261107.1`
- Repository: [OPNXLabs/opnx-ui](https://github.com/OPNXLabs/opnx-ui)
- License: OPNX source-available license
- Used by: `OPNX.Samples.RtspViewer`, `OPNX.Samples.PlaybackTimeline`, `OPNX.Samples.ControlGallery`, `OPNX.Samples.SystemMonitoring`, `OPNX.Samples.WebRtcViewer`

Users must review the license and third-party notices distributed with each OPNX package. Publishing these samples does not expand the rights granted by those packages.

## Version Scope

The versions above match `Directory.Build.props` and the sample project references. These packages use FFmpeg.AutoGen 9.0.1.1 and include WebRTC dependencies through the aggregate Lib package. Match the native runtimes to the exact restored dependency graph.

Preserve ASP.NET Core notices and distinguish the MIT DataChannelDotnet wrapper from native libdatachannel (MPL-2.0) and OpenSSL 3 (Apache-2.0). The notices delivered by the exact selected packages remain authoritative.

## FFmpeg Native Libraries

Some samples can interoperate with FFmpeg native shared libraries through OPNX.Lib and FFmpeg.AutoGen.

- Project: [https://ffmpeg.org/](https://ffmpeg.org/)
- License: depends on the selected build and enabled components

RtspViewer restores the pinned Windows x64 LGPL v3 shared build `n9.0.2-39-g7adfbe40dc-20261031` directly from the upstream publisher at local build time. FFmpeg DLLs/archives are not included in the current repository tree; they are cached in ignored local folders. The download URL, archive checksum and per-DLL checksums are recorded in [src/OPNX.Samples.RtspViewer/Build/ffmpeg-runtime.json](https://github.com/OPNXLabs/opnx-samples/blob/master/src/OPNX.Samples.RtspViewer/Build/ffmpeg-runtime.json). See [runtime information](https://github.com/OPNXLabs/opnx-samples/blob/master/src/OPNX.Samples.RtspViewer/FFMpeg/Lib/x64/README.txt), `BUILD-INFO.txt`, `LICENSE.FFmpeg.txt` and `COPYING.GPLv3.txt` for configuration and license texts. Other samples do not restore this runtime.

FFmpeg is used under its own license, independently of OPNX-owned code. The current dynamically linked, replaceable LGPL build does not by itself require release of OPNX application source. LGPL library modification/replacement and reverse engineering to debug such modifications are permitted as required by the applicable license; OPNX's restrictions do not override those rights.

A local build/publish output includes downloaded third-party binaries. Anyone redistributing that output must provide all applicable notices and the required corresponding-source/build-material access for the actual FFmpeg build and its covered dependencies. The scripts and FFmpeg source URL alone are not a complete corresponding-source bundle. Other FFmpeg builds can have different GPL/nonfree conditions. See [restoration and redistribution guidance](docs/ffmpeg-runtime.md) for offline builds, replacement libraries and the provenance of the previous runtime.

References:

- [FFmpeg legal information](https://ffmpeg.org/legal.html)
- [FFmpeg downloads](https://ffmpeg.org/download.html)

## Transitive Dependencies

OPNX.Lib and OPNX.UI.WPF bring additional NuGet and native dependencies. Their package contents and third-party notices are the authoritative source for the exact versions and applicable license texts.

## Microsoft WebView2

`OPNX.Samples.WebRtcViewer` additionally uses Microsoft.Web.WebView2 1.0.4258.31 under the [Microsoft package license](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4258.31/License), included as `LICENSE.txt` in that NuGet package. The Microsoft Edge WebView2 Runtime is installed and licensed separately. WebRTC native dependencies (DataChannelDotnet/libdatachannel) are provided through OPNX.Lib; see its bundled third-party notices. This sample does not bundle FFmpeg or copy OPNX.V application code.
