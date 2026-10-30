# OPNX 샘플

[English](README.md)

**OPNX.Lib와 OPNX.UI를 직접 평가할 수 있는 C# 실행 샘플입니다.**

RTSP MultiView · FFmpeg 디코딩 · WPF 영상 · 재생 타임라인 · TCP · EntityStore

[RTSP 뷰어 실행](#rtsp-multi-live-viewer-실행) · [타임라인 실행](#playback-timeline-실행) · [OPNX.Lib](https://github.com/OPNXLabs/opnx-lib) · [OPNX.UI](https://github.com/OPNXLabs/opnx-ui) · [평가 및 OEM 문의](#상업-라이선스-및-oem-문의)

![OPNX RTSP 멀티 라이브 뷰어](docs/images/rtsp-multi-live-viewer.png)

## 시작할 샘플을 선택하세요

| 샘플 | 확인할 수 있는 기능 | 요구사항 |
| --- | --- | --- |
| [`RtspMultiLiveViewer`](#rtsp-multi-live-viewer-실행) | RTSP 입력, FFmpeg 디코딩, WPF 영상 표시, 카메라 탐색 및 구성 가능한 MultiView | Windows, .NET 10, FFmpeg 네이티브 DLL, RTSP 소스 |
| [`PlaybackTimeline`](#playback-timeline-실행) | 다채널 녹화 구간, 이벤트, 시간 탐색, 선택 및 타임라인 스타일 | Windows 및 .NET 10 |
| [`TcpChat`](#tcp-chat-실행) | OPNX TCP 클라이언트/서버, 패킷 프레이밍, 연결 이벤트 및 텍스트 전달 | .NET 10 |
| [`EntityStore`](#entitystore-실행) | 메모리 기반 엔티티 CRUD, 조회, 복사/업데이트 및 변경 알림 | .NET 10 |

> **라이선스:** OPNX Samples는 오픈 소스가 아닌 source-available 소프트웨어입니다. [LICENSE.txt](LICENSE.txt)에 따라 학습, 연구, 테스트 및 비상업적 평가에 사용할 수 있습니다. 상업적 사용, 운영 환경 사용, 재배포 및 OEM 통합에는 OPNX의 사전 서면 허가가 필요합니다. 샘플은 OPNX.Lib 또는 OPNX.UI에 대한 추가 권리를 부여하지 않습니다.
> 기업 내부의 비운영 평가·PoC·기술 검토도 허용됩니다. 상세 범위는 아래 평가 안내와 영문 라이선스를 확인하세요.

## 샘플에서 VMS/NVR까지

샘플은 SDK와 UI의 개별 기능을 분리해 보여줍니다. OPNX.V는 같은 기반을 라이브 모니터링, 스케줄 녹화, 저장영상 검색, 동기 재생, 장치 관리, 권한, 로그, 이벤트 및 알람을 포함하는 개발 중인 VMS/NVR 애플리케이션으로 구성합니다.

![OPNX.V 다채널 라이브 모니터링](docs/images/opnx-v-live-monitoring.png)

## 샘플

- `OPNX.Samples.RtspMultiLiveViewer`: OPNX.Lib와 OPNX.UI.WPF를 사용하는 WPF RTSP 멀티 라이브 뷰어 샘플입니다.
- `OPNX.Samples.TcpChat`: OPNX.Lib의 TCP 서버, 클라이언트, 패킷 프레이밍, 메시지 전달 API를 사용하는 콘솔 TCP 채팅 샘플입니다.
- `OPNX.Samples.EntityStore`: OPNX.Lib의 메모리 기반 엔티티 CRUD, 조회, 복사/업데이트, 변경 이벤트를 사용하는 콘솔 EntityStore 샘플입니다.
- `OPNX.Samples.PlaybackTimeline`: OPNX.Lib 데이터 모델과 OPNX.UI.WPF 타임라인 컨트롤을 사용하는 WPF 재생 타임라인 샘플입니다.

## 사용한 OPNX 컴포넌트

이 샘플들은 OPNX.Lib와 OPNX.UI만으로 작은 애플리케이션을 구성하는 방법을 보여주기 위한 예제입니다.

OPNX.V 서버, 저장소, 사용자, 권한, 장치 관리, 녹화 모델에는 의존하지 않습니다.

`OPNX.Samples.RtspMultiLiveViewer`는 아래 컴포넌트 사용 흐름을 보여줍니다.

- `OPNX.Lib.Streaming.RTSP`: RTSP 연결, 스트림 구성, NAL unit 전달.
- `OPNX.Lib.Media.FFMpeg`: FFmpeg 초기화와 디코딩된 영상 프레임 처리.
- `OPNX.UI.WPF.Controls.OpnxMultiView`: 라이브뷰 타일 레이아웃, 선택 셀 배치, 셀 분할과 병합.
- `OPNX.UI.WPF.Controls.OpnxTreeListView`: 카메라 목록 표시.

- `OPNX.UI.WPF.Controls.OpnxTitlebar`: borderless WPF 창의 커스텀 타이틀바.
- `OPNX.UI.WPF.Controls.OpnxButton`, `OpnxTextBox`, `OpnxPasswordBox`, `OpnxComboBox`, `OpnxNumericBox`: OPNX UI 리소스로 스타일링된 입력 및 명령 컨트롤.

`OPNX.Samples.TcpChat`는 아래 컴포넌트 사용 흐름을 보여줍니다.

- `OPNX.Lib.Network.Protocol.Tcp.OPNXTcpServer`: TCP 서버 accept 흐름.
- `OPNX.Lib.Network.Protocol.Tcp.OPNXTcpClient`: TCP 클라이언트 연결 래핑과 패킷 송수신.
- `OPNX.Lib.Network.Protocol.Framing.PacketHeader`: 프레이밍된 텍스트 메시지를 위한 OPNX 패킷 헤더 사용.
- `PacketReceived`, `Disconnected`, `ClientAccepted` 이벤트를 이용한 간단한 연결 지향 통신.

OPNX.Lib.Network는 공통 연결 모델과 패킷 개념을 중심으로 설계되어 있습니다.

TCP, Named Pipe, Shared Memory 전송 방식은 비슷한 클라이언트/서버 스타일의 사용 패턴과 동일한 패킷 프레이밍 모델을 공유할 수 있습니다.

애플리케이션 코드는 메시지 정의와 패킷 처리 방식을 일관되게 유지하면서 배포 경계에 맞는 전송 방식을 선택할 수 있습니다.

이 샘플은 가장 실행하기 쉽고 이해하기 쉬운 TCP를 사용합니다.

패킷 구조와 이벤트 기반 통신 방식은 다른 전송 방식에도 이어지는 것을 의도합니다.

`OPNX.Samples.EntityStore`는 아래 컴포넌트 사용 흐름을 보여줍니다.

- `OPNX.Lib.Data.ORM.Entity`: 작은 샘플 엔티티 정의.
- `OPNX.Lib.Data.ORM.Datas.Attributes.DataColumnAttribute`: ORM 스타일 매핑을 위한 엔티티 필드 설명.
- `OPNX.Lib.Data.ORM.EntityStore`: 메모리 기반 insert, update, delete, lookup, collection access.
- `EntityChanged` 이벤트를 이용한 insert, update, delete, property-change 알림.

`EntityStore`는 EF Core를 대체하기 위한 범용 ORM이라기보다 OPNX 지향 엔티티 저장 패턴입니다.

OPNX 애플리케이션에서 상태 엔티티를 메모리에 유지하고 변경 이벤트를 발생시키는 용도에 초점을 둡니다.

또한 속성 기반 필드 메타데이터를 제공하고 MySQL/PostgreSQL 같은 DB 서비스와 선택적으로 동기화할 수 있습니다.

이 샘플은 외부 DB 준비 없이 실행할 수 있도록 DB 설정을 기본 흐름에서 제외했습니다.

`OPNX.Samples.PlaybackTimeline`은 아래 컴포넌트 사용 흐름을 보여줍니다.

- `OPNX.UI.WPF.Controls.OpnxPlaybackTimeline`: 재생 범위, 현재 시각, 선택 및 타임라인 시각화.
- OPNX.Lib 기반 녹화·이벤트 모델과 재생 UI 컨트롤의 바인딩.
- 타임라인 레이아웃, 스타일, 확대 범위 선택 및 녹화 미디어 리뷰 상호작용.

![OPNX 재생 타임라인](docs/images/playback-timeline.png)

## 빌드

```powershell
dotnet build .\OPNX.Samples.slnx -c Debug
```

## RTSP Multi Live Viewer 실행

```powershell
dotnet run --project src/OPNX.Samples.RtspMultiLiveViewer
```

## TCP Chat 실행

첫 번째 터미널에서 서버를 실행합니다.

```powershell
dotnet run --project src/OPNX.Samples.TcpChat -- server
```

두 번째 터미널에서 클라이언트를 실행합니다.

```powershell
dotnet run --project src/OPNX.Samples.TcpChat -- client
```

양쪽 모두 메시지를 입력하고 Enter를 누르면 상대에게 전송됩니다. `exit`를 입력하면 종료합니다.

## EntityStore 실행

```powershell
dotnet run --project src/OPNX.Samples.EntityStore
```

## Playback Timeline 실행

```powershell
dotnet run --project src/OPNX.Samples.PlaybackTimeline
```

## FFmpeg 네이티브 바이너리

FFmpeg를 사용하는 샘플은 실행 시 호환되는 FFmpeg 네이티브 DLL이 필요합니다. 관리 코드 패키지인 `FFmpeg.AutoGen`에는 FFmpeg 네이티브 바이너리가 포함되어 있지 않습니다.

애플리케이션 실행 경로 기준으로 FFmpeg를 초기화하는 샘플은 아래 위치에 네이티브 DLL을 배치합니다.

```text
FFMpeg/Lib/x64
FFMpeg/Lib/x86
```

64비트 Debug 실행 기준의 실무적인 준비 순서는 다음과 같습니다.

1. 먼저 솔루션을 한 번 빌드해서 출력 폴더를 생성합니다.
2. 프로세스 아키텍처와 맞는 FFmpeg shared-library DLL을 준비합니다.
3. 빌드 출력 폴더 아래에 `FFMpeg/Lib/x64` 디렉터리를 만듭니다.
4. 준비한 FFmpeg DLL을 해당 디렉터리에 복사합니다.
5. 샘플을 다시 실행합니다.

예를 들어 64비트 Debug 출력 폴더에는 아래와 비슷한 파일들이 있어야 합니다.

```text
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/avcodec-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/avformat-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/avutil-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/avfilter-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/avdevice-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/swscale-*.dll
bin/x64/Debug/net10.0-windows/FFMpeg/Lib/x64/swresample-*.dll
```

샘플 화면에 `FFMPEG initialize failed`가 표시된다면, 상태 메시지에 표시된 경로에 DLL 파일이 있는지 먼저 확인합니다.

또한 DLL 아키텍처가 실행 프로세스와 맞는지도 확인합니다.

이 샘플 저장소에는 FFmpeg 바이너리를 커밋하지 않습니다.

자체 빌드와 함께 FFmpeg 바이너리를 재배포한다면, 선택한 FFmpeg 빌드가 사용 목적에 맞게 재배포 가능한지 확인해야 합니다.

또한 해당 FFmpeg 라이선스 조건을 준수해야 합니다.

일반적으로 애플리케이션이 GPL 또는 nonfree 요구사항을 충족할 준비가 되어 있지 않다면, 동적 링크 방식의 LGPL 빌드를 사용하고 GPL/nonfree 구성 요소가 포함되지 않은 빌드를 선택하는 편이 좋습니다.

참고:

- https://ffmpeg.org/legal.html
- https://ffmpeg.org/download.html

## 패키지 의존성

모든 샘플은 로컬 OPNX 소스 프로젝트가 아니라 공개된 NuGet 패키지를 사용합니다.

- `OPNX.Lib`: `0.1.0-preview.20260716.1`
- `OPNX.UI.WPF`: `0.1.0-preview.20260716.1`

패키지 버전은 [Directory.Build.props](Directory.Build.props)에서 중앙 관리합니다.

## 평가와 코드 활용 범위

- 개인 학습, 연구, 테스트와 비상업적 평가에 사용할 수 있습니다.
- 기업 내부의 비운영 평가, PoC와 기술 검토도 라이선스에 따라 별도 서면 허가 없이 가능합니다.
- 실제 운영, 고객 대상 서비스, 상용 제품·OEM 통합과 재배포에는 사전 서면 허가가 필요합니다.
- 평가용 코드 수정 권한이 상용 제품에 코드를 포함하거나 수정본을 재배포할 권한까지 부여하지는 않습니다.
- OPNX 소유 코드와 의존성의 라이선스는 각각 적용됩니다. 최종 조건은 저장소의 영문 라이선스를 확인하세요.

## 최신 SDK와 샘플 버전

이 저장소는 위에 명시된 공개 패키지 버전으로 실행하는 네 개의 샘플을 제공합니다. 최신 Lib·UI 소스의 모든 기능을 포함하지 않습니다.

| 현재 SDK 소스의 기능 | 이 샘플 저장소 |
| --- | --- |
| RTSP 입력·디코딩·MultiView | `RtspMultiLiveViewer` |
| 다채널 재생 타임라인 | `PlaybackTimeline` |
| TCP 메시지 송수신 | `TcpChat` |
| 메모리 EntityStore CRUD | 고정 패키지 API를 사용하는 `EntityStore` |
| WebRTC 미디어 송출·Kestrel 시그널링 | 전용 실행 샘플 없음 |
| TCP 파일 전송·SharedMemory 원본 버퍼 | 전용 실행 샘플 없음 |
| Remux·자막·알림·트레이·시간 입력 | 전용 실행 샘플 없음 |

EntityStore 샘플의 `NewEntity`, `OldEntity`, `PropertyName`, `Value`는 고정된 이전 패키지 API입니다. 최신 Lib는 `EntityType`, `EntityID`, `OriginalValues`, `CurrentValues`, `ChangedProperties`를 제공하고 `FindCurrent<TEntity, TKey>`로 현재 엔티티를 조회합니다. 패키지를 갱신할 때는 이벤트 처리와 조회 API를 함께 마이그레이션해야 합니다.

네이티브 FFmpeg도 선택한 패키지의 FFmpeg.AutoGen 버전에 맞춰 준비합니다. 최신 Lib·UI 소스의 `9.0.1.1` 바인딩 요구사항을 이전 패키지에 그대로 적용하지 않습니다. 최신 WebRTC를 포함한 통합 SDK로 갱신하면 ASP.NET Core 공유 프레임워크와 DataChannelDotnet 네이티브 자산 요구사항도 함께 확인해야 합니다.

## 라이선스

OPNX Samples는 source-available 형태로 공개되지만 permissive open-source software로 라이선스되는 것은 아닙니다.

OPNX 소유 샘플 코드는 학습, 평가, 연구, 테스트 및 기타 비상업적 목적으로 사용할 수 있습니다. 상업적 사용, 재배포, OEM 통합, 상업 제품 또는 서비스에의 포함에는 OPNX의 사전 서면 허가가 필요합니다.

샘플 공개는 OPNX.Lib, OPNX.UI, FFmpeg 또는 다른 의존성의 상업적 사용이나 재배포 권한을 부여하지 않습니다. 각 컴포넌트에는 해당 컴포넌트의 라이선스가 적용됩니다.

자세한 조건은 [LICENSE.txt](LICENSE.txt)를 확인하십시오. 한국어 참고 번역은 [LICENSE.ko.txt](LICENSE.ko.txt)에서 확인할 수 있습니다. 의존성 고지는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)를 확인하십시오.

## 관련 프로젝트

- [OPNX.Lib](https://github.com/OPNXLabs/opnx-lib)
- [OPNX.UI](https://github.com/OPNXLabs/opnx-ui)

## 상업 라이선스 및 OEM 문의

OPNX Samples는 대한민국에 사업자로 등록된 오픈엑스(OPNX)가 개발하고 배포합니다.

- [https://www.opnx.kr/](https://www.opnx.kr/)
- `opnx@opnx.kr`

## 보안 및 기여

- 보안 문제는 [SECURITY.ko.md](https://github.com/OPNXLabs/opnx-samples/blob/master/SECURITY.ko.md)의 절차에 따라 비공개로 제보해 주세요.
- 이슈 등록이나 변경 제안 전 [CONTRIBUTING.ko.md](https://github.com/OPNXLabs/opnx-samples/blob/master/CONTRIBUTING.ko.md)를 확인해 주세요.
