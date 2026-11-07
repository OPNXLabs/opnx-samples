# Contributing

[Korean](CONTRIBUTING.ko.md)

OPNX Samples is published as source-available preview software for evaluation and technical review.

Issue reports, documentation feedback, and reproducible sample problems are welcome. Before reporting a security vulnerability, follow [SECURITY.md](SECURITY.md) and do not disclose it publicly.

External pull requests are not accepted until OPNX establishes a contribution licensing policy. Do not submit code changes unless an OPNX maintainer has requested them and confirmed the applicable contribution terms in writing.

Do not include confidential information, customer data, credentials, camera URLs, private media, or material you do not have the right to share.

Commercial licensing and partnership inquiries: `opnx@opnx.kr`

## Reproduction And Documentation Feedback

- Include the sample name, repository commit, configuration (Debug/Release), platform (AnyCPU/x64), Windows/.NET versions, and restored OPNX package versions.
- For UI reports, include theme/accent, window size and display scaling, steps, and expected/actual behavior. The current project is `RtspViewer`; do not use the former `RtspMultiLiveViewer` path.
- For RTSP reports, include codec/transport and FFmpeg initialization state. For WebRTC, include WebView2 Runtime/browser versions and signaling, ICE and received-frame state. Redact real endpoints, passwords and tokens; use safe substitutes.
- Monitoring `N/A` may mean unavailable data; include OS, GPU/driver and sampling timing. The timeline uses simulated data, so distinguish its UI behavior from actual media playback.
- Documentation changes should keep both READMEs, run commands, sample counts, dependency notices and relative links aligned. UI images must match the code; follow the [image notes](docs/images/README.md).
