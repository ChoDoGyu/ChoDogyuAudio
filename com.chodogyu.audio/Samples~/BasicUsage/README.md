# ChoDogyu Audio Framework - Basic Usage

Audio Framework의 기본 Runtime 사용 흐름을 확인하기 위한 Sample입니다.

이 Sample은 별도의 Audio Asset을 포함하지 않습니다.

프로젝트에서 사용할 수 있는 AudioClip을 직접 지정하여 다음 기능을 확인할 수 있습니다.

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

## Setup

빈 Scene에서 GameObject를 하나 생성합니다.

```text
Audio Sample
```

다음 Component를 추가합니다.

```text
AudioController
AudioBasicUsageSample
```

`AudioBasicUsageSample`에는 다음 AudioClip을 지정합니다.

```text
Direct Bgm A
Direct Bgm B
Direct Sfx
```

Play Mode에 진입하면 Game View 왼쪽 상단에 Sample 조작 UI가 표시됩니다.

## Direct AudioClip

다음 버튼으로 AudioClip을 직접 재생할 수 있습니다.

```text
BGM A
BGM B
Play SFX
```

BGM A를 재생한 뒤 BGM B를 누르면 `Cross Fade Duration`에 따라 Cross Fade가 적용됩니다.

## Audio Catalog

AudioId 기반 재생을 확인하려면 AudioCatalog를 생성합니다.

Unity Project Window에서 다음 메뉴를 사용합니다.

```text
Create
→ CDG
→ Audio
→ Audio Catalog
```

예를 들어 다음 Entry를 등록합니다.

```text
ID              Category    Clip
bgm.sample.a    Bgm         BGM A
bgm.sample.b    Bgm         BGM B
sfx.sample      Sfx         SFX
```

생성한 AudioCatalog를 같은 GameObject의 `AudioController`에 지정합니다.

이후 다음 버튼을 사용할 수 있습니다.

```text
Catalog BGM A
Catalog BGM B
Catalog SFX
```

ID 문자열을 변경했다면 `AudioBasicUsageSample`의 AudioId 값도 동일하게 변경합니다.

## Volume

Sample UI의 Slider를 사용하여 다음 값을 실시간으로 변경할 수 있습니다.

```text
Master Volume
BGM Volume
SFX Volume
```

## Mute

다음 Mute 상태를 독립적으로 확인할 수 있습니다.

```text
Master Mute
BGM Mute
SFX Mute
```

## BGM Control

다음 BGM 제어를 확인할 수 있습니다.

```text
Pause
Resume
Fade Stop
```

`Fade Stop`은 `Cross Fade Duration` 값을 Fade Out 시간으로 사용합니다.

## Stop All

`Stop All`은 현재 BGM과 모든 SFX를 즉시 중지합니다.

## Runtime State

Sample UI 하단에서 다음 상태를 확인할 수 있습니다.

```text
Current BGM
BGM Playing
BGM Paused
BGM Fading
Active SFX Voices
```

## Notes

이 Sample의 `OnGUI`는 Audio Framework의 기능을 추가 UI 패키지 의존성 없이 확인하기 위한 Sample 전용 인터페이스입니다.

Audio Framework 자체는 특정 UI 시스템에 의존하지 않습니다.