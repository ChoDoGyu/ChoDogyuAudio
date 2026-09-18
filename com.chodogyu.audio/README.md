# ChoDogyu Audio Framework

Unity 프로젝트에서 BGM과 SFX의 재생, 전환, Volume 및 Mute 상태를 일관된 방식으로 관리하기 위한 범용 Audio Framework입니다.

게임에서 어떤 음악과 효과음을 언제 재생할지는 사용하는 프로젝트가 결정하고, Framework는 AudioSource 생성 및 재사용, BGM 전환, SFX 동시 재생, Volume 계산, Mute 상태와 Audio Catalog 조회를 담당합니다.

특정 게임이나 장르에 종속되지 않으며 독립적인 Unity Package Manager 패키지로 구성했습니다.

---

## 주요 기능

### BGM

- `AudioClip` 직접 재생
- `AudioId` / `AudioCatalog` 기반 재생
- Loop 재생
- 즉시 교체
- Fade In
- Fade Out
- Cross Fade
- Pause / Resume
- 동일 BGM 재생 요청에 대한 안전한 처리
- 현재 BGM 및 재생 상태 조회

### SFX

- `AudioClip` 직접 재생
- `AudioId` / `AudioCatalog` 기반 재생
- 동일 SFX 중첩 재생
- 고정된 AudioSource Voice 재사용
- Voice 수 제한
- Overflow 정책
  - `RejectNew`
  - `StopOldest`
- 전체 Pause / Resume / Stop
- 현재 사용 중인 Voice 수 조회

### Volume / Mute

다음 세 가지 Volume을 독립적으로 관리합니다.

```text
Master
BGM
SFX
```

다음 Mute 상태를 독립적으로 관리합니다.

```text
Master Mute
BGM Mute
SFX Mute
```

Volume과 Mute 상태 변경은 현재 재생 중인 BGM과 SFX에 즉시 반영됩니다.

### Audio Catalog

- `AudioId` 기반 Audio Entry 식별
- BGM / SFX Category 구분
- AudioClip 등록
- Entry별 Volume Scale
- 중복 ID 검사
- 잘못된 Entry Validation
- Custom Inspector 기반 Validation 표시

### Sample

`Basic Usage` Sample을 제공합니다.

Sample에는 다음 항목이 포함되어 있습니다.

```text
실행 가능한 Sample Scene
AudioCatalog
BGM AudioClip 2개
SFX AudioClip 1개
Basic Usage Script
```

별도의 Audio Asset 준비 없이 Framework의 주요 기능을 바로 확인할 수 있습니다.

---

## 요구 사항

- Unity 6.3 이상
- ChoDogyu Core 1.0.0

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

Audio Framework는 `CDG.Core.Results`의 `Result`, `Result<T>`, `ResultError`를 사용하므로 ChoDogyu Core가 필요합니다.

다음 CDG 패키지는 필수 의존성이 아닙니다.

```text
ChoDogyu Object Pooling
ChoDogyu Data Framework
ChoDogyu Save / Load Framework
ChoDogyu UI Framework
ChoDogyu General Editor Tools
```

Audio Framework는 자체적으로 필요한 AudioSource를 고정 생성하여 재사용하므로 Object Pooling 패키지를 요구하지 않습니다.

---

## 설치

ChoDogyu Core를 먼저 설치한 뒤 Audio Framework를 설치합니다.

### 1. ChoDogyu Core 설치

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

### 2. ChoDogyu Audio Framework 설치

```text
https://github.com/ChoDoGyu/ChoDogyuAudio.git?path=/com.chodogyu.audio#v1.0.0
```

Unity에서:

```text
Window
→ Package Management
→ Package Manager
→ Install package from git URL...
```

Core를 먼저 설치한 뒤 Audio Framework를 설치합니다.

---

## 기본 구조

Runtime의 중심 구조:

