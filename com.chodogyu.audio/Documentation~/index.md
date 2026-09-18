# ChoDogyu Audio Framework

ChoDogyu Audio Framework는 Unity 프로젝트에서 BGM과 SFX의 재생, 전환, 동시 재생, Volume, Mute 및 Audio Catalog를 일관된 방식으로 관리하기 위한 범용 Runtime Framework입니다.

Framework는 게임의 오디오 콘텐츠나 게임별 재생 규칙을 결정하지 않습니다.

```text
프로젝트
→ 어떤 Audio를 언제 재생할지 결정
→ AudioController의 생명주기 결정
→ Save / Scene / UI 연동 결정

Audio Framework
→ AudioClip 재생
→ BGM Fade / Cross Fade
→ SFX Voice 재사용
→ Overflow 처리
→ Volume / Mute 계산
→ AudioId / AudioCatalog 조회
→ Catalog Validation
```

특정 게임이나 장르에 종속되지 않는 독립적인 Unity Package Manager 패키지로 사용할 수 있도록 구성했습니다.

---

# 1. Package Information

```text
Package Name : com.chodogyu.audio
Display Name : ChoDogyu Audio Framework
Version      : 1.0.0
Unity        : 6000.3+
Author       : ChoDogyu
```

개발 및 검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

Assembly:

```text
Runtime      : CDG.Audio
Editor       : CDG.Audio.Editor
Runtime Test : CDG.Audio.Tests.Runtime
Editor Test  : CDG.Audio.Tests.Editor
Sample       : CDG.Audio.Samples.BasicUsage
```

Namespace:

```text
CDG.Audio
CDG.Audio.Editor
CDG.Audio.Samples.BasicUsage
```

---

# 2. Requirements

필수 패키지:

```text
ChoDogyu Core 1.0.0
```

Audio Framework는 ChoDogyu Core의 다음 타입을 사용합니다.

```text
Result
Result<T>
ResultError
```

Audio Framework의 `package.json`은 ChoDogyu Core의 Git URL을 직접 의존성으로 선언하지 않습니다.

따라서 Git UPM으로 사용할 때는 Core를 먼저 설치해야 합니다.

다음 CDG 패키지는 필수 의존성이 아닙니다.

```text
ChoDogyu General Editor Tools
ChoDogyu Object Pooling
ChoDogyu Data Framework
ChoDogyu Save / Load Framework
ChoDogyu UI Framework
```

다음 외부 시스템도 v1.0에서 요구하지 않습니다.

```text
AudioMixer
Addressables
DOTween
TextMeshPro
```

---

# 3. Installation

## 3.1 ChoDogyu Core 설치

Unity Package Manager에서 다음 Git URL을 사용합니다.

```text
https://github.com/ChoDoGyu/ChoDogyuCore.git?path=/com.chodogyu.core#v1.0.0
```

Unity에서:

```text
Window
→ Package Management
→ Package Manager
→ +
→ Install package from git URL...
```

Core 설치가 완료되고 Console Error가 없는 것을 확인합니다.

---

## 3.2 ChoDogyu Audio Framework 설치

Core 설치 후 다음 Git URL을 사용합니다.

```text
https://github.com/ChoDoGyu/ChoDogyuAudio.git?path=/com.chodogyu.audio#v1.0.0
```

설치 후 Package Manager에서 다음 정보가 표시되는지 확인합니다.

```text
ChoDogyu Audio Framework
1.0.0
```

---

# 4. Package Structure

패키지의 기본 구조는 다음과 같습니다.

```text
com.chodogyu.audio/
├─ Documentation~/
│  └─ index.md
├─ Editor/
├─ Runtime/
├─ Samples~/
│  └─ BasicUsage/
├─ Tests/
│  ├─ Editor/
│  └─ Runtime/
├─ CHANGELOG.md
├─ README.md
└─ package.json
```

각 영역의 책임은 다음과 같습니다.

### Runtime

게임 실행 중 사용하는 Audio Framework 기능을 포함합니다.

```text
AudioController
AudioId
AudioCategory
AudioEntry
AudioCatalog
AudioErrorCodes
BgmPlayer
SfxPlayer
SfxVoice
AudioVolumeState
SfxOverflowPolicy
```

### Editor

AudioCatalog의 Inspector Validation 기능을 포함합니다.

### Tests

Runtime과 Editor 동작을 자동 검증합니다.

### Samples~

Package Manager에서 Import 가능한 Basic Usage Sample을 포함합니다.

### Documentation~

상세 사용법과 설계 정책을 설명합니다.

---

# 5. Architecture

Audio Framework의 외부 Runtime 진입점은 `AudioController`입니다.

```text
AudioController
├─ AudioVolumeState
├─ BgmPlayer
│  ├─ BGM AudioSource A
│  └─ BGM AudioSource B
└─ SfxPlayer
   ├─ SfxVoice 0
   ├─ SfxVoice 1
   ├─ ...
   └─ SfxVoice N
```

외부 프로젝트는 내부 `BgmPlayer`, `SfxPlayer`, `SfxVoice`를 직접 조작하지 않습니다.

```text
Game Code
   ↓
AudioController
   ├─ BGM
   ├─ SFX
   ├─ Volume
   ├─ Mute
   └─ Catalog
```

이 구조를 통해 외부 API와 내부 AudioSource 관리 책임을 분리합니다.

---

# 6. AudioController Setup

Scene에 빈 GameObject를 생성합니다.

예:

```text
Game Audio
```

`AudioController` Component를 추가합니다.

```text
Game Audio
└─ AudioController
```

Play Mode에서 AudioController가 초기화되면 내부적으로 다음 자식 GameObject가 생성됩니다.

```text
Game Audio
├─ AudioController
└─ Audio Runtime Sources
```

`Audio Runtime Sources`에는 다음 AudioSource가 생성됩니다.

```text
BGM AudioSource A
BGM AudioSource B
SFX AudioSource × Sfx Voice Count
```

기본 SFX Voice Count는:

```text
16
```

입니다.

---

# 7. Why AudioSource Is Managed Internally

