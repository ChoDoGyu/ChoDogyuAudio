# ChoDogyu Audio Framework

Unity 프로젝트에서 BGM과 SFX의 재생, 전환, 동시 재생, Volume 및 Mute 상태를 일관된 방식으로 관리하기 위한 범용 Audio Framework입니다.

게임에서 어떤 Audio를 언제 재생할지는 사용하는 프로젝트가 결정하고, Framework는 AudioSource 관리, BGM Fade / Cross Fade, SFX Voice 재사용, Overflow 처리, Volume / Mute 계산 및 Audio Catalog 조회를 담당합니다.

특정 게임이나 장르에 종속되지 않으며 Unity Package Manager를 통해 독립적으로 설치할 수 있도록 구성했습니다.

---

## 주요 기능

### BGM

- `AudioClip` 직접 재생
- `AudioId` / `AudioCatalog` 기반 재생
- Loop
- 즉시 교체
- Fade In
- Fade Out
- Cross Fade
- Pause / Resume
- 동일 BGM 재생 요청 처리
- 현재 BGM 상태 조회

### SFX

- `AudioClip` 직접 재생
- `AudioId` / `AudioCatalog` 기반 재생
- 동일 SFX 중첩 재생
- 고정 AudioSource Voice 재사용
- 설정 가능한 Voice Count
- Overflow 정책
  - `RejectNew`
  - `StopOldest`
- 전체 Stop / Pause / Resume
- 활성 Voice 상태 조회

### Volume / Mute

- Master Volume
- BGM Volume
- SFX Volume
- Master Mute
- BGM Mute
- SFX Mute
- 재생 중인 Audio에 설정 변경 즉시 반영

### Audio Catalog

- `AudioId` 기반 Audio Entry 식별
- BGM / SFX Category 구분
- AudioClip 등록
- Entry별 Volume Scale
- 중복 ID Validation
- 잘못된 Entry Validation
- Custom Inspector 기반 Validation 결과 표시

### Sample

Package Manager용 `Basic Usage` Sample을 제공합니다.

```text
Sample Scene
AudioCatalog
BGM AudioClip 2개
SFX AudioClip 1개
Basic Usage Script
```

Import 후 별도의 Audio Asset 준비 없이 주요 기능을 바로 실행할 수 있습니다.

---

## 핵심 구조

```text
AudioController
├─ AudioVolumeState
├─ BgmPlayer
│  ├─ AudioSource A
│  └─ AudioSource B
└─ SfxPlayer
   └─ SfxVoice × N
```

외부에서는 `AudioController`를 Runtime Audio 흐름의 진입점으로 사용합니다.

내부의 BGM Player, SFX Player, Voice 및 Volume State는 외부 사용 코드에서 직접 관리하지 않습니다.

---

## BGM

BGM은 두 개의 AudioSource를 사용하여 Fade 및 Cross Fade를 처리합니다.

```text
BGM A
Gain 1 → 0

BGM B
Gain 0 → 1
```

전환 도중 새로운 요청이 들어오면 가장 최근 요청을 우선합니다.

동일한 BGM을 다시 요청하면 불필요하게 처음부터 재생하지 않습니다.

---

## SFX

SFX를 재생할 때마다 AudioSource를 새로 생성하지 않습니다.

초기화 시 설정된 수의 AudioSource를 생성하고 반복해서 재사용합니다.

기본 Voice Count:

```text
16
```

여러 SFX 또는 동일 SFX를 동시에 재생할 수 있습니다.

모든 Voice가 사용 중일 때는 다음 Overflow 정책을 사용할 수 있습니다.

```text
StopOldest
RejectNew
```

기본값:

```text
StopOldest
```

---

## Volume 계산

BGM:

```text
Master Volume
× BGM Volume
× Entry Volume Scale
× Fade Gain
```

SFX:

```text
Master Volume
× SFX Volume
× Entry Volume Scale
× Play Volume Scale
```

Mute 상태는 Volume 값을 변경하지 않고 별도로 관리합니다.

따라서 Mute를 해제하면 기존 Volume 설정으로 복원됩니다.

---

## AudioId

AudioCatalog에서 Audio Entry를 식별하기 위한 문자열 기반 값 형식입니다.

```csharp
AudioId id = new AudioId("bgm.field");
```

다음 문자열 처리는 자동으로 수행하지 않습니다.