```text
AudioController
├─ AudioVolumeState
├─ BgmPlayer
│  └─ AudioSource A / B
└─ SfxPlayer
   └─ SfxVoice × N
```

외부에서는 내부 Player나 Voice를 직접 조작하지 않고 `AudioController`를 Runtime 진입점으로 사용합니다.

`AudioController`는 초기화 시 자신이 소유하는 자식 GameObject를 생성합니다.

```text
AudioController GameObject
└─ Audio Runtime Sources
   ├─ BGM AudioSource A
   ├─ BGM AudioSource B
   └─ SFX AudioSource × Voice Count
```

기본 SFX Voice Count는 `16`입니다.

Controller가 제거되면 Framework가 생성한 `Audio Runtime Sources`도 함께 정리됩니다.

---

## AudioController

`AudioController`는 Audio Framework의 주요 Runtime API를 제공합니다.

GameObject에 Component를 추가하여 사용합니다.

```csharp
using CDG.Audio;
using UnityEngine;

public sealed class GameAudioExample : MonoBehaviour
{
    [SerializeField]
    private AudioController audioController;

    [SerializeField]
    private AudioClip bgmClip;

    [SerializeField]
    private AudioClip sfxClip;

    private void Start()
    {
        audioController.PlayBgm(bgmClip, 1f);
        audioController.PlaySfx(sfxClip);
    }
}
```

---

## Direct AudioClip 재생

AudioCatalog를 사용하지 않고 `AudioClip`을 직접 전달할 수 있습니다.

### BGM

```csharp
audioController.PlayBgm(bgmClip);
```

Fade In을 적용하려면 Duration을 전달합니다.

```csharp
audioController.PlayBgm(bgmClip, 1f);
```

### SFX

```csharp
audioController.PlaySfx(sfxClip);
```

재생 요청별 Volume Scale도 지정할 수 있습니다.

```csharp
audioController.PlaySfx(sfxClip, 0.5f);
```

Direct AudioClip 재생에는 AudioCatalog가 필요하지 않습니다.

---

## AudioId

`AudioId`는 AudioCatalog의 Entry를 식별하는 문자열 기반 값 형식입니다.

```csharp
AudioId id = new AudioId("bgm.field");
```

빈 문자열 또는 공백만 포함된 문자열은 유효한 ID로 취급하지 않습니다.

ID 생성 시 다음 처리는 자동으로 수행하지 않습니다.

```text
Trim
대소문자 변경
문자열 정규화
```

따라서 다음 두 ID는 서로 다른 ID입니다.

```text
bgm.field
BGM.FIELD
```

비교에는 `StringComparison.Ordinal` 기준이 사용됩니다.

---

## AudioCategory

Audio Entry는 다음 두 Category 중 하나를 사용합니다.

```text
Bgm
Sfx
```

BGM Entry를 SFX API로 재생하거나 SFX Entry를 BGM API로 재생하면 Category Mismatch 오류를 반환합니다.

---

## AudioCatalog

AudioCatalog는 Project Window에서 생성할 수 있습니다.

```text
Create
→ CDG
→ Audio
→ Audio Catalog
```

각 Entry는 다음 정보를 가집니다.

```text
AudioId
AudioCategory
AudioClip
Volume Scale
```

예:

```text
ID              Category    Clip        Volume Scale
bgm.field       Bgm         FieldBgm    1.0
bgm.boss        Bgm         BossBgm     0.8
sfx.attack      Sfx         Attack      1.0
```

생성한 AudioCatalog를 `AudioController`의 `Catalog` 필드에 지정합니다.

---

## AudioId 기반 BGM 재생

```csharp
AudioId bgmId = new AudioId("bgm.field");
audioController.PlayBgm(bgmId);
```

Fade Duration을 함께 전달할 수 있습니다.

```csharp
audioController.PlayBgm(bgmId, 1f);
```

Catalog Entry의 Volume Scale은 BGM 출력 계산에 함께 적용됩니다.