Audio Framework는 SFX를 재생할 때마다 새로운 GameObject 또는 AudioSource를 생성하지 않습니다.

초기화 시 필요한 AudioSource를 미리 구성하고 반복해서 재사용합니다.

```text
초기화
→ AudioSource 생성

SFX 재생 요청
→ 사용 가능한 Voice 찾기
→ 기존 AudioSource 재사용

SFX 종료
→ Voice 반환
→ 다음 재생에 재사용
```

따라서 v1.0에서는 ChoDogyu Object Pooling 패키지를 의존성으로 사용하지 않습니다.

AudioSource는 이미 매우 작은 고정 집합으로 관리되므로 Audio Framework 내부의 bounded voice 관리만으로 책임을 완결합니다.

---

# 8. Direct AudioClip Playback

AudioCatalog를 사용하지 않고 AudioClip을 직접 재생할 수 있습니다.

## 8.1 Direct BGM

```csharp
using CDG.Audio;
using UnityEngine;

public sealed class AudioExample : MonoBehaviour
{
    [SerializeField]
    private AudioController audioController;

    [SerializeField]
    private AudioClip bgmClip;

    private void Start()
    {
        audioController.PlayBgm(bgmClip);
    }
}
```

기본값:

```text
Fade Duration : 0
Loop          : true
```

Fade In 시간을 지정할 수 있습니다.

```csharp
audioController.PlayBgm(bgmClip, 1f);
```

Loop 여부도 지정할 수 있습니다.

```csharp
audioController.PlayBgm(bgmClip, 1f, false);
```

---

## 8.2 Direct SFX

```csharp
audioController.PlaySfx(sfxClip);
```

요청별 Volume Scale을 지정할 수도 있습니다.

```csharp
audioController.PlaySfx(sfxClip, 0.5f);
```

Direct AudioClip 재생에는 AudioCatalog가 필요하지 않습니다.

---

# 9. AudioId

`AudioId`는 AudioCatalog와 Runtime에서 Audio Entry를 식별하기 위한 값 형식입니다.

```csharp
AudioId id = new AudioId("bgm.field");
```

`Value`를 통해 문자열 값을 확인할 수 있습니다.

```csharp
string value = id.Value;
```

빈 ID 여부:

```csharp
bool isEmpty = id.IsEmpty;
```

다음 값은 유효하지 않은 ID입니다.

```text
""
" "
"    "
null
```

AudioId는 생성 시 문자열을 수정하지 않습니다.

다음 동작을 자동으로 수행하지 않습니다.

```text
Trim
ToLower
ToUpper
문자열 정규화
```

따라서 다음 ID는 서로 다릅니다.

```text
bgm.field
BGM.FIELD
bgm.field 
```

비교는 Ordinal 문자열 비교를 기준으로 합니다.

---

# 10. AudioCategory

Audio Entry는 다음 두 Category 중 하나를 사용합니다.

```csharp
AudioCategory.Bgm
AudioCategory.Sfx
```

Category는 단순한 분류 정보가 아니라 올바른 재생 API를 검증하는 데 사용됩니다.

예를 들어:

```text
AudioId : sfx.attack
Category: Sfx
```

인 Entry를 다음 API에 전달하면:

```csharp
audioController.PlayBgm(new AudioId("sfx.attack"));
```

실패 Result가 반환됩니다.

오류 코드:

```text
AUDIO_CATEGORY_MISMATCH
```

---

# 11. AudioEntry

AudioCatalog의 각 Entry는 다음 정보를 가집니다.

```text
Id
Category
Clip
Volume Scale
```

예:

```text
Id           : bgm.field
Category     : Bgm
Clip         : FieldBgm
Volume Scale : 0.8
```

`Volume Scale`은 개별 Audio Asset의 상대적인 출력 크기를 조정하기 위한 값입니다.

예를 들어 특정 BGM 원본 음원이 다른 음원보다 너무 크다면:

```text
bgm.normal : 1.0
bgm.loud   : 0.7
```

처럼 Catalog에서 보정할 수 있습니다.

유효 범위:

```text
0 ~ 1
```

---

# 12. AudioCatalog

AudioCatalog는 `AudioId`를 기준으로 AudioClip과 Category를 관리하는 ScriptableObject입니다.

Project Window에서:

```text
Create
→ CDG
→ Audio
→ Audio Catalog
```

를 선택합니다.

예:

```text
AudioCatalog

Entries
├─ bgm.field
│  ├─ Category: Bgm
│  ├─ Clip: FieldBgm
│  └─ Volume Scale: 1
│
├─ bgm.boss
│  ├─ Category: Bgm
│  ├─ Clip: BossBgm
│  └─ Volume Scale: 0.8
│
└─ sfx.attack
   ├─ Category: Sfx
   ├─ Clip: Attack
   └─ Volume Scale: 1
```

생성한 Catalog를 `AudioController`의 `Catalog` 필드에 연결합니다.

---

# 13. AudioCatalog Public Query API

AudioCatalog는 등록된 Entry를 외부에서 조회할 수 있습니다.

## Count

```csharp
int count = catalog.Count;
```

## Entries

```csharp
IReadOnlyList<AudioEntry> entries = catalog.Entries;
```

외부에서는 이 목록을 통해 Entry를 직접 추가하거나 제거할 수 없습니다.

## Contains

```csharp
bool contains = catalog.Contains(new AudioId("bgm.field"));
```

## TryGet

```csharp
if (catalog.TryGet(new AudioId("bgm.field"), out AudioEntry entry))
{
    Debug.Log(entry.Clip.name);
}
```

## Get

Result 기반으로 조회할 수 있습니다.

```csharp
Result<AudioEntry> result = catalog.Get(new AudioId("bgm.field"));

if (result.IsSuccess)
{
    AudioEntry entry = result.Value;
}
```

---

# 14. AudioId Playback

AudioController에 AudioCatalog가 연결되어 있다면 ID 기반 재생을 사용할 수 있습니다.

## BGM

```csharp
AudioId id = new AudioId("bgm.field");
audioController.PlayBgm(id);
```

Fade Duration:

