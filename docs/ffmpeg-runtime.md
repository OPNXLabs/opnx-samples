# FFmpeg runtime restoration and licensing

## Run the sample

The restore script, build target and version manifest are in the RtspViewer project's `Build` folder and can be opened from Visual Studio Solution Explorer.

Build RtspViewer normally. The first build downloads the pinned Windows x64 LGPL shared ZIP directly from the upstream binary publisher, verifies its SHA-256, extracts only seven named DLLs, verifies those DLLs, and copies them to `FFMpeg/Lib/x64` in the build output. Later builds reuse the local cache without a network request or a full DLL hash scan. `dotnet publish` copies the same runtime and notices into its local output.

The repository distributes restoration scripts and license/build information; FFmpeg DLLs and ZIPs are kept in ignored `.cache/ffmpeg/` folders. No manual extraction is needed. Other samples do not invoke this restoration target.

- Pinned build: `n9.0.2-39-g7adfbe40dc-20261031` (`win64-lgpl-shared-9.0`).
- Manifest, exact URL and archive/DLL SHA-256 values: [src/OPNX.Samples.RtspViewer/Build/ffmpeg-runtime.json](https://github.com/OPNXLabs/opnx-samples/blob/master/src/OPNX.Samples.RtspViewer/Build/ffmpeg-runtime.json).
- Release: [autobuild-2026-10-31-12-59](https://github.com/FFmpeg-Builds/FFmpeg-Builds/releases/tag/autobuild-2026-10-31-12-59).
- Build configuration and license texts: [runtime information](https://github.com/OPNXLabs/opnx-samples/blob/master/src/OPNX.Samples.RtspViewer/FFMpeg/Lib/x64/README.txt).

### Offline builds and unavailable downloads

The build does not use `latest` and does not silently substitute a different version. The upstream publisher [retains the last build of each month for two years](https://github.com/FFmpeg-Builds/FFmpeg-Builds#release-retention-policy), so this is a longer-lived download, not a permanent availability guarantee.

A populated cache works offline. To prohibit network downloads:

```powershell
dotnet build src/OPNX.Samples.RtspViewer -p:FFmpegAllowDownload=false
```

To supply the exact pinned ZIP yourself (it is still checked against the manifest):

```powershell
dotnet build src/OPNX.Samples.RtspViewer -p:FFmpegRuntimeArchive="C:/Downloads/ffmpeg-n9.0.2-39-g7adfbe40dc-win64-lgpl-shared-9.0.zip"
```

If the release is gone and you have neither the cache nor the exact archive, restoration stops with an error. Maintainers must update the manifest, the default cache version in `src/OPNX.Samples.RtspViewer/Build/FFmpeg.Runtime.targets`, build information and documentation together after checking the replacement build's ABI and license configuration. No new download endpoint should be accepted just to bypass a checksum error.

### Replacement libraries

An interface-compatible replacement shared library is allowed. For your own FFmpeg 9.0-ABI x64 build, disable automatic restoration and point to a directory containing the seven DLL names listed in the manifest:

```powershell
dotnet build src/OPNX.Samples.RtspViewer -p:FFmpegAutoRestore=false -p:FFmpegRuntimeDirectory="C:/MyFFmpeg/x64"
```

This mode checks required file presence only and does not enforce the pinned hashes. No signature or hash check prevents the running application from loading a compatible replacement DLL. If redistributing a custom build, replace the recorded configuration, provenance and notices to describe what you actually ship.

## License scope and redistribution

The LGPL license texts are included for reference and copied with local output. OPNX's license governs OPNX-owned code; it does not restrict independently granted third-party rights. In the current replaceable-shared-library arrangement, using LGPL FFmpeg does not by itself require publication of OPNX.Lib, OPNX.UI or application source. This statement does not cover copying FFmpeg implementation code into proprietary code, changing to a GPL/nonfree build, or other linking arrangements.

Downloading a runtime for local evaluation and distributing that runtime to other people are different actions. If you distribute a DLL bundle, installer, build output, publish output or container, meet the licenses of the actual FFmpeg build and its dependencies. That includes applicable copyright/license notices, access to the corresponding source and build materials for covered components, and the LGPL rights to modify/replace library portions and debug those modifications. FFmpeg's source alone may not cover all statically included dependencies. Source may be hosted by a third party where the license permits, but the distributor remains responsible for the required availability. These restoration files are not a complete corresponding-source distribution.

- [FFmpeg license](https://github.com/FFmpeg/FFmpeg/blob/master/LICENSE.md)
- [LGPLv3, especially section 4](https://www.gnu.org/licenses/lgpl-3.0.html)
- [GPLv3 section 6 (incorporated by LGPLv3)](https://www.gnu.org/licenses/gpl-3.0.html#section6)

### Previous runtime reference

The short-lived bundled-runtime commits were replaced in the local main-branch history. Existing clones or forks may retain those DLLs; changing history does not change their licenses. The previous runtime was `n9.0.2-48-gd4070a14ce-20261107`, with original archive SHA-256 `1ca86821f7fd84c99be0be3a541bc936e26834cc9db49355853869c4f8ad3a98`, from [this upstream release](https://github.com/FFmpeg-Builds/FFmpeg-Builds/releases/tag/autobuild-2026-11-07-12-47), using [FFmpeg source revision d4070a14cecc7e29eb278f9b6036c2284255f70d](https://github.com/FFmpeg/FFmpeg/tree/d4070a14cecc7e29eb278f9b6036c2284255f70d). Original local files and build information are backed up for follow-up reference. This provenance is not a complete dependency-notice or corresponding-source bundle for that build.

### Maintainer note: publishing the rewritten history

The local history replaces the old DLL-bundling commits rather than merging them. If the remote branch still points to those earlier commits, a normal pull/merge reconnects that history and may restore the DLLs. Inspect the remote changes first; when authorized to publish this replacement history, use `git push --force-with-lease origin master`. If the lease fails, review the newly published remote changes before proceeding. Do not use an unconditional force push. Existing clones may need to reconcile their local branches with the rewritten remote history.

## 한국어 안내

복원 스크립트·빌드 연결 파일·버전 설정은 RtspViewer 프로젝트의 `Build` 폴더에 있으며 Visual Studio 솔루션 탐색기에서 확인할 수 있습니다.

RtspViewer를 평소처럼 빌드하면 최초 한 번 고정된 LGPL shared ZIP을 외부 배포처에서 직접 다운로드하고, SHA-256을 검증한 뒤 필요한 DLL 7개만 자동 압축 해제해 출력 폴더로 복사합니다. 다음 빌드부터는 네트워크 요청이나 DLL 전체 해시 재검사 없이 로컬 캐시를 재사용합니다. Debug·Release 및 로컬 publish 출력에 같은 DLL과 고지 문서가 복사됩니다. 다른 샘플에는 이 복원 작업이 적용되지 않습니다.

저장소에는 DLL이나 ZIP 대신 복원 스크립트와 라이선스·빌드 정보를 보관합니다. 다운로드 파일은 Git에서 제외된 `.cache/ffmpeg/`에 저장됩니다. 사용자가 압축을 풀거나 DLL을 수동 복사할 필요는 없습니다.

`latest`를 사용하지 않고 날짜·파일명·체크섬을 고정합니다. 배포처의 월말 빌드 보관 정책은 2년이므로 영구 링크는 아닙니다. 캐시가 있으면 오프라인으로 빌드할 수 있고, 정확한 ZIP은 위의 `FFmpegRuntimeArchive` 옵션으로 지정할 수 있습니다. 다운로드가 불가능하거나 체크섬이 다르면 다른 버전으로 임의 변경하지 않고 오류로 안내합니다. 호환되는 자체 DLL은 `FFmpegAutoRestore=false`와 `FFmpegRuntimeDirectory` 옵션으로 사용할 수 있습니다.

교체 가능한 LGPL 공유 라이브러리를 사용하는 현재 방식 자체만으로 OPNX.Lib·OPNX.UI·애플리케이션 자체 소스 코드를 공개해야 하는 것은 아닙니다. 다만 DLL을 다른 사람에게 재배포하면 FFmpeg 및 해당 의존성의 대응 소스·빌드 자료 제공과 고지, 라이브러리 교체 및 수정 디버깅 권리 등의 조건을 충족해야 합니다. 자동 다운로드 방식이 생성된 바이너리의 재배포 의무까지 없애 주는 것은 아닙니다.

DLL을 잠시 포함했던 두 커밋은 로컬 주 브랜치 이력에서 새 커밋으로 대체했습니다. 기존 클론·포크의 파일까지 회수하는 것은 아니며 해당 파일의 라이선스는 계속 적용됩니다. 이전 버전의 출처·체크섬·FFmpeg 소스 리비전은 위에 기록하고 원본 파일과 빌드 정보는 로컬에 보존했습니다. 이 출처 기록 자체가 해당 빌드의 전체 의존성 고지나 대응 소스 묶음을 대체하지는 않습니다.

관리자 참고: 원격에 예전 DLL 포함 커밋이 남아 있는 상태에서 일반 Pull·Merge를 하면 제거했던 이력이 다시 연결될 수 있습니다. 원격의 추가 변경을 확인한 후, 이력 교체를 반영할 때는 `git push --force-with-lease origin master`를 사용합니다. 보호 조건에 걸려 거절되면 원격의 새 변경을 먼저 검토하고 무조건 강제 푸시는 사용하지 않습니다.