---

## AudioId 기반 SFX 재생

```csharp
AudioId sfxId = new AudioId("sfx.attack");
audioController.PlaySfx(sfxId);
```

요청 Volume Scale을 추가로 전달할 수 있습니다.

```csharp
audioController.PlaySfx(sfxId, 0.5f);
```

Catalog Entry Volume Scale과 요청 Volume Scale은 서로 독립적으로 적용됩니다.

---

## BGM Cross Fade

Audio Framework는 두 개의 BGM AudioSource를 사용하여 Cross Fade를 처리합니다.

현재 BGM이 재생 중인 상태에서 다른 BGM을 양수의 Fade Duration으로 재생하면 Cross Fade가 시작됩니다.

```csharp
audioController.PlayBgm(fieldBgm);
audioController.PlayBgm(bossBgm, 2f);
```

전환 과정:

```text
Field BGM Gain
1 → 0

Boss BGM Gain
0 → 1
```

Transition 중 새로운 BGM 요청이 들어오면 가장 최근 요청이 우선됩니다.

---

## BGM Fade Out

현재 BGM을 Fade Out 후 중지할 수 있습니다.

```csharp
audioController.StopBgm(1f);
```

Duration이 `0`이면 즉시 중지합니다.

```csharp
audioController.StopBgm();
```

---

## BGM Pause / Resume

```csharp
audioController.PauseBgm();
audioController.ResumeBgm();
```

Pause는 현재 재생 위치를 유지합니다.

---

## 동일 BGM 재생 요청

현재 관리 중인 BGM과 동일한 AudioClip을 다시 요청하면 불필요하게 처음부터 재생하지 않습니다.

Fade Out 중 동일 BGM이 다시 요청되면 진행 중인 Fade Out을 취소하고 현재 BGM을 유지합니다.

---

## SFX Voice

SFX는 재생할 때마다 새로운 GameObject나 AudioSource를 생성하지 않습니다.

`AudioController` 초기화 시 고정된 수의 AudioSource를 생성하고 `SfxVoice`로 재사용합니다.

기본 Voice Count:

```text
16
```

Inspector에서 `Sfx Voice Count`를 변경하여 프로젝트에 맞게 설정할 수 있습니다.

---

## SFX 동시 재생

여러 SFX를 연속으로 요청하면 사용 가능한 Voice를 순서대로 사용합니다.

```csharp
audioController.PlaySfx(hitClip);
audioController.PlaySfx(explosionClip);
audioController.PlaySfx(clickClip);
```

동일한 AudioClip도 여러 Voice에서 동시에 재생할 수 있습니다.

---

## SFX Overflow

모든 SFX Voice가 사용 중인 상태에서 새로운 SFX 요청이 들어왔을 때의 정책을 선택할 수 있습니다.

### StopOldest

가장 오래 사용 중인 Voice를 중지하고 새로운 SFX를 재생합니다.

`AudioController`의 기본 정책입니다.

### RejectNew

현재 Voice를 유지하고 새로운 SFX 요청을 실패로 반환합니다.

---

## Volume 계산

BGM의 최종 Volume은 다음 요소를 기준으로 계산됩니다.

```text
Master Volume
× BGM Volume
× Audio Entry Volume Scale
× Fade Gain
```

Direct AudioClip BGM은 Entry Volume Scale 대신 `1`을 사용합니다.

SFX의 최종 Volume은 다음 요소를 기준으로 계산됩니다.

```text
Master Volume
× SFX Volume
× Audio Entry Volume Scale
× Play Volume Scale
```

Direct AudioClip SFX는 Entry Volume Scale 대신 `1`을 사용합니다.

---

## Volume 설정

### Master

```csharp
audioController.SetMasterVolume(0.8f);
```

### BGM

```csharp
audioController.SetBgmVolume(0.6f);
```

### SFX

```csharp
audioController.SetSfxVolume(0.7f);
```