```csharp
audioController.PlayBgm(id, 1f);
```

Loop 설정:

```csharp
audioController.PlayBgm(id, 1f, false);
```

---

## SFX

```csharp
AudioId id = new AudioId("sfx.attack");
audioController.PlaySfx(id);
```

추가 Play Volume Scale:

```csharp
audioController.PlaySfx(id, 0.5f);
```

---

# 15. BGM Player Model

BGM은 두 개의 AudioSource를 사용합니다.

```text
BGM Source A
BGM Source B
```

일반 재생에서는 하나가 Active Source가 됩니다.

다른 BGM으로 Cross Fade할 때 다른 Source가 Incoming Source로 사용됩니다.

예:

```text
Source A : Field BGM
Source B : Empty
```

새 BGM 요청:

```text
Source A : Field BGM → Fade Out
Source B : Boss BGM  → Fade In
```

완료 후:

```text
Source A : Empty
Source B : Boss BGM
```

다음 전환에서는 두 Source의 역할이 다시 교체될 수 있습니다.

---

# 16. BGM Immediate Playback

Fade Duration이 `0`이면 현재 BGM을 즉시 교체합니다.

```csharp
audioController.PlayBgm(firstClip);
audioController.PlayBgm(secondClip);
```

결과:

```text
First BGM Stop
→ Second BGM Play
```

---

# 17. BGM Fade In

현재 BGM이 없는 상태에서 양수의 Fade Duration으로 BGM을 재생하면 Fade In이 적용됩니다.

```csharp
audioController.PlayBgm(bgmClip, 2f);
```

Fade Gain:

```text
0
↓
1
```

최종 출력은 Fade Gain과 다른 Volume 요소를 함께 계산합니다.

---

# 18. BGM Fade Out

현재 BGM을 Fade Out한 뒤 중지할 수 있습니다.

```csharp
audioController.StopBgm(2f);
```

진행:

```text
현재 Gain
↓
0
→ Stop
```

즉시 중지하려면:

```csharp
audioController.StopBgm();
```

---

# 19. BGM Cross Fade

현재 BGM 재생 중 다른 BGM을 양수 Duration으로 요청하면 Cross Fade가 적용됩니다.

```csharp
audioController.PlayBgm(fieldBgm);
audioController.PlayBgm(bossBgm, 2f);
```

전환:

```text
Field BGM
Gain 1 → 0

Boss BGM
Gain 0 → 1
```

두 BGM은 같은 시간 동안 서로 반대 방향으로 Gain이 변경됩니다.

---

# 20. Latest Request Wins

BGM 전환 중 새로운 BGM 요청이 들어오면 가장 최근 요청을 우선합니다.

예:

```text
BGM A
→ BGM B Cross Fade 시작
→ 완료 전에 BGM C 요청
```

Framework는 오래된 전환 요청을 Queue에 쌓아 순차 실행하지 않습니다.

가장 최근의 재생 요청을 기준으로 현재 상태에서 새로운 전환을 구성합니다.

이 정책은 Audio 요청이 빠르게 바뀌는 상황에서 오래된 음악 요청이 나중에 다시 재생되는 것을 방지합니다.

---

# 21. Same BGM Request

현재 관리 중인 AudioClip과 동일한 BGM을 다시 요청하면 성공으로 처리하지만 처음부터 다시 재생하지 않습니다.

```csharp
audioController.PlayBgm(fieldBgm);
audioController.PlayBgm(fieldBgm);
```

두 번째 요청은 성공 No-op입니다.

현재 BGM이 Fade Out 중인 상태에서 동일 BGM을 다시 요청하면 Pending Stop을 취소하고 기존 BGM을 유지합니다.

```text
BGM 재생
→ Fade Out 시작
→ 같은 BGM 재요청
→ Fade Out 취소
→ BGM 유지
```

---

# 22. BGM Pause / Resume

일시정지:

```csharp
audioController.PauseBgm();
```

재개:

```csharp
audioController.ResumeBgm();
```

Pause는 현재 재생 위치를 유지합니다.

Pause된 BGM에 `PlayBgm()`을 호출하는 것을 Resume 의미로 사용하지 않습니다.

재개가 필요하면 명시적으로:

```csharp
audioController.ResumeBgm();
```

을 호출합니다.

---

# 23. BGM Runtime State

현재 BGM 정보를 조회할 수 있습니다.

```csharp
AudioClip clip = audioController.CurrentBgmClip;
bool playing = audioController.IsBgmPlaying;
bool paused = audioController.IsBgmPaused;
bool fading = audioController.IsBgmFading;
```

각 값은 다음 의미를 가집니다.

### CurrentBgmClip

Framework가 현재 BGM으로 관리하고 있는 AudioClip입니다.

### IsBgmPlaying

현재 BGM AudioSource가 실제 재생 중인지 나타냅니다.

### IsBgmPaused

현재 BGM이 Pause 상태인지 나타냅니다.

### IsBgmFading

Fade In, Fade Out 또는 Cross Fade가 진행 중인지 나타냅니다.

---

# 24. SFX Voice Model

SFX는 고정된 AudioSource를 `SfxVoice`로 관리합니다.

기본 설정:

```text
Sfx Voice Count = 16
```

예:

```text
SfxPlayer
├─ Voice 0
├─ Voice 1
├─ Voice 2
├─ ...
└─ Voice 15
```

새 SFX 요청 시:

```text
사용 가능한 Voice 탐색
→ AudioClip 설정
→ Volume 적용
→ Play
```

재생이 끝나면:

```text
재생 종료 감지
→ Voice 비활성화
→ Clip 참조 정리
→ 재사용 가능
```

---

# 25. SFX Concurrent Playback

SFX는 여러 Voice를 사용하므로 동시에 재생할 수 있습니다.

```csharp
audioController.PlaySfx(hitClip);
audioController.PlaySfx(explosionClip);
audioController.PlaySfx(clickClip);
```

동일 AudioClip도 중첩할 수 있습니다.

```csharp
audioController.PlaySfx(hitClip);
audioController.PlaySfx(hitClip);
audioController.PlaySfx(hitClip);
```

