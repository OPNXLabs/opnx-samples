# 기여 안내

[English](CONTRIBUTING.md)

> 이 문서는 영문 [CONTRIBUTING.md](CONTRIBUTING.md)의 이해를 돕기 위한 한국어 번역본입니다. 내용이 서로 다를 경우 영문 문서를 기준으로 합니다.

OPNX Samples는 평가와 기술 검토를 위한 source-available preview 소프트웨어로 공개됩니다.

이슈 제보, 문서 피드백 및 재현 가능한 샘플 문제 제보는 환영합니다. 보안 취약점을 제보하기 전에는 [SECURITY.ko.md](SECURITY.ko.md)를 확인하고 공개적으로 게시하지 마십시오.

OPNX가 기여 라이선스 정책을 확립하기 전까지 외부 Pull Request는 받지 않습니다. OPNX 관리자가 코드 변경을 요청하고 적용할 기여 조건을 서면으로 확인한 경우가 아니라면 코드 변경을 제출하지 마십시오.

기밀정보, 고객 데이터, 인증정보, 카메라 URL, 비공개 영상 또는 공유할 권한이 없는 자료를 포함하지 마십시오.

상업 라이선스 및 파트너십 문의: `opnx@opnx.kr`

## 문제 재현과 문서 피드백

- 샘플 이름, 저장소 커밋, 구성(Debug/Release), 플랫폼(AnyCPU/x64), Windows 및 .NET 버전, 복원된 OPNX 패키지 버전을 기록합니다.
- UI 문제는 테마·강조색, 창 크기와 화면 배율, 조작 순서 및 예상/실제 동작을 포함합니다. 현재 샘플 이름은 `RtspViewer`이며 이전 `RtspMultiLiveViewer` 경로는 사용하지 않습니다.
- RTSP 문제는 코덱·전송 방식과 FFmpeg 초기화 상태를, WebRTC 문제는 WebView2 Runtime/브라우저 버전과 시그널링·ICE·수신 프레임 상태를 포함합니다. 실제 주소·비밀번호·토큰은 제거하고 재현 가능한 대체 정보를 사용합니다.
- 모니터링의 `N/A`는 수집 불가일 수 있으므로 OS·GPU·드라이버와 수집 시점을 함께 기록합니다. Timeline은 모의 데이터 UI이며 실제 영상 재생 오류와 구분합니다.
- 문서 수정 시 한영 README, 실행 명령, 샘플 수, 의존성 고지와 상대 링크를 함께 확인합니다. UI 이미지는 현재 코드와 일치해야 하며 [이미지 안내](docs/images/README.md)를 따릅니다.