Volume 값은 `0`에서 `1` 범위로 처리됩니다.

---

## Mute

Mute는 Volume 값을 덮어쓰지 않고 별도의 상태로 관리됩니다.

### Master Mute

```csharp
audioController.SetMasterMuted(true);
```

### BGM Mute

```csharp
audioController.SetBgmMuted(true);
```

### SFX Mute

```csharp
audioController.SetSfxMuted(true);
```

Mute를 해제하면 기존 Volume 설정이 다시 적용됩니다.

---

## 현재 상태 조회

다음 Runtime 상태를 조회할 수 있습니다.

```csharp
float masterVolume = audioController.MasterVolume;
float bgmVolume = audioController.BgmVolume;
float sfxVolume = audioController.SfxVolume;

bool masterMuted = audioController.IsMasterMuted;
bool bgmMuted = audioController.IsBgmMuted;
bool sfxMuted = audioController.IsSfxMuted;

AudioClip currentBgm = audioController.CurrentBgmClip;
bool isPlaying = audioController.IsBgmPlaying;
bool isPaused = audioController.IsBgmPaused;
bool isFading = audioController.IsBgmFading;

int voiceCount = audioController.SfxVoiceCount;
int activeVoiceCount = audioController.ActiveSfxVoiceCount;
```

---

## 전체 정지

현재 BGM과 모든 SFX를 즉시 중지합니다.

```csharp
audioController.StopAll();
```

SFX만 중지하려면:

```csharp
audioController.StopAllSfx();
```

---

## SFX Pause / Resume

모든 활성 SFX를 일괄 제어할 수 있습니다.

```csharp
audioController.PauseAllSfx();
audioController.ResumeAllSfx();
```

---

## Audio Catalog Validation

AudioCatalog를 선택하면 Custom Inspector에서 Catalog 유효성을 검사합니다.

다음과 같은 잘못된 데이터를 확인할 수 있습니다.

```text
비어 있거나 공백만 있는 AudioId
중복 AudioId
null AudioClip
유효하지 않은 AudioCategory
0~1 범위를 벗어난 Volume Scale
NaN 또는 Infinity Volume Scale
null Entry
```

Catalog에 여러 문제가 있으면 가능한 항목을 한 번에 수집하여 Inspector에 표시합니다.

별도의 EditorWindow는 사용하지 않습니다.

Audio Catalog의 편집 규모에서는 Inspector 기반 Validation이 더 단순하고 직접적인 작업 흐름을 제공하도록 구성했습니다.

---

## Result 기반 오류 처리

재생이나 Catalog 조회가 실패할 수 있는 API는 ChoDogyu Core의 `Result` 또는 `Result<T>`를 사용합니다.

```csharp
Result result = audioController.PlayBgm(new AudioId("bgm.field"));

if (result.IsFailure)
{
    Debug.LogError($"{result.Error.Code}: {result.Error.Message}");
}
```

주요 오류 코드:

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

---

## Runtime Source 생명주기

Audio Framework가 사용하는 AudioSource는 `AudioController`가 소유합니다.

초기화 시:

```text
Audio Runtime Sources 생성
→ BGM AudioSource 2개 생성
→ 설정된 수만큼 SFX AudioSource 생성
```

`AudioController`가 제거되면:

```text
BGM 정지
→ SFX 정지
→ Audio Runtime Sources 제거
→ 내부 Runtime 참조 정리
```

따라서 `AudioController` Component를 제거한 뒤 다시 추가해도 중복 Runtime Source가 남지 않습니다.

---

## Scene 생명주기

Audio Framework는 다음 동작을 자동으로 수행하지 않습니다.

```text
DontDestroyOnLoad
Scene 이벤트 구독
Scene별 BGM 자동 변경
Scene 전환 시 자동 Stop
```

AudioController의 생명주기는 사용하는 프로젝트가 결정합니다.