각 요청은 서로 다른 사용 가능한 Voice를 사용할 수 있습니다.

---

# 26. SFX Voice Count

현재 설정된 전체 SFX Voice 수:

```csharp
int count = audioController.SfxVoiceCount;
```

현재 사용 중인 Voice 수:

```csharp
int activeCount = audioController.ActiveSfxVoiceCount;
```

Voice Count는 Inspector의:

```text
Sfx Voice Count
```

에서 설정합니다.

최소값은 `1`입니다.

---

# 27. SFX Overflow Policy

모든 Voice가 사용 중일 때 새로운 요청을 어떻게 처리할지 설정합니다.

지원 정책:

```csharp
SfxOverflowPolicy.RejectNew
SfxOverflowPolicy.StopOldest
```

AudioController 기본값:

```text
StopOldest
```

---

## 27.1 StopOldest

Voice가 모두 사용 중이면 가장 오래전에 재생을 시작한 Voice를 중지합니다.

```text
Voice 0 → oldest
Voice 1
Voice 2
...
```

새 요청:

```text
Voice 0 Stop
→ 새 SFX 재생
```

재생 순서는 내부 Sequence 값을 사용해 결정적으로 관리됩니다.

---

## 27.2 RejectNew

모든 Voice가 사용 중이면 기존 SFX를 유지하고 새 요청을 거부합니다.

Result:

```text
Failure
```

오류 코드:

```text
AUDIO_NO_AVAILABLE_SFX_VOICE
```

---

# 28. SFX Control

모든 SFX 즉시 정지:

```csharp
audioController.StopAllSfx();
```

모든 활성 SFX 일시정지:

```csharp
audioController.PauseAllSfx();
```

일시정지된 SFX 재개:

```csharp
audioController.ResumeAllSfx();
```

BGM에는 영향을 주지 않습니다.

---

# 29. Stop All

BGM과 모든 SFX를 즉시 중지하려면:

```csharp
audioController.StopAll();
```

동작:

```text
BGM Stop
+
All SFX Stop
```

Fade Out을 적용하지 않고 즉시 정지합니다.

---

# 30. Volume Model

Audio Framework는 다음 세 가지 전역 Volume을 관리합니다.

```text
Master Volume
BGM Volume
SFX Volume
```

기본값:

```text
Master = 1
BGM    = 1
SFX    = 1
```

설정:

```csharp
audioController.SetMasterVolume(0.8f);
audioController.SetBgmVolume(0.6f);
audioController.SetSfxVolume(0.7f);
```

Volume 값은 `0~1` 범위로 처리됩니다.

---

# 31. BGM Volume Calculation

Catalog 기반 BGM의 최종 출력:

```text
Master Volume
× BGM Volume
× Entry Volume Scale
× Fade Gain
```

예:

```text
Master      = 0.8
BGM         = 0.5
Entry Scale = 0.75
Fade Gain   = 1
```

최종:

```text
0.8 × 0.5 × 0.75 × 1
= 0.3
```

Direct AudioClip 재생에서는 Entry Volume Scale 대신 `1`을 사용합니다.

```text
Master
× BGM
× 1
× Fade Gain
```

---

# 32. SFX Volume Calculation

Catalog 기반 SFX의 최종 출력:

```text
Master Volume
× SFX Volume
× Entry Volume Scale
× Play Volume Scale
```

예:

```text
Master     = 0.8
SFX        = 0.5
Entry      = 0.75
Play Scale = 0.5
```

최종:

```text
0.8 × 0.5 × 0.75 × 0.5
= 0.15
```

Direct AudioClip 재생에서는 Entry Volume Scale 대신 `1`을 사용합니다.

---

# 33. Volume Input Rules

Volume 입력은 `0~1` 범위로 제한됩니다.

예:

```text
-1 → 0
 2 → 1
```

NaN은:

```text
0
```

으로 처리합니다.

Volume 변경은 새 Audio 재생부터만 적용되는 것이 아니라 현재 재생 중인 BGM과 SFX에도 즉시 반영됩니다.

Cross Fade 중에도 현재 Gain을 유지한 상태에서 변경된 Volume이 다시 계산됩니다.

---

# 34. Mute

Audio Framework는 Volume과 Mute를 별도 상태로 관리합니다.

```text
Volume
≠
Mute
```

Master Mute:

```csharp
audioController.SetMasterMuted(true);
```

BGM Mute:

```csharp
audioController.SetBgmMuted(true);
```

SFX Mute:

```csharp
audioController.SetSfxMuted(true);
```

---

# 35. Why Mute Is Separate From Volume

Mute를 Volume `0`으로 덮어쓰는 방식은 기존 사용자 설정 값을 잃을 수 있습니다.

예:

```text
Master Volume = 0.4
```

Mute:

```text
출력 = 0
Volume 설정값 = 0.4 유지
```

Mute 해제:

```text
출력 = 기존 0.4 기준으로 복원
```

따라서 Audio Framework는 Volume과 Mute를 독립적으로 관리합니다.

---

# 36. Mute State Query

```csharp
bool masterMuted = audioController.IsMasterMuted;
bool bgmMuted = audioController.IsBgmMuted;
bool sfxMuted = audioController.IsSfxMuted;
```

Volume 조회:

```csharp
float masterVolume = audioController.MasterVolume;
float bgmVolume = audioController.BgmVolume;
float sfxVolume = audioController.SfxVolume;
```

---

# 37. AudioCatalog Validation

AudioCatalog를 선택하면 기본 Inspector 아래에 Validation 결과가 표시됩니다.

정상 Catalog:

```text
Validation

Audio Catalog 유효성 검사 통과.
```

문제가 있는 경우 각 문제를 Error HelpBox로 표시합니다.

Framework는 첫 번째 오류 하나에서 검사를 종료하지 않고 가능한 Validation Issue를 모두 수집합니다.

---

# 38. Validation Rules

다음 항목을 검사합니다.

## Null Entry

```text
Entry 자체가 null
```

오류:

```text
AUDIO_INVALID_CATALOG
```

