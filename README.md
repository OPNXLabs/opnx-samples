# OPNX Samples

[Korean](README.ko.md)

**Runnable C# samples for evaluating OPNX.Lib and OPNX.UI.**

RTSP viewer · WebRTC browser playback · Playback timeline · Control gallery · System monitoring · Protocol chat · EntityStore

[Run the RTSP viewer](#run-rtsp-viewer) · [Run WebRTC](#run-webrtc-viewer) · [Run the timeline](#run-playback-timeline) · [OPNX.Lib](https://github.com/OPNXLabs/opnx-lib) · [OPNX.UI](https://github.com/OPNXLabs/opnx-ui) · [Evaluation and OEM inquiries](#commercial-and-oem-inquiries)


## Choose A Starting Point

| Sample | What it demonstrates | Requirements |
| --- | --- | --- |
| [`RtspViewer`](#run-rtsp-viewer) | RTSP input, FFmpeg decoding, WPF video presentation, camera navigation, and configurable MultiView | Windows, .NET 10, FFmpeg native DLLs, RTSP source |
| [`WebRtcViewer`](#run-webrtc-viewer) | RTSP → WebRTC → WebView2 / browser video, live receive statistics | Windows, .NET 10, WebView2 Runtime, H.264 RTSP source |
| [`PlaybackTimeline`](#run-playback-timeline) | Multi-channel recording ranges, events, time navigation, selection, and timeline styling | Windows and .NET 10 |
| [`ControlGallery`](#run-control-gallery) | Basic/operational controls, live themes and accents, tree/list search and paging | Windows and .NET 10 |
| [`SystemMonitoring`](#run-system-monitoring) | Local CPU, memory, process, network, disk and GPU metrics with OPNX.UI controls | Windows and .NET 10 |
| [`ProtocolChat`](#run-protocol-chat) | TCP / Named Pipe / SharedMemory with common chat, request/response, timeout, and cancellation logic | .NET 10; SharedMemory requires Windows |
| [`EntityStore`](#run-entitystore) | In-memory entity CRUD, lookup, copy/update, and change notifications | .NET 10 |

> **License:** OPNX Samples is source-available, not open-source. Learning, research, testing, and non-commercial evaluation are permitted under [LICENSE.txt](LICENSE.txt). Commercial use, production use, redistribution, and OEM integration require prior written permission from OPNX. The samples do not grant additional rights to OPNX.Lib or OPNX.UI.
> Internal non-production evaluation, proof of concept, and technical review by commercial organizations are also permitted. See the evaluation guidance below and the English license for scope.

## From Samples To A VMS/NVR

The samples isolate individual SDK and UI capabilities. OPNX.V composes the same foundations into a VMS/NVR application under active development, including live monitoring, scheduled recording, recorded-video search, synchronized playback, device management, permissions, logs, events, and alarms.

![OPNX.V multi-channel live monitoring](docs/images/opnx-v-live-monitoring.png)

## Samples

- `OPNX.Samples.RtspViewer`: A WPF RTSP multi-live viewer sample using OPNX.Lib and OPNX.UI.WPF.
- `OPNX.Samples.WebRtcViewer`: A WPF RTSP-to-WebRTC bridge with an embedded WebView2 player and a shared external-browser player.
- `OPNX.Samples.PlaybackTimeline`: A WPF playback timeline sample using OPNX.Lib data models and OPNX.UI.WPF timeline controls.
- `OPNX.Samples.ControlGallery`: Interactive basic and operational controls with live palette switching, a searchable tree/list, paging, notifications, and optional tray integration.
- `OPNX.Samples.SystemMonitoring`: Local resource monitoring with live OPNX.UI gauges, themed tabs, and resource lists.
- `OPNX.Samples.ProtocolChat`: A console chat sample sharing packet and message handling across TCP, Named Pipe, and SharedMemory.
- `OPNX.Samples.EntityStore`: A console EntityStore sample using OPNX.Lib in-memory entity CRUD, lookup, copy/update, and change events.

## UI Layout And First Actions

The five WPF samples share a title bar, cards/panels, status feedback, and OPNX.UI input and command controls. Switch **OPNX.V Dark / Graphite / Light** and **Blue / Teal / Amber** from the upper-right selectors. Each project links `src/Shared/Themes/SampleTheme.cs` and `Controls.xaml`; none references OPNX.V projects.

| Screen | Layout and purpose | Try first |
| --- | --- | --- |
| **RtspViewer** | Left camera list and `Add camera`; layout toolbar above MultiView; status below. Registration dialog and video headers share the palette | Add a camera → double-click the source → split/merge → wheel zoom and right-click capture. `Save layout`/`Restore` last only for the current application session |
| **WebRtcViewer** | RTSP URL, account, password and `Connect`/`Stop`/`Open browser` above an embedded player with connection state, resolution, FPS and bitrate | Connect an H.264 source → play → open the same player in an external browser. `Stop` stops the source for all viewers; `Stop player` stops only that player |
| **PlaybackTimeline** | Simulated playback and rate above recording/event rows; selection details below; Appearance inspector on the right | Select an event → navigate time → `Simulate play` → change row/bar dimensions and colors. This is simulated data, not video playback |
| **ControlGallery** | Cards group input, selection, dates/time, buttons, navigation, progress, tree/list data and desktop integration | Validate the port → toggle `Tree mode` → search/page → preview a notification. The tray icon is opt-in |
| **SystemMonitoring** | Collection controls and timestamp, CPU/memory/current-process/GPU summaries, and network/disk/GPU detail tabs | Change the 1–60 second interval → stop/restart → filter interfaces. Disk values are capacity; unavailable GPU values show `N/A` |

Video surfaces remain black. The embedded WebRtcViewer HTML player receives palette messages from WPF; an external browser uses its default dark palette. OPNX.UI supplies the desktop controls, while the browser's native `<video>` presents WebRTC media.

Current Release screenshots appear with the run instructions below, using OPNX.V Dark / Blue and a common image canvas. Machine name and local path are redacted. See the [image notes](docs/images/README.md) for capture states.

## Used OPNX Components

The samples show how OPNX.Lib and OPNX.UI can be used together in small applications.

They do not depend on OPNX.V server, storage, user, permission, device-management, or recording models.

`OPNX.Samples.RtspViewer` demonstrates:

- `OPNX.Lib.Streaming.RTSP`: RTSP connection, stream configuration, and NAL unit delivery.
- `OPNX.Lib.Media.FFMpeg`: FFmpeg initialization and decoded video frame handling.
- `OPNX.UI.WPF.Controls.OpnxMultiView`: live-view tile layout, selected-cell placement, split, and merge.
- `OPNX.UI.WPF.Controls.OpnxTreeListView`: camera list presentation.

- `OPNX.UI.WPF.Controls.OpnxTitlebar`: custom borderless WPF window title bar.
- `OPNX.UI.WPF.Controls.OpnxButton`, `OpnxTextBox`, `OpnxPasswordBox`, `OpnxComboBox`, and `OpnxNumericBox`: common input and command controls styled with OPNX UI resources.

`OPNX.Samples.WebRtcViewer` connects `RTSPClient` → `RTPPacketMaker` → `DataChannelPeerConnection`. A loopback Kestrel host serves `Player.html` and exchanges SDP/ICE over WebSocket. It uses the public OPNX.Lib API and has no dependency on OPNX.V camera entities or servers.

`OPNX.Samples.ProtocolChat` demonstrates three transports with one `ChatPeer` message handler and one console input loop:

| Transport | SDK components | Connection setup |
| --- | --- | --- |
| TCP | `OPNXTcpServer`, `OPNXTcpClient` | IP address and port; one accepted client per run |
| Named Pipe | `OPNXNPipeServer`, `OPNXNPipeClient` | Matching local pipe name |
| SharedMemory | `OPNXSMProducer`, `OPNXSMConsumer` | Two named one-way channels, reversed for server/client |

TCP and Named Pipe clients share `OPNXClientBase`. The Named Pipe server and SharedMemory endpoints expose matching packet events and send operations but do not all implement one SDK interface. The sample supplies their send operations to `ChatPeer` as delegates; no extra transport interface is introduced. Only connection setup and lifetime differ. `PacketHeader`, payload types, serialization, text handling, request IDs, response waiting, timeouts, and cancellation are shared.

SharedMemory uses Windows named maps in this SDK and has no socket-style connected/disconnected event. A small common readiness exchange confirms that the other sample process is running before input starts. Startup waits up to 30 seconds; request timeout is three seconds. Run only one server and one client for each endpoint name; restart both after a session. TCP can connect separate machines; the sample's Named Pipe and SharedMemory modes are local IPC. Named Pipe clients should be started after their server; SharedMemory can start in either order.

`OPNX.Samples.EntityStore` demonstrates:

- `OPNX.Lib.Data.ORM.Entity`: defining a small sample entity.
- `OPNX.Lib.Data.ORM.Datas.Attributes.EntityColumnAttribute`: describing entity fields for ORM-style mapping.
- `OPNX.Lib.Data.ORM.EntityStore`: in-memory insert, update, delete, lookup, and collection access.
- `EntityChanged` events for insert, update, delete, and property-change notification.

`EntityStore` is not intended to be an EF Core replacement.

It provides an OPNX-oriented entity store pattern for keeping stateful entities in memory and raising entity change events.

It also describes fields with attributes and can optionally synchronize with database services such as MySQL or PostgreSQL.

This sample keeps database setup out of the default path so it can run without external infrastructure.

`OPNX.Samples.PlaybackTimeline` demonstrates:

- `OPNX.UI.WPF.Controls.OpnxPlaybackTimeline`: playback range, current time, selection, and timeline visualization.
- OPNX.Lib-backed record and event models bound to playback UI controls.
- Timeline layout, styling, zoom/range selection, and recorded-media review interaction.


## Interactions In The Updated Samples

- **RTSP viewer:** drag tile boundaries to resize. Use the mouse wheel over video for digital zoom; right-click to reset zoom or save the displayed frame as PNG. `Save layout` stores geometry for the current application session; `Restore layout` reuses available connections and stops videos beyond the saved capacity. `Detach video` moves the selected video to another window without reconnecting. Closing that window returns it to an empty tile, or stops it if no tile is available. Layout presets, split, and merge stop affected videos before changing their cells. No camera credentials are written to disk.
- **Timeline:** eight simulated channels contain recording intervals, gaps, and motion events. Click a channel, bar, or event to inspect selection details; pan or change the range to query mock data for that range. `Simulate play` and 0.25x–8x rates move the timeline only; no media files or video player are attached.
- **EntityStore:** run the console sample to inspect CRUD, immutable row changes, direct property notifications, and tracked-instance identity. It requires no database.
- **Protocol chat:** normal text remains a notice packet. `/request <text>` sends a correlated echo request; `/timeout` deliberately receives no response and times out after three seconds; `/cancel` cancels a no-response request after 250 ms. Ctrl+C exits. Request IDs and waiting logic are implemented in this sample on top of the SDK packet types.

## Build

Use the .NET 10 SDK on Windows with the ASP.NET Core 10 shared framework. The full solution includes WPF projects. FFmpeg native DLLs are needed to run RtspViewer; the WebView2 Runtime is needed to run WebRtcViewer.

```powershell
dotnet build .\OPNX.Samples.slnx -c Debug
```

## Run RTSP Viewer

![OPNX RTSP Viewer](docs/images/rtsp-viewer.png)

MultiView layout and source panel. FFmpeg native DLLs were unavailable in the capture environment; the initialization error is visible and no video is playing.

```powershell
dotnet run --project src/OPNX.Samples.RtspViewer
```

## Run WebRTC Viewer

![OPNX WebRTC Viewer](docs/images/webrtc-viewer.png)

Input and embedded browser player before connection; camera address and credentials are empty.

```powershell
dotnet run --project src/OPNX.Samples.WebRtcViewer
```

Windows, .NET 10 and the Microsoft Edge WebView2 Runtime are required. Enter an `rtsp://` URL, optional account and password, then choose **Connect**. Use **Stop** to disconnect the source and all players. **Open browser** opens the same player in your default browser (Chrome/Edge recommended); the desktop application must remain running.

This sample uses OPNX.Lib RTSP-over-TCP input and WebRTC H.264 output, with a native HTML `<video>` player embedded through WebView2. It requires no FFmpeg binaries. Select an H.264 camera stream compatible with the negotiated browser profile; H.265 transcoding and audio forwarding are not included. The player shows received resolution, FPS and bitrate. A newly connected player waits for the next keyframe. Up to four local players can connect.

The relay binds to `127.0.0.1` on an automatically selected port and uses an ephemeral player path. Camera credentials remain in memory in the desktop process and are not placed in browser URLs or saved to configuration. Remote access, TURN and production authentication are outside this sample. Closing the application stops the relay and streams. Native DataChannelDotnet dependencies are supplied by OPNX.Lib; Windows x64 is recommended.

## Run Playback Timeline

![OPNX Playback Timeline](docs/images/playback-timeline.png)

Simulated recording ranges, motion event selection and the Appearance inspector.

```powershell
dotnet run --project src/OPNX.Samples.PlaybackTimeline
```

## Run Control Gallery

![OPNX Control Gallery](docs/images/control-gallery.png)

Input, validation, selection, date/time, command, navigation and progress controls.

![OPNX Control Gallery tree and paging](docs/images/control-gallery-data.png)

Grouped tree, search and desktop integration area.

```powershell
dotnet run --project src/OPNX.Samples.ControlGallery
```

One window groups input, selection, dates/time, buttons, navigation, progress, data, and desktop integration. Change `Theme` (OPNX.V Dark / Graphite / Light) and `Accent` (Blue / Teal / Amber) to update existing controls immediately; `Reset theme` restores the defaults. The sample uses independent palettes following OPNX.V's common-brush and control-style resource approach and does not reference OPNX.V projects.

The data section includes a searchable, paged flat list and a tree grouped by server; switch `Tree mode` to compare them. Enter a valid port to clear the validation error. Notifications use the current palette. The tray icon is opt-in, keeps the gallery visible, and is removed on exit. The current package's internal DateRangeSelector styles are adapted in the sample after its visual children are generated. Video controls remain covered by the existing viewer/timeline samples. No FFmpeg setup is needed for this gallery.

## Run System Monitoring

![OPNX System Monitoring](docs/images/system-monitoring.png)

Live local resource collection; the machine name is redacted.

```powershell
dotnet run --project src/OPNX.Samples.SystemMonitoring
```

Collection starts automatically at a two-second interval. `Stop` retains the last successful snapshot and timestamp; `Start collecting` resumes with a new collector. Set an interval from 1 to 60 seconds; changing it during collection restarts the monitor. CPU/process usage and network rates show a sampling state until a baseline is available. A newly appearing network interface also needs its own baseline.

The OPNX.UI dashboard uses `OpnxTitlebar`, `OpnxComboBox`, `OpnxNumericBox`, `OpnxButton`, `OpnxRadialProgressBar`, `OpnxTabControl`/`OpnxTabItem`, `OpnxCheckBox`, and `OpnxTreeListView`. Network, disk-volume capacity, and GPU details have separate tabs. Network rates are bytes per second; the list may include virtual/filter/loopback interfaces, so it is not summed into a potentially duplicated total. `Active interfaces only` filters by operational status. The process card describes this sample process, not every running process.

Missing nullable GPU values appear as `N/A`, meaning unsupported or unavailable; the SDK does not distinguish every missing-value cause. An empty GPU list means no data was returned, not necessarily that no GPU exists. Whole-collection errors retain the last successful values and show an error message. GPU support depends on the OS and driver; this WPF sample runs on Windows. Disk usage is capacity, not I/O throughput. It does not modify system settings, query remote machines, or require FFmpeg binaries.

The gallery, monitoring, RTSP viewer, timeline, and WebRTC viewer apps link the same `src/Shared/Themes/SampleTheme.cs` and `Controls.xaml` sources. Theme/accent changes update existing controls without restarting collection. No private OPNX.V project is referenced. WebRtcViewer additionally uses Microsoft WebView2 for browser playback.

## Run Protocol Chat

Open two terminals in the repository directory. Choose one transport and run its server command in the first terminal and client command in the second:

```powershell
# TCP (default: 127.0.0.1:9000)
dotnet run --project src/OPNX.Samples.ProtocolChat -- tcp server
dotnet run --project src/OPNX.Samples.ProtocolChat -- tcp client

# Local Named Pipe (default name: opnx-protocol-chat)
dotnet run --project src/OPNX.Samples.ProtocolChat -- pipe server
dotnet run --project src/OPNX.Samples.ProtocolChat -- pipe client

# Local SharedMemory (Windows only; default name: opnx-protocol-chat)
dotnet run --project src/OPNX.Samples.ProtocolChat -- shm server
dotnet run --project src/OPNX.Samples.ProtocolChat -- shm client
```

Both processes print `peer ready` when the common readiness exchange completes. Enter text to send a message. Use `/request hello` for an echo response, `/timeout` for a deliberately unanswered request, `/cancel` for cancellation, and `exit` or Ctrl+C to stop.

Optional arguments are `tcp server|client [IP] [port]`, `pipe server|client [pipe-name]`, and `shm server|client [channel-name]`. Both peers must select the same transport and endpoint. Bare `server`/`client` remains shorthand for TCP, but the project path is now `OPNX.Samples.ProtocolChat`.

## Run EntityStore

```powershell
dotnet run --project src/OPNX.Samples.EntityStore
```

## Shared UI Appearance

All five WPF samples use the same common-brush palettes and control styles from `src/Shared/Themes`. Switch theme and accent from the upper-right controls. The RTSP viewer uses a source panel, layout toolbar, and persistent status line; its registration dialog and video headers follow the same palette. Video surfaces remain black. The timeline keeps playback and selection in the main workspace and exposes dimensions, visibility, and colors in its right-hand appearance panel. Changing its theme preserves timeline data, selection, and playback state. Theme-aware recording colors maintain contrast in both light and dark modes. The current package's fixed timeline header labels are adapted inside the sample.

## FFmpeg Native Binaries

Only `RtspViewer` initializes FFmpeg in these samples and requires compatible FFmpeg native DLLs at runtime. The managed `FFmpeg.AutoGen` package does not include FFmpeg native binaries.

For samples that initialize FFmpeg from the application directory, place the native DLLs under this application-relative path:

```text
FFMpeg/Lib/x64
FFMpeg/Lib/x86
```

A practical setup for a 64-bit Debug run is:

1. Build the solution once so the output directory is created.
2. Prepare FFmpeg shared-library DLLs that match the process architecture.
3. Create the `FFMpeg/Lib/x64` directory under the build output.
4. Copy the FFmpeg DLLs into that directory.
5. Run the sample again.

For example, the 64-bit Debug output should contain files similar to:

```text
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/avcodec-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/avformat-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/avutil-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/avfilter-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/avdevice-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/swscale-*.dll
src/OPNX.Samples.RtspViewer/bin/Debug/net10.0-windows/FFMpeg/Lib/x64/swresample-*.dll
```

If the sample shows `FFMPEG initialize failed`, first check the directory reported by the status message.

Also make sure the DLL architecture matches the process architecture.

FFmpeg binaries are not committed to this samples repository.

If you redistribute FFmpeg binaries with your own build, make sure the selected FFmpeg build is redistributable for your use case.

Also comply with the matching FFmpeg license terms.

In general, prefer an LGPL build that is dynamically linked and built without GPL or nonfree components unless your application is prepared to satisfy GPL/nonfree requirements.

Useful references:

- https://ffmpeg.org/legal.html
- https://ffmpeg.org/download.html

## Package Dependencies

All samples consume the published NuGet packages rather than local OPNX source projects.

- `OPNX.Lib`: `0.1.0-preview.20261106.3`
- `OPNX.UI.WPF`: `0.1.0-preview.20261107.1`

- `Microsoft.Web.WebView2`: `1.0.4258.31` — WebRtcViewer only; the WebView2 Runtime is a separate prerequisite.

OPNX package versions are managed centrally in [Directory.Build.props](Directory.Build.props). The WebView2 SDK version is pinned in the WebRtcViewer project.

## Evaluation And Code Reuse

- Personal learning, research, testing, and non-commercial evaluation are permitted.
- Internal non-production evaluation, proof of concept, and technical review by a commercial organization are also permitted under the license without separate written permission.
- Production use, customer-facing services, commercial/OEM integration, and redistribution require prior written permission.
- Permission to modify evaluation code does not grant permission to include it in a commercial product or redistribute modified copies.
- OPNX-owned code and dependencies have separate licenses. The repository's English license controls the final terms.

## Current SDK And Sample Versions

This repository provides seven runnable samples using the published package versions listed above. It does not demonstrate every feature in current Lib/UI source.

| Current SDK source capability | This sample repository |
| --- | --- |
| RTSP input, decoding, and MultiView | `RtspViewer` |
| Multi-channel playback timeline | `PlaybackTimeline` |
| TCP / Named Pipe / SharedMemory messaging | `ProtocolChat` |
| In-memory EntityStore CRUD | `EntityStore` using the pinned package API |
| WebRTC media and Kestrel signaling | `WebRtcViewer`: H.264 video and local browser signaling |
| TCP file transfer and SharedMemory raw buffers | No dedicated runnable sample |
| Notifications, tray, and date/time input | `ControlGallery` |
| Remux and subtitles | No dedicated runnable sample |

The EntityStore sample uses `EntityColumn`, explicit entity/key types, and `FindCurrent<TEntity, TKey>`. It prints changed properties and original/current values, demonstrates direct property notifications, and shows that updates preserve the tracked object. In-memory inserts assign IDs explicitly. A direct property notification may not contain an original value; the output marks it as unavailable.

The selected packages use `FFmpeg.AutoGen 9.0.1.1`; supply compatible native FFmpeg libraries for the process architecture. The aggregate Lib package requires the ASP.NET Core 10 shared framework and brings DataChannelDotnet native assets. These dependencies also apply when a particular sample does not exercise WebRTC.

## License

OPNX Samples is source-available, but it is not licensed as permissive open-source software.

The OPNX-owned sample code may be used for learning, evaluation, research, testing, and other non-commercial purposes. Commercial use, redistribution, OEM integration, or inclusion in commercial products or services requires prior written permission from OPNX.

Publishing these samples does not grant commercial or redistribution rights to OPNX.Lib, OPNX.UI, FFmpeg, or any other dependency. Each component remains subject to its own license.

See [LICENSE.txt](LICENSE.txt) for full terms. A Korean reference translation is available at [LICENSE.ko.txt](LICENSE.ko.txt). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for dependency notices.

## Related Projects

- [OPNX.Lib](https://github.com/OPNXLabs/opnx-lib)
- [OPNX.UI](https://github.com/OPNXLabs/opnx-ui)

## Commercial And OEM Inquiries

OPNX Samples is developed and distributed by 오픈엑스 (OPNX), a business registered in the Republic of Korea.

- [https://www.opnx.kr/](https://www.opnx.kr/)
- `opnx@opnx.kr`

## Security And Contributions

- Report security issues privately as described in [SECURITY.md](https://github.com/OPNXLabs/opnx-samples/blob/master/SECURITY.md).
- Review [CONTRIBUTING.md](https://github.com/OPNXLabs/opnx-samples/blob/master/CONTRIBUTING.md) before opening an issue or proposing a change.

## AI Development Assistance

These samples were developed with assistance from AI coding tools. Builds and selected runtime scenarios have been verified, but behavior may vary across environments. Review and test the samples in your target environment before use.
