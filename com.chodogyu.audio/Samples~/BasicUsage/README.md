# ChoDogyu Audio Framework - Basic Usage

Audio Framework의 기본 Runtime 사용 흐름을 바로 실행하여 확인할 수 있는 Sample입니다.

Sample에는 실행 가능한 Scene, AudioCatalog와 테스트용 AudioClip이 포함되어 있습니다.

다음 기능을 확인할 수 있습니다.

- Direct AudioClip BGM 재생
- Direct AudioClip SFX 재생
- AudioId / AudioCatalog 기반 BGM 재생
- AudioId / AudioCatalog 기반 SFX 재생
- BGM Fade In
- BGM Cross Fade
- BGM Fade Out
- BGM Pause / Resume
- SFX 동시 재생
- Master / BGM / SFX Volume
- Master / BGM / SFX Mute
- Runtime BGM 및 SFX 상태 확인

## 포함 내용

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

## 실행 방법

Unity Package Manager에서 `Basic Usage` Sample을 Import합니다.

Import된 Sample의 다음 Scene을 엽니다.

```text
Scenes/BasicUsage.unity
```

Play Mode에 진입하면 Game View 왼쪽 상단에 Sample 조작 UI가 표시됩니다.

Sample Scene에는 다음 구성이 미리 연결되어 있습니다.

```text
AudioSample
├─ AudioController
└─ AudioBasicUsageSample
```

`AudioController`에는 Sample의 `AudioCatalog`가 연결되어 있으며, `AudioBasicUsageSample`에는 BGM A, BGM B와 SFX AudioClip이 연결되어 있습니다.

따라서 별도의 설정 없이 Sample Scene을 실행하여 기본 기능을 확인할 수 있습니다.

## Direct AudioClip

다음 버튼으로 AudioClip을 직접 재생할 수 있습니다.

```text
BGM A
BGM B
Play SFX
```

`BGM A`를 재생한 뒤 `BGM B`를 누르면 `Cross Fade Duration`에 따라 Cross Fade가 적용됩니다.

`Play SFX`를 연속해서 누르면 여러 SFX Voice를 사용하여 동일한 SFX를 중첩 재생할 수 있습니다.

## Audio Catalog

Sample에는 다음 Entry가 등록된 `AudioCatalog`가 포함되어 있습니다.

```text
ID              Category    Clip
bgm.sample.a    Bgm         Direct Bgm A
bgm.sample.b    Bgm         Direct Bgm B
sfx.sample      Sfx         Direct Sfx
```

다음 버튼은 `AudioId`를 이용하여 Catalog에서 Audio Entry를 조회한 뒤 재생합니다.

```text
Catalog BGM A
Catalog BGM B
Catalog SFX
```

`Catalog BGM A`를 재생한 뒤 `Catalog BGM B`를 누르면 ID 기반 재생에서도 BGM Cross Fade를 확인할 수 있습니다.

`Catalog SFX`를 연속해서 누르면 Catalog 기반 SFX 중첩 재생을 확인할 수 있습니다.

## Volume

Sample UI의 Slider를 사용하여 다음 Volume을 실시간으로 변경할 수 있습니다.

```text
Master Volume
BGM Volume
SFX Volume
```

### Master Volume

BGM과 SFX 전체 출력에 적용됩니다.

### BGM Volume

BGM 출력에만 적용됩니다.

### SFX Volume

SFX 출력에만 적용됩니다.

## Mute

다음 Mute 상태를 독립적으로 확인할 수 있습니다.

```text
Master Mute
BGM Mute
SFX Mute
```

### Master Mute

현재 BGM과 SFX 전체를 음소거합니다.

### BGM Mute

BGM만 음소거합니다.

### SFX Mute

SFX만 음소거합니다.

Mute를 해제하면 기존 Volume 설정이 다시 적용됩니다.

## BGM Control

다음 BGM 제어 기능을 확인할 수 있습니다.

```text
Pause
Resume
Fade Stop
```

### Pause

현재 BGM을 일시정지합니다.

### Resume

Pause 상태의 BGM을 다시 재생합니다.

### Fade Stop

현재 BGM을 즉시 중지하지 않고 `Cross Fade Duration`에 설정된 시간 동안 Fade Out한 뒤 중지합니다.

## Stop All

`Stop All`은 현재 BGM과 사용 중인 모든 SFX Voice를 즉시 중지합니다.

## Runtime State

Sample UI 하단에서 현재 Runtime 상태를 확인할 수 있습니다.

```text
Current BGM
BGM Playing
BGM Paused
BGM Fading
Active SFX Voices
```

### Current BGM

현재 Audio Framework가 관리하고 있는 BGM AudioClip 이름을 표시합니다.

### BGM Playing

현재 BGM이 실제로 재생 중인지 표시합니다.

### BGM Paused

현재 BGM이 Pause 상태인지 표시합니다.

### BGM Fading

현재 Fade 또는 Cross Fade가 진행 중인지 표시합니다.

### Active SFX Voices

현재 사용 중인 SFX Voice 수와 전체 SFX Voice 수를 표시합니다.

## 직접 구성해보기

Sample에 포함된 Scene을 사용하지 않고 직접 구성하려면 빈 GameObject에 다음 Component를 추가할 수 있습니다.

```text
AudioController
AudioBasicUsageSample
```

`AudioBasicUsageSample`에 사용할 AudioClip을 지정하고, ID 기반 재생을 사용하려면 `AudioController`에 `AudioCatalog`를 지정합니다.

AudioCatalog는 다음 메뉴에서 생성할 수 있습니다.

```text
Create
→ CDG
→ Audio
→ Audio Catalog
```

## Notes

이 Sample의 `OnGUI`는 Audio Framework의 기능을 추가 UI 패키지 의존성 없이 확인하기 위한 Sample 전용 인터페이스입니다.

Audio Framework 자체는 특정 UI 시스템에 의존하지 않습니다.

Sample에 포함된 Audio Asset은 Framework 기능 확인을 위한 예제 리소스이며, 실제 프로젝트에서는 프로젝트에서 사용하는 AudioClip으로 교체하여 사용할 수 있습니다.