---

## Empty AudioId

```text
""
" "
```

오류:

```text
AUDIO_INVALID_ID
```

---

## Duplicate AudioId

예:

```text
bgm.field
bgm.field
```

오류:

```text
AUDIO_DUPLICATE_ID
```

---

## Null AudioClip

AudioId는 있지만 Clip이 지정되지 않은 경우입니다.

오류:

```text
AUDIO_INVALID_CATALOG
```

---

## Invalid AudioCategory

Enum에 정의되지 않은 값이 저장된 경우입니다.

오류:

```text
AUDIO_INVALID_CATALOG
```

---

## Invalid Volume Scale

유효 범위:

```text
0 ~ 1
```

다음 값도 허용하지 않습니다.

```text
NaN
Positive Infinity
Negative Infinity
```

오류:

```text
AUDIO_INVALID_CATALOG
```

---

# 39. Why No Dedicated Audio EditorWindow

v1.0에서는 별도의 Audio Framework EditorWindow를 제공하지 않습니다.

AudioCatalog의 현재 편집 작업은:

```text
Entry 추가
ID 입력
Category 선택
Clip 지정
Volume Scale 지정
Validation 확인
```

정도로 Inspector 안에서 자연스럽게 완료됩니다.

따라서 단순히 다른 패키지와 형태를 맞추기 위해 큰 EditorWindow를 추가하지 않았습니다.

향후 다음과 같은 기능이 실제 필요해질 경우 별도 Editor Tool 확장을 검토할 수 있습니다.

```text
프로젝트 AudioClip 자동 검색
대량 Catalog 등록
ID 일괄 변경
검색 / 필터
Audio Preview
Unused Clip 탐색
Duplicate Asset 탐색
AudioMixer 자동 연결
Audio Import Setting Validation
```

v1.0에서는 필요 이상의 Editor 계층을 추가하지 않는 것을 선택했습니다.

---

# 40. Result-Based Error Handling

실패할 수 있는 Runtime API는 ChoDogyu Core의 Result를 사용합니다.

예:

```csharp
using CDG.Audio;
using CDG.Core.Results;
using UnityEngine;

public sealed class AudioResultExample : MonoBehaviour
{
    [SerializeField]
    private AudioController audioController;

    private void Start()
    {
        Result result = audioController.PlayBgm(new AudioId("bgm.field"));

        if (result.IsFailure)
        {
            Debug.LogError($"{result.Error.Code}: {result.Error.Message}");
        }
    }
}
```

외부 프로젝트에서는 사용자에게 표시되는 Message 문자열보다 안정적인 `Error.Code`를 기준으로 분기하는 것을 권장합니다.

---

# 41. Error Codes

## AUDIO_INVALID_ID

AudioId가 비어 있거나 유효하지 않습니다.

대표 상황:

```text
new AudioId("")
new AudioId("   ")
```

---

## AUDIO_INVALID_CLIP

재생 요청한 AudioClip이 null이거나 유효하지 않습니다.

대표 상황:

```csharp
audioController.PlayBgm((AudioClip)null);
```

---

## AUDIO_CATALOG_NOT_ASSIGNED

AudioController에 Catalog가 없는 상태에서 ID 기반 재생을 요청했습니다.

```text
Catalog = None
+
PlayBgm(AudioId)
```

Direct AudioClip 재생에는 Catalog가 필요하지 않습니다.

---

## AUDIO_NOT_FOUND

유효한 Catalog는 존재하지만 요청한 ID가 등록되어 있지 않습니다.

```text
Requested : bgm.unknown
Catalog   : 해당 ID 없음
```

---

## AUDIO_CATEGORY_MISMATCH

Entry Category와 요청 API가 일치하지 않습니다.

예:

```text
Entry Category = Sfx
PlayBgm(id)
```

---

## AUDIO_DUPLICATE_ID

Catalog에 같은 AudioId가 두 번 이상 등록되어 있습니다.

---

## AUDIO_INVALID_CATALOG

Catalog에 잘못된 Entry 또는 설정이 포함되어 있습니다.

예:

```text
Null Entry
Null AudioClip
Invalid Category
Invalid Volume Scale
```

---

## AUDIO_NO_AVAILABLE_SFX_VOICE

모든 SFX Voice가 사용 중이고 `RejectNew` 정책이 적용되어 새 요청을 처리할 수 없습니다.

---

# 42. Runtime Source Lifecycle

AudioController는 자신이 사용하는 Runtime AudioSource를 직접 소유합니다.

초기화:

```text
AudioController Awake
→ Audio Runtime Sources 생성
→ BGM Source A 생성
→ BGM Source B 생성
→ SFX Source N개 생성
→ BgmPlayer 구성
→ SfxPlayer 구성
```

AudioController Component 제거:

```text
OnDestroy
→ BGM Stop
→ SFX Stop All
→ Audio Runtime Sources 제거
→ Runtime 참조 초기화
```

따라서 Component만 제거해도 Runtime Source 자식 GameObject가 남지 않습니다.

AudioController를 다시 추가하면 새로운 Runtime Source Root 하나만 생성됩니다.

---

# 43. Scene Lifecycle Responsibility

Audio Framework는 Scene 전환 정책을 소유하지 않습니다.

자동으로 수행하지 않는 기능:

```text
DontDestroyOnLoad
SceneManager.sceneLoaded 구독
SceneManager.sceneUnloaded 구독
Scene별 BGM 자동 선택
Scene 전환 시 BGM 자동 Stop
Scene 전환 시 SFX 자동 Stop
```

프로젝트가 Scene을 넘어 AudioController를 유지해야 한다면 프로젝트 Composition Root에서 직접 결정합니다.

예:

```csharp
private void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```

이 코드는 Audio Framework 내부에 강제로 포함되지 않습니다.

---

# 44. Why Framework Does Not Force DontDestroyOnLoad

프로젝트마다 Audio의 생명주기가 다를 수 있습니다.

예:

```text
프로젝트 A
→ 게임 전체에서 AudioController 하나 유지

프로젝트 B
→ Scene마다 AudioController 교체

프로젝트 C
→ Bootstrap Scene의 Service Root에서 유지
```