```text
Trim
대소문자 변경
문자열 정규화
```

비교는 Ordinal 기준입니다.

빈 문자열 또는 공백만 포함된 문자열은 유효하지 않은 ID로 처리합니다.

---

## AudioCatalog

Project Window에서 생성할 수 있습니다.

```text
Create
→ CDG
→ Audio
→ Audio Catalog
```

각 Entry:

```text
AudioId
AudioCategory
AudioClip
Volume Scale
```

예:

```text
bgm.field    Bgm    FieldBgm    1.0
bgm.boss     Bgm    BossBgm     0.8
sfx.attack   Sfx    Attack      1.0
```

---

## Catalog Validation

AudioCatalog Custom Inspector에서 다음 문제를 검사합니다.

```text
빈 AudioId
공백 AudioId
중복 AudioId
Null AudioClip
유효하지 않은 AudioCategory
잘못된 Volume Scale
NaN
Infinity
Null Entry
```

발견된 문제는 가능한 범위에서 한 번에 표시합니다.

v1.0에서는 별도의 대형 EditorWindow를 사용하지 않고 Catalog 편집과 Validation을 Inspector 안에서 완료하도록 구성했습니다.

---

## 저장소 구조

```text
ChoDogyuAudio/
├─ AudioDevelopment/
│  └─ 패키지 개발 및 검증용 Unity 프로젝트
│
├─ com.chodogyu.audio/
│  ├─ Runtime/
│  ├─ Editor/
│  ├─ Tests/
│  │  ├─ Runtime/
│  │  └─ Editor/
│  ├─ Samples~/
│  │  └─ BasicUsage/
│  ├─ Documentation~/
│  │  └─ index.md
│  ├─ package.json
│  ├─ README.md
│  └─ CHANGELOG.md
│
├─ .gitattributes
├─ .gitignore
└─ README.md
```

### AudioDevelopment

Audio Framework 개발, 테스트 및 통합 검증에 사용하는 Unity 프로젝트입니다.

실제 UPM 배포 대상에는 포함되지 않습니다.

### com.chodogyu.audio

실제 Unity Package Manager 패키지입니다.

다른 Unity 프로젝트에서는 이 폴더를 Git UPM 패키지로 설치합니다.

---

## 요구 사항

- Unity 6.3 이상
- ChoDogyu Core 1.0.0

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

Audio Framework는 ChoDogyu Core의 Result 계열 타입을 사용합니다.

다음 패키지는 필수 의존성이 아닙니다.

```text
ChoDogyu General Editor Tools
ChoDogyu Object Pooling
ChoDogyu Data Framework
ChoDogyu Save / Load Framework
ChoDogyu UI Framework
```

---

## 설치

### 1. ChoDogyu Core

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

### 2. ChoDogyu Audio Framework

```text
https://github.com/ChoDoGyu/ChoDogyuAudio.git?path=/com.chodogyu.audio#v1.0.0
```

Unity:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

Core를 먼저 설치한 뒤 Audio Framework를 설치합니다.

---

## 기본 사용

### Direct BGM

```csharp
audioController.PlayBgm(bgmClip);
```

Fade:

```csharp
audioController.PlayBgm(bgmClip, 1f);
```

### Direct SFX

```csharp
audioController.PlaySfx(sfxClip);
```

### AudioId BGM

```csharp
audioController.PlayBgm(new AudioId("bgm.field"));
```

### AudioId SFX

```csharp
audioController.PlaySfx(new AudioId("sfx.attack"));
```

---

## BGM 제어

```csharp
audioController.StopBgm();
audioController.StopBgm(1f);

audioController.PauseBgm();
audioController.ResumeBgm();
```

---

## SFX 제어

```csharp
audioController.StopAllSfx();
audioController.PauseAllSfx();
audioController.ResumeAllSfx();
```

---

## Volume

```csharp
audioController.SetMasterVolume(0.8f);
audioController.SetBgmVolume(0.6f);
audioController.SetSfxVolume(0.7f);
```

---

## Mute

```csharp
audioController.SetMasterMuted(true);
audioController.SetBgmMuted(true);
audioController.SetSfxMuted(true);
```

---

## Basic Usage Sample

Package Manager에서:

```text
ChoDogyu Audio Framework
→ Samples
→ Basic Usage
→ Import
```

Sample Scene:

```text
BasicUsage/Scenes/BasicUsage.unity
```

확인 가능한 기능:

```text
Direct BGM
Direct SFX
AudioId BGM
AudioId SFX
Cross Fade
Fade Stop
Pause / Resume
SFX 동시 재생
Volume
Mute
Runtime State
```

---

## Runtime Source 생명주기

AudioController가 초기화되면:

```text
Audio Runtime Sources
├─ BGM AudioSource A
├─ BGM AudioSource B
└─ SFX AudioSource × N
```

을 생성합니다.

AudioController Component가 제거되면 Framework가 소유한 Runtime Source도 함께 정리합니다.

따라서 Component를 제거한 뒤 다시 추가해도 중복된 Runtime Source가 남지 않습니다.

---

## 책임 범위

Framework가 담당:

```text
BGM 재생
Fade In / Fade Out
Cross Fade
SFX 재생
SFX 동시 재생
SFX Voice 재사용
SFX Overflow
Volume
Mute
AudioId
AudioCatalog
Catalog Validation
Result 기반 오류 처리
Runtime AudioSource 생명주기
```

Framework가 담당하지 않음:

```text
게임별 Audio 재생 규칙
AudioMixer 자동 관리
3D Spatial Audio Framework
BGM Playlist
Audio Ducking
Dialogue System
Addressables 자동 연동
Scene별 BGM Mapping
Scene 이벤트 자동 처리
Volume / Mute 자동 저장
UI Slider 자동 연결
Runtime Singleton
Service Locator
자동 DontDestroyOnLoad
```

---

## Runtime Error Codes

```text
AUDIO_INVALID_ID
AUDIO_INVALID_CLIP
AUDIO_CATALOG_NOT_ASSIGNED
AUDIO_NOT_FOUND
AUDIO_CATEGORY_MISMATCH
AUDIO_DUPLICATE_ID
AUDIO_INVALID_CATALOG
AUDIO_NO_AVAILABLE_SFX_VOICE
```

오류 메시지가 아닌 `Error.Code`를 기준으로 실패 원인을 구분할 수 있습니다.

---

## UPM 독립 설치 검증

완전히 새로운 Unity 6.3 프로젝트에서 실제 Git UPM 흐름을 검증했습니다.

```text
ChoDogyu Core v1.0.0 설치
→ 성공

ChoDogyu Audio Framework 설치
→ 성공

Runtime / Editor Compile
→ 성공

Basic Usage Sample Import
→ 성공

Sample Scene 실행
→ 성공

Direct BGM / SFX
→ 정상

AudioId BGM / SFX
→ 정상

Cross Fade
→ 정상

Volume / Mute
→ 정상

Package Remove
→ 성공

Core 유지
→ 확인

Audio Framework 재설치
→ 성공

Sample 재Import 및 실행
→ 성공
```

개발 프로젝트 또는 Local Package 경로에 의존하지 않는 독립 설치를 확인했습니다.

---

## 다른 CDG 패키지와의 관계

```text
Core
→ 공통 Result 기반

Pooling
→ Runtime Object 재사용

Data
→ 정적 데이터 및 Import

Save
→ 플레이 상태 저장

UI
→ 화면 흐름

Audio
→ BGM / SFX 재생 및 Audio 상태 관리
```

각 패키지는 자신의 책임을 독립적으로 유지합니다.

Audio는 Core 외 다른 CDG 패키지에 직접 의존하지 않습니다.

---

## 설계 방향

```text
게임이 재생 시점을 결정
Framework가 재생 방법을 관리

BGM과 SFX 책임 분리
고정 AudioSource 재사용
Volume과 Mute 분리
명시적인 Overflow 정책
Result 기반 오류 처리
내부 구현 캡슐화
최소 패키지 의존성
Scene / Save / UI 정책 비강제
독립 설치 가능한 UPM 구조
```

---

## 문서

패키지 사용법:

```text
com.chodogyu.audio/README.md
```

상세 설계 및 사용 규칙:

```text
com.chodogyu.audio/Documentation~/index.md
```

버전 변경 사항:

```text
com.chodogyu.audio/CHANGELOG.md
```

---

## 버전

현재 버전:

```text
v1.0.0
```

Package:

```text
com.chodogyu.audio
```

Runtime Assembly:

```text
CDG.Audio
```

Editor Assembly:

```text
CDG.Audio.Editor
```