여러 Scene에서 Audio를 유지해야 하는 프로젝트라면 Composition Root 등 프로젝트의 상위 계층에서 `DontDestroyOnLoad` 정책을 적용할 수 있습니다.

Framework 자체는 특정 Scene 구조를 강제하지 않습니다.

---

## 저장

Audio Framework는 Volume 또는 Mute 설정을 자동 저장하지 않습니다.

설정 저장이 필요한 프로젝트는 ChoDogyu Save / Load Framework 또는 프로젝트 자체 저장 시스템을 사용하여 값을 저장하고, 로드 후 다음 API를 호출합니다.

```csharp
audioController.SetMasterVolume(savedMasterVolume);
audioController.SetBgmVolume(savedBgmVolume);
audioController.SetSfxVolume(savedSfxVolume);

audioController.SetMasterMuted(savedMasterMuted);
audioController.SetBgmMuted(savedBgmMuted);
audioController.SetSfxMuted(savedSfxMuted);
```

Audio Framework와 Save Framework 사이에는 직접적인 패키지 의존성이 없습니다.

---

## Basic Usage Sample

Unity Package Manager에서 Audio Framework를 선택하면 다음 Sample을 Import할 수 있습니다.

```text
Basic Usage
```

Import 후:

```text
Assets
└─ Samples
   └─ ChoDogyu Audio Framework
      └─ 1.0.0
         └─ Basic Usage
```

`Scenes/BasicUsage.unity`를 열고 Play Mode에 진입하면 바로 실행할 수 있습니다.

Sample에서는 다음 기능을 직접 확인할 수 있습니다.

```text
Direct BGM
Direct SFX
AudioId BGM
AudioId SFX
Cross Fade
Fade Stop
Pause / Resume
Volume
Mute
Runtime State
SFX 동시 재생
```

---

## v1.0 범위에서 제외한 기능

다음 기능은 v1.0의 책임 범위에 포함하지 않습니다.

```text
AudioMixer Parameter 자동 관리
AudioMixer Snapshot
3D Spatial Audio 관리
Follow Target Audio
개별 SFX Handle
Looping SFX 관리
Random Pitch
Random Audio Group
BGM Playlist
Audio Ducking
Dialogue Audio
Addressables 자동 연동
Streaming Audio 관리
Scene별 Audio Mapping
Volume 설정 자동 저장
UI Slider 자동 연결
자동 DontDestroyOnLoad
```

필요한 기능은 프로젝트 계층에서 조합하거나 이후 버전에서 독립적인 필요성이 확인되었을 때 확장할 수 있습니다.

---

## 설계 방향

Audio Framework는 다음 책임 분리를 기준으로 설계했습니다.

```text
게임 / 프로젝트
→ 어떤 Audio를 언제 재생할지 결정
→ AudioController의 생명주기 결정
→ Save / Scene / UI 연동 결정

Audio Framework
→ AudioClip 재생 방법 관리
→ BGM 전환 관리
→ SFX Voice 재사용 관리
→ Volume / Mute 계산
→ AudioId / Catalog 조회
→ Catalog Validation
```

게임별 규칙을 Framework 내부로 끌어들이지 않고 여러 프로젝트에서 재사용 가능한 Audio Runtime 기반을 제공하는 것을 목표로 합니다.

---

## Assembly

Runtime:

```text
CDG.Audio
```

Editor:

```text
CDG.Audio.Editor
```

Runtime Test:

```text
CDG.Audio.Tests.Runtime
```

Editor Test:

```text
CDG.Audio.Tests.Editor
```

Sample:

```text
CDG.Audio.Samples.BasicUsage
```

Namespace:

```text
CDG.Audio
CDG.Audio.Editor
CDG.Audio.Samples.BasicUsage
```

---

## 패키지 정보

```text
Package Name : com.chodogyu.audio
Version      : 1.0.0
Unity        : 6000.3+
Author       : ChoDogyu
```