Framework가 자동 `DontDestroyOnLoad`를 사용하면 특정 프로젝트 구조를 강제하게 됩니다.

따라서 AudioController의 생명주기는 사용하는 프로젝트가 결정합니다.

---

# 45. Save Responsibility

Audio Framework는 Volume과 Mute 값을 자동 저장하지 않습니다.

예:

```text
Master Volume
BGM Volume
SFX Volume
Master Mute
BGM Mute
SFX Mute
```

이 값의 영속화는 프로젝트 또는 Save 시스템의 책임입니다.

ChoDogyu Save / Load Framework를 함께 사용하는 경우에도 두 패키지가 직접 의존하지 않습니다.

예:

```text
Save Framework
→ 설정 데이터 Load

Game Composition
→ AudioController.SetMasterVolume(...)
→ AudioController.SetBgmVolume(...)
→ AudioController.SetSfxVolume(...)
→ AudioController.SetMasterMuted(...)
→ AudioController.SetBgmMuted(...)
→ AudioController.SetSfxMuted(...)
```

이 구조를 통해 Audio Framework가 특정 Save 구현을 강제하지 않습니다.

---

# 46. UI Responsibility

Audio Framework는 Volume Slider나 Mute Toggle을 자동 연결하지 않습니다.

UI Framework를 사용하는 프로젝트라면 프로젝트 코드에서 연결합니다.

예:

```text
UI Slider
→ Game Settings Presenter
→ AudioController.SetMasterVolume()
```

Audio Framework와 UI Framework 사이에는 직접적인 패키지 의존성이 없습니다.

---

# 47. AudioMixer Responsibility

v1.0에서는 AudioMixer를 필수 구성으로 사용하지 않습니다.

Volume과 Mute는 Framework가 관리하는 AudioSource Volume을 기준으로 동작합니다.

AudioMixer를 프로젝트에서 별도로 사용하는 것은 가능합니다.

하지만 v1.0 Audio Framework는 다음을 자동 관리하지 않습니다.

```text
Mixer Parameter
Mixer Group
Mixer Snapshot
Mixer Transition
```

따라서 프로젝트 고유의 Mixer 구조를 Framework가 강제하지 않습니다.

---

# 48. Basic Usage Sample

Package Manager에서:

```text
ChoDogyu Audio Framework
→ Samples
→ Basic Usage
→ Import
```

을 선택합니다.

Import 경로:

```text
Assets/
└─ Samples/
   └─ ChoDogyu Audio Framework/
      └─ 1.0.0/
         └─ Basic Usage/
```

Sample 구성:

```text
BasicUsage/
├─ Audio/
│  ├─ AudioCatalog.asset
│  ├─ Direct Bgm A.wav
│  ├─ Direct Bgm B.wav
│  └─ Direct Sfx.wav
├─ Scenes/
│  └─ BasicUsage.unity
├─ Scripts/
│  ├─ AudioBasicUsageSample.cs
│  └─ CDG.Audio.Samples.BasicUsage.asmdef
└─ README.md
```

---

# 49. Running The Sample

다음 Scene을 엽니다.

```text
BasicUsage/Scenes/BasicUsage.unity
```

Play Mode에 진입합니다.

Game View에서 다음 기능을 바로 사용할 수 있습니다.

```text
Direct AudioClip
├─ BGM A
├─ BGM B
└─ Play SFX

Audio Catalog / AudioId
├─ Catalog BGM A
├─ Catalog BGM B
└─ Catalog SFX

BGM Control
├─ Pause
├─ Resume
├─ Fade Stop
└─ Stop All

Volume
├─ Master
├─ BGM
└─ SFX

Mute
├─ Master
├─ BGM
└─ SFX
```

Runtime State도 함께 표시됩니다.

```text
Current BGM
BGM Playing
BGM Paused
BGM Fading
Active SFX Voices
```

Sample에는 필요한 AudioClip과 Catalog가 이미 포함되어 있으므로 별도 Asset 준비 없이 실행할 수 있습니다.

---

# 50. Sample UI Design

Basic Usage Sample은 IMGUI의 `OnGUI`를 사용합니다.

이는 실제 게임 UI 구현 방식을 권장하기 위한 것이 아닙니다.

Sample의 목적은:

```text
Audio Framework 기능 검증
+
추가 UI Package 의존성 방지
```

입니다.

따라서 Sample을 Import하는 것만으로 Unity UI, TextMeshPro 또는 ChoDogyu UI Framework에 새로운 의존성이 생기지 않습니다.

---

# 51. Direct And Catalog Playback Together

Direct AudioClip API와 AudioId API는 같은 AudioController에서 함께 사용할 수 있습니다.

예:

```csharp
audioController.PlayBgm(directBgm);
audioController.PlaySfx(directSfx);

audioController.PlayBgm(new AudioId("bgm.catalog"), 1f);
audioController.PlaySfx(new AudioId("sfx.catalog"));
```

두 접근 방식은 서로 배타적이지 않습니다.

프로젝트는 상황에 따라 선택할 수 있습니다.

예:

```text
공통 게임 Audio
→ AudioCatalog 사용

런타임 생성 또는 임시 Audio
→ Direct AudioClip 사용
```

---

# 52. Catalog Or Direct Clip

## AudioCatalog가 적합한 경우

```text
여러 시스템에서 같은 Audio를 ID로 참조
Audio Asset 목록을 한 곳에서 관리
Category Validation 필요
Entry별 Volume 보정 필요
```

## Direct AudioClip이 적합한 경우

```text
Prefab이 Clip을 직접 가지고 있음
일회성 Audio
Catalog 등록이 불필요함
런타임에서 이미 AudioClip 참조를 가지고 있음
```

Framework는 두 방식을 모두 지원하며 한 방식을 강제하지 않습니다.

---

# 53. Public AudioController API

BGM:

```csharp
Result PlayBgm(AudioClip clip, float fadeDuration = 0f, bool loop = true);
Result PlayBgm(AudioId id, float fadeDuration = 0f, bool loop = true);

void StopBgm(float fadeDuration = 0f);
void PauseBgm();
void ResumeBgm();
```

