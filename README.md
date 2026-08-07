# JALib.Server
### Jongyeol Adofai Library Server
![C#](https://img.shields.io/badge/Lang-C%23-c9c8e4.svg?&logo=c#)
![.NET](https://img.shields.io/badge/Framework-.NET_10-c9c8e4.svg?&logo=dotnet)
![Rider](https://img.shields.io/badge/IDE-Rider-c9c8e4.svg?&logo=rider)

---

### 소개
* ASP.NET Core 기반의 **[JALib](https://github.com/Jongye0l/JALib)** 전용 백엔드 API 서버입니다.
* 기존 Spring Boot 기반 [JALib-Server](https://github.com/Jongye0l/JALib-Server)를 .NET으로 재작성한 프로젝트입니다.

---

### 주요 기능 및 특징
* **ASP.NET Core MVC 기반 웹 서버:** `JALibController`, `AdminController`, `ModApplicatorController` 등 Attribute-routed Controller를 통해 JALib 클라이언트 및 관련 도구들과 통신하기 위한 REST API 및 엔드포인트 제공
* **모드 관리 및 배포 지원:** 모드 버전/강제 업데이트 정보 조회, 다운로드, `ModApplicatorController` + Razor View(`Views/ModApplicator`)를 통한 Windows 등 플랫폼별 모드 자동 적용 안내 페이지 제공
* **바이너리/패킷 핸들링:** 자체 정의한 패킷 구조(`JavaByteArrayDataInput`, `JavaByteArrayDataOutput`)를 통해 기존 Java 서버와 동일한 포맷으로 데이터를 직렬화/역직렬화
* **다른 JALib 서버와의 연동:** `ConnectOtherLib`를 통해 원격 JALib 서버(`otherLibURL`)와 모드 데이터를 동기화
* **현지화(Localization):** 요청 헤더(`Accept-Language`) 기반 다국어(한국어/영어) 응답 지원
* **GZip 압축 지원:** `JavaByteArrayDataOutput`의 압축 옵션 및 `GZipFile` 헬퍼를 통한 네트워크 전송 데이터 압축/해제
* **Serilog 기반 로깅:** 콘솔 및 파일(일별 롤링, gzip 아카이빙) 로그 출력을 ASP.NET Core 호스트 로깅과 통합

---

### 기술 스택
* **Backend:** C# / ASP.NET Core (.NET 10)
* **Build Tool:** .NET SDK (`dotnet build` / `dotnet publish`)
* **View Engine:** Razor (`.cshtml`, 모드 자동 적용 안내 페이지)
* **Logging:** Serilog (Console, File)

---

### 설정
* 실행 파일과 같은 경로의 `settings.json`을 통해 로그 경로, 모드 데이터 경로, 토큰 경로, 연동 서버 주소(`otherLibURL`) 등을 설정합니다.
* 기본 포트는 `43910`입니다.

---

### 라이선스
이 프로젝트는 **BSD 3-Clause License**를 따릅니다. 자세한 내용은 [LICENSE](./LICENSE) 파일을 참고해 주세요.

---
# [Join My Discord!](https://discord.jongyeol.kr)
