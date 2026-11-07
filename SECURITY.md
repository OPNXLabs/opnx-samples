# Security Policy

[Korean](SECURITY.ko.md)

OPNX Samples is preview software under active development. Only the latest published state is considered for security fixes.

Do not report suspected vulnerabilities through public GitHub issues, discussions, or pull requests. Report them privately to `opnx@opnx.kr` with the affected sample and package versions, impact, reproducible steps, known mitigations, and a safe contact method.

Do not include credentials, personal information, customer data, camera passwords, RTSP credentials, or production secrets. Allow OPNX reasonable time to investigate before public disclosure.

Receipt of a report does not guarantee a fix, response time, bounty, compensation, or reward. Commercial support and response commitments require a separate written agreement.

## Sample Execution And Reporting Scope

`WebRtcViewer` is a local evaluation relay using an ephemeral port on `127.0.0.1` and a per-run player path. That path is not production authentication. Port forwarding and external binding are outside its design; external browsers run on the same machine. RTSP credentials stay in desktop memory, but other local players can receive video while the source is running, so do not share the player URL. Use `Stop` or close the application to end the relay.

Connect only to sources you are authorized to access. Captured PNGs, screenshots, logs, SDP and ICE details can contain private video, endpoints and network information. Redact these before reporting; never commit camera endpoints, accounts or passwords as sample or test data. The TCP/Named Pipe/SharedMemory chat examples demonstrate messaging, not production authentication or encryption configuration.

Keep the WebView2 Runtime, browser, .NET and selected FFmpeg native build updated through their respective providers. This repository's license does not guarantee the security maintenance or production suitability of those components.