SFX:

```csharp
Result PlaySfx(AudioClip clip, float volumeScale = 1f);
Result PlaySfx(AudioId id, float volumeScale = 1f);

void StopAllSfx();
void PauseAllSfx();
void ResumeAllSfx();
```

Volume:

```csharp
void SetMasterVolume(float volume);
void SetBgmVolume(float volume);
void SetSfxVolume(float volume);
```

Mute:

```csharp
void SetMasterMuted(bool muted);
void SetBgmMuted(bool muted);
void SetSfxMuted(bool muted);
```

전체 정지:

```csharp
void StopAll();
```

State:

```csharp
AudioCatalog Catalog { get; }

float MasterVolume { get; }
float BgmVolume { get; }
float SfxVolume { get; }

bool IsMasterMuted { get; }
bool IsBgmMuted { get; }
bool IsSfxMuted { get; }

AudioClip CurrentBgmClip { get; }
bool IsBgmPlaying { get; }
bool IsBgmPaused { get; }
bool IsBgmFading { get; }

int SfxVoiceCount { get; }
int ActiveSfxVoiceCount { get; }
```

---

# 54. Recommended Composition

프로젝트에서는 AudioController를 직접 모든 곳에서 찾는 방식보다 프로젝트의 Composition Root 또는 명확한 의존성 전달 구조를 사용하는 것을 권장합니다.

예:

```text
Game Root
├─ AudioController
├─ Save System
├─ Scene System
└─ UI System
```

게임 시스템:

```text
Battle System
→ AudioController 참조

UI Presenter
→ AudioController 참조

Scene Composition
→ AudioController 참조
```

Audio Framework 자체는 Singleton이나 Service Locator를 제공하지 않습니다.

---

# 55. No Forced Singleton

AudioController는 Singleton이 아닙니다.

Framework는:

```text
AudioController.Instance
```

같은 전역 접근 API를 제공하지 않습니다.

이유:

```text
프로젝트별 생명주기 정책 보존
테스트 가능성 유지
명시적인 의존성 전달 가능
멀티 컨텍스트 구성 가능
Framework의 전역 상태 강제 방지
```

필요하다면 사용하는 프로젝트가 자신의 Composition 정책에서 Singleton 형태를 적용할 수 있습니다.

---

# 56. Time Scale

BGM Fade Transition은 Runtime에서:

```text
Time.unscaledDeltaTime
```

을 기준으로 갱신됩니다.

따라서:

```text
Time.timeScale = 0
```

상태에서도 Fade가 계속 진행될 수 있습니다.

이는 Pause Menu 등 게임 시간이 정지된 상태에서도 Audio Transition을 완료할 수 있도록 하기 위한 정책입니다.

---

# 57. Validation And Runtime Validation

Editor의 Catalog Validation은 여러 문제를 한 번에 보여주는 사용자 편의 기능입니다.

하지만 Runtime도 Catalog 유효성을 별도로 신뢰하지 않습니다.

Runtime에서 Catalog Lookup을 구성할 때 핵심 유효성 규칙을 다시 확인합니다.

따라서 Editor Validation을 수행하지 않은 Asset이 Runtime에 전달되어도 Framework가 잘못된 Catalog 상태를 감지할 수 있습니다.

```text
Editor Validation
→ 제작 단계 문제 확인

Runtime Validation
→ 실행 단계 안전성 유지
```

두 계층의 목적이 다릅니다.

---

# 58. Tests

Audio Framework는 Runtime과 Editor Test Assembly를 분리합니다.

```text
Tests/
├─ Runtime/
│  └─ CDG.Audio.Tests.Runtime
└─ Editor/
   └─ CDG.Audio.Tests.Editor
```

주요 Runtime 검증 범위:

```text
AudioId
AudioCatalog
AudioVolumeState
BGM 재생
Fade In
Fade Out
Cross Fade
Same BGM Request
SFX Voice
동시 재생
Overflow
Volume
Mute
AudioController Integration
Runtime Source Lifecycle
```

주요 Editor 검증 범위:

```text
Valid Catalog
Null Entry
Empty ID
Duplicate ID
Null Clip
Invalid Category
Invalid Volume
NaN
Infinity
Multiple Issues
Entry Index
```

---

# 59. UPM Verification

v1.0은 개발 프로젝트의 Local Package 연결만으로 검증하지 않았습니다.

완전히 새로운 Unity 프로젝트에서 실제 Git UPM 설치 흐름을 검증했습니다.

검증 환경:

```text
Unity 6.3 LTS
6000.3.9f1
```

검증 순서:

```text
새 Unity 프로젝트 생성
→ ChoDogyu Core v1.0.0 Git 설치
→ ChoDogyu Audio Framework Git 설치
→ Compile 확인
→ Basic Usage Sample Import
→ Sample Scene 실행
→ Direct BGM / SFX 확인
→ AudioId BGM / SFX 확인
→ Cross Fade 확인
→ Volume / Mute 확인
→ Pause / Resume 확인
→ Fade Stop 확인
→ Stop All 확인
→ Sample 삭제
→ Audio Framework 제거
→ Core 유지 확인
→ Audio Framework 재설치
→ Sample 재Import
→ Sample Scene 재실행
```

최종 확인:

```text
Missing Script 없음
Missing Reference 없음
Console Error 없음
개발 프로젝트 Local Package 경로 비의존
```

---

# 60. Package Removal

Audio Framework Package를 제거하기 전에 Import한 Sample이 필요하지 않다면:

```text
Assets/Samples/ChoDogyu Audio Framework/
```

를 삭제할 수 있습니다.

Package Manager에서 Audio Framework를 제거해도 ChoDogyu Core는 별도 패키지이므로 자동 제거되지 않습니다.

```text
Audio Framework Remove
→ Core 유지
```

이는 각 CDG 패키지가 독립 제품으로 설치되는 구조를 유지하기 위한 것입니다.

---

# 61. v1.0 Scope

v1.0은 다음 기능에 집중합니다.

```text
BGM Playback
BGM Fade
BGM Cross Fade
SFX Playback
SFX Concurrent Playback
SFX Fixed Voice Management
SFX Overflow
Master / BGM / SFX Volume
Master / BGM / SFX Mute
AudioId
AudioCatalog
Catalog Validation
Basic Usage Sample
Result Error Handling
```

---

# 62. Features Outside v1.0 Scope

다음 기능은 v1.0에서 의도적으로 제외했습니다.

```text
AudioMixer Parameter 자동 관리
AudioMixer Snapshot
3D Spatial Audio Framework
Follow Target Audio
개별 SFX Handle
개별 SFX Stop Handle
Looping SFX Framework
Random Pitch
Random Audio Group
BGM Playlist
BGM Queue
Audio Ducking
Dialogue Audio
Voice Over System
Addressables 자동 연동
Streaming Audio 관리
Scene별 BGM Mapping
Scene 전환 자동 Audio 처리
Volume 설정 자동 Save
Mute 설정 자동 Save
UI Slider 자동 연결
자동 DontDestroyOnLoad
```

기능이 적어서 제외한 것이 아니라 Audio Framework의 v1.0 책임을 명확하게 유지하기 위한 범위 결정입니다.

---

# 63. Extension Direction

향후 확장이 필요할 경우 현재 구조를 기반으로 독립적으로 추가할 수 있습니다.

예:

```text
AudioMixer Adapter
3D Audio Layer
Looping SFX Handle
BGM Playlist
Audio Ducking
Addressables Audio Provider
Audio Preview Editor Tool
```

확장 시에도 기존 `AudioController`의 기본 책임을 불필요하게 비대하게 만들지 않는 것을 우선합니다.

---

# 64. Design Principles

Audio Framework는 다음 원칙을 기준으로 구성했습니다.

## 명확한 책임

```text
Framework
→ 재생 방법 관리

Game
→ 재생 시점 결정
```

## 최소 의존성

```text
Audio
→ Core

Audio
-X→ Save
-X→ UI
-X→ Scene
-X→ Pooling
```

## Runtime과 Editor 분리

```text
CDG.Audio
CDG.Audio.Editor
```

## 내부 구현 캡슐화

```text
외부
→ AudioController

내부
→ BgmPlayer
→ SfxPlayer
→ SfxVoice
→ AudioVolumeState
```

## 명시적 실패

```text
Result
Result<T>
AudioErrorCodes
```

## 프로젝트 정책 비강제

```text
No Singleton
No DontDestroyOnLoad
No Save Auto Integration
No Scene Auto Integration
No UI Auto Integration
```

---

# 65. Troubleshooting

## ID 기반 재생이 AUDIO_CATALOG_NOT_ASSIGNED로 실패합니다

AudioController의 Inspector에서 AudioCatalog가 지정되어 있는지 확인합니다.

```text
AudioController
→ Catalog
```

Direct AudioClip 재생에는 Catalog가 필요하지 않습니다.

---

## AUDIO_NOT_FOUND가 반환됩니다

AudioCatalog에 요청한 AudioId가 정확하게 존재하는지 확인합니다.

AudioId는 대소문자를 구분합니다.

```text
bgm.field
BGM.FIELD
```

은 서로 다른 ID입니다.

---

## AUDIO_CATEGORY_MISMATCH가 반환됩니다

Entry의 Category와 사용한 API를 확인합니다.

```text
Bgm Entry
→ PlayBgm

Sfx Entry
→ PlaySfx
```

---

## SFX가 새로 재생되지 않습니다

Overflow Policy가 `RejectNew`이고 모든 Voice가 사용 중인지 확인합니다.

필요에 따라:

```text
Sfx Voice Count 증가
```

또는:

```text
Overflow Policy = StopOldest
```

를 사용할 수 있습니다.

---

## BGM이 즉시 바뀝니다

Cross Fade를 사용하려면 양수의 Fade Duration을 전달해야 합니다.

```csharp
audioController.PlayBgm(nextBgm, 1f);
```

Duration `0`은 즉시 교체입니다.

---

## Scene을 바꾸자 AudioController가 사라집니다

Audio Framework는 자동 `DontDestroyOnLoad`를 사용하지 않습니다.

Scene 간 유지가 필요하다면 프로젝트 Composition Root에서 생명주기를 결정합니다.

---

## Volume 설정이 재실행 후 복원되지 않습니다

Audio Framework는 설정을 자동 저장하지 않습니다.

Save System에서 값을 저장하고 로드 후 AudioController의 Volume / Mute API를 호출해야 합니다.

---

## Catalog Inspector에서 Validation Error가 표시됩니다

HelpBox에 표시되는 오류 코드와 Entry Index를 확인합니다.

대표적으로 다음 항목을 검사합니다.

```text
Empty ID
Duplicate ID
Null Clip
Invalid Category
Invalid Volume Scale
```

---

# 66. Summary

ChoDogyu Audio Framework v1.0.0은 다음 문제를 해결하는 것을 목표로 합니다.

```text
BGM 재생 로직이 프로젝트마다 반복됨
→ AudioController로 통합

Fade / Cross Fade 구현이 반복됨
→ BgmPlayer 내부 처리

SFX마다 AudioSource를 생성함
→ 고정 Voice 재사용

동시 SFX 제한 정책이 없음
→ Overflow Policy 제공

Volume 계산이 여러 시스템에 흩어짐
→ Master / BGM / SFX 모델 통합

Mute가 Volume 값을 파괴함
→ Volume과 Mute 분리

문자열 Audio Key 관리가 불안정함
→ AudioId 제공

Audio Asset 등록 오류를 늦게 발견함
→ AudioCatalog Validation 제공

Scene / Save / UI 정책까지 Audio System이 소유함
→ 프로젝트 책임으로 분리
```

Framework는 게임별 Audio 규칙을 대신 결정하는 시스템이 아니라, 여러 Unity 프로젝트에서 반복되는 Audio Runtime 기반을 재사용 가능한 패키지로 제공하는 것을 목표로 합니다.