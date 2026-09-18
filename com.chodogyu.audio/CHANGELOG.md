# Changelog

ChoDogyu Audio Framework 패키지의 주요 변경 사항을 기록합니다.

## [1.0.0] - 2026-09-18

첫 정식 배포 버전입니다.

### Added

#### Runtime Core

- Runtime Assembly `CDG.Audio` 추가
- Root Namespace `CDG.Audio` 적용
- `AudioController` 기반 Runtime 진입점 제공
- 직접 `AudioClip` 재생 지원
- `AudioId` / `AudioCatalog` 기반 재생 지원
- BGM과 SFX 책임 분리
- Master / BGM / SFX Volume 독립 관리
- Master / BGM / SFX Mute 독립 관리
- ChoDogyu Core의 `Result`, `Result<T>`, `ResultError` 기반 오류 처리 적용
- Runtime Singleton 및 Service Locator 비사용
- 특정 게임 및 장르 규칙 비포함
- 자동 `DontDestroyOnLoad` 비사용
- Scene 이벤트 자동 구독 비사용
- Save / UI / Scene Framework 직접 의존성 비사용

#### AudioId

- 문자열 기반 `AudioId` 값 형식 추가
- `IEquatable<AudioId>` 구현
- Ordinal 문자열 비교 적용
- `Value` 제공
- `IsEmpty` 제공
- 빈 문자열 ID 검증
- 공백만 포함된 ID 검증
- 문자열 정규화 비사용
- 자동 Trim 비사용
- 자동 대소문자 변경 비사용
- `ToString()`을 통한 원본 ID 값 반환

#### AudioCategory

- `AudioCategory.Bgm` 추가
- `AudioCategory.Sfx` 추가
- BGM / SFX Catalog Entry 역할 구분
- 재생 API와 Entry Category 불일치 검증

#### AudioEntry

- Audio Entry 구조 추가
- `AudioId` 저장
- `AudioCategory` 저장
- `AudioClip` 저장
- Entry별 `VolumeScale` 저장
- Catalog 기반 BGM 및 SFX 재생 시 Entry Volume Scale 적용

#### AudioCatalog

- `AudioCatalog` ScriptableObject 추가
- 메뉴 경로 `Create/CDG/Audio/Audio Catalog` 제공
- Audio Entry 목록 관리
- Read-only `Entries` 제공
- `Count` 제공
- `Contains()` 제공
- `TryGet()` 제공
- `Get()` Result API 제공
- AudioId 기반 Lookup Cache 구성
- Catalog 변경 시 Lookup 재구성
- 빈 AudioId 검증
- 중복 AudioId 검증
- null AudioClip 검증
- 유효하지 않은 AudioCategory 검증
- 잘못된 Volume Scale 검증
- null Entry 검증
- 잘못된 Catalog 상태에서 Result Error 반환

#### AudioController

- `AudioController` Component 추가
- `[DisallowMultipleComponent]` 적용
- Direct AudioClip BGM 재생 API 제공
- AudioId 기반 BGM 재생 API 제공
- Direct AudioClip SFX 재생 API 제공
- AudioId 기반 SFX 재생 API 제공
- BGM Stop API 제공
- BGM Pause / Resume API 제공
- 전체 SFX Stop API 제공
- 전체 SFX Pause / Resume API 제공
- 전체 Audio Stop API 제공
- Master Volume 설정 API 제공
- BGM Volume 설정 API 제공
- SFX Volume 설정 API 제공
- Master Mute 설정 API 제공
- BGM Mute 설정 API 제공
- SFX Mute 설정 API 제공
- 현재 BGM AudioClip 조회 제공
- BGM Playing 상태 조회 제공
- BGM Paused 상태 조회 제공
- BGM Fading 상태 조회 제공
- 전체 SFX Voice 수 조회 제공
- 활성 SFX Voice 수 조회 제공
- AudioCatalog Inspector 참조 지원

#### Runtime AudioSource Management

- `AudioController` 초기화 시 Runtime AudioSource 자동 구성
- `Audio Runtime Sources` 자식 GameObject 생성
- BGM용 AudioSource 2개 생성
- SFX용 고정 AudioSource Voice 생성
- 기본 SFX Voice Count `16`
- 재생마다 AudioSource를 생성하거나 파괴하지 않는 구조 적용
- SFX AudioSource 재사용
- Runtime Source Root 중복 생성 방지
- `AudioController` 제거 시 Framework 소유 Runtime Source 정리
- BGM 및 SFX 중지 후 Runtime Source 제거
- 내부 Runtime 참조 해제
- Controller 제거 후 재추가 시 단일 Runtime Source Root 유지

#### BGM Player

- 내부 `BgmPlayer` 추가
- BGM용 두 개의 AudioSource 관리
- BGM 기본 재생 지원
- Loop 설정 지원
- 즉시 BGM 교체 지원
- Fade In 지원
- Fade Out 지원
- Cross Fade 지원
- Pause 지원
- Resume 지원
- 현재 BGM 조회 지원
- Playing 상태 조회 지원
- Paused 상태 조회 지원
- Fading 상태 조회 지원
- 양수 Duration 기반 Transition 처리
- Duration `0` 즉시 처리
- `Time.unscaledDeltaTime` 기반 Fade 갱신
- `Time.timeScale = 0` 상태와 독립된 Fade 진행
- Fade Gain과 사용자 Volume Scale 분리
- 현재 BGM과 동일한 AudioClip 재생 요청을 성공 No-op으로 처리
- Fade Out 중 동일 BGM 재요청 시 Pending Stop 취소
- Cross Fade 중 최신 재생 요청 우선 처리

#### BGM Fade

- Fade In Transition 추가
- Fade Out Transition 추가
- 현재 Fade Gain에서 자연스럽게 이어지는 Transition 처리
- Fade 완료 후 최종 Gain 정규화
- Fade Out 완료 시 AudioSource 정리

#### BGM Cross Fade

- Dual AudioSource 기반 Cross Fade 구현
- Incoming BGM Gain `0 → 1`
- Outgoing BGM Gain `현재 값 → 0`
- Cross Fade 중 각 BGM의 Entry Volume Scale 유지
- Transition 완료 후 이전 AudioSource 정리
- Cross Fade 도중 새로운 요청 처리
- 가장 최근 BGM 요청 우선 정책 적용

#### SFX Voice

- 내부 `SfxVoice` 추가
- 하나의 AudioSource를 하나의 Voice로 관리
- SFX 재생 상태 관리
- SFX Pause 상태 관리
- SFX Stop 지원
- 재생 완료 상태 감지
- 완료된 Voice 자동 비활성화
- 완료된 Voice의 AudioClip 참조 정리
- 현재 Clip 조회 제공
- Active 상태 조회 제공
- Playing 상태 조회 제공
- Paused 상태 조회 제공
- 재생 요청별 Volume Scale 지원

#### SFX Player

- 내부 `SfxPlayer` 추가
- 고정된 SFX Voice 목록 관리
- 여러 SFX 동시 재생 지원
- 동일 AudioClip 중첩 재생 지원
- 사용 가능한 첫 Voice 선택
- 전체 Voice Pause 지원
- 전체 Voice Resume 지원
- 전체 Voice Stop 지원
- 활성 Voice 수 조회
- 전체 Voice 수 조회
- SFX 완료 후 Voice 재사용
- 재생 순서를 위한 Sequence 관리
- 동적 AudioSource 생성 비사용

#### SFX Overflow

- `SfxOverflowPolicy` 추가
- `RejectNew` 정책 추가
- `StopOldest` 정책 추가
- `AudioController` 기본 Overflow 정책을 `StopOldest`로 설정
- 모든 Voice 사용 중 `RejectNew` 선택 시 새 요청 거부
- 모든 Voice 사용 중 `StopOldest` 선택 시 가장 오래된 Voice 재사용
- Sequence 기반 결정적 Oldest 선택
- 사용 가능한 Voice가 없는 경우 Result Error 반환
- 잘못된 직렬화 Overflow 값에 대한 `StopOldest` Fallback 적용

#### Volume

- 내부 `AudioVolumeState` 추가
- Master Volume 기본값 `1`
- BGM Volume 기본값 `1`
- SFX Volume 기본값 `1`
- Volume 값 `0~1` 범위 처리
- NaN 입력을 `0`으로 처리
- 현재 재생 중인 BGM에 Volume 변경 즉시 반영
- 현재 재생 중인 SFX에 Volume 변경 즉시 반영
- Fade 진행 중 Volume 변경 즉시 반영

BGM 최종 Volume 계산:

```text
Master Volume
× BGM Volume
× Entry Volume Scale
× Fade Gain
```

SFX 최종 Volume 계산:

```text
Master Volume
× SFX Volume
× Entry Volume Scale
× Play Volume Scale
```

#### Mute

- Master Mute 상태 추가
- BGM Mute 상태 추가
- SFX Mute 상태 추가
- Mute 상태와 Volume 값을 별도로 관리
- Master Mute 시 BGM과 SFX 전체 출력 차단
- BGM Mute 시 BGM 출력만 차단
- SFX Mute 시 SFX 출력만 차단
- Mute 해제 시 기존 Volume 값 복원

#### Runtime Error Codes

- `AUDIO_INVALID_ID`
- `AUDIO_INVALID_CLIP`
- `AUDIO_CATALOG_NOT_ASSIGNED`
- `AUDIO_NOT_FOUND`
- `AUDIO_CATEGORY_MISMATCH`
- `AUDIO_DUPLICATE_ID`
- `AUDIO_INVALID_CATALOG`
- `AUDIO_NO_AVAILABLE_SFX_VOICE`

#### Editor Assembly

- Editor Assembly `CDG.Audio.Editor` 추가
- Root Namespace `CDG.Audio.Editor` 적용
- Editor Platform 전용 Assembly 구성
- `CDG.Audio` 참조
- `CDG.Core` 참조

#### Audio Catalog Validation

- `AudioCatalogValidator` 추가
- AudioCatalog 전체 Entry Validation 지원
- Validation Issue 구조 추가
- Entry Index 기반 오류 위치 제공
- null Entry 검사
- 빈 AudioId 검사
- 공백 AudioId 검사
- 중복 AudioId 검사
- null AudioClip 검사
- 유효하지 않은 AudioCategory 검사
- `0~1` 범위를 벗어난 Volume Scale 검사
- NaN Volume Scale 검사
- Positive Infinity 검사
- Negative Infinity 검사
- 여러 Validation Issue 동시 수집
- 유효한 Catalog에 대한 정상 결과 제공

#### Audio Catalog Custom Inspector

- `AudioCatalogInspector` 추가
- 기본 Serialized Inspector 유지
- Catalog Validation 결과 표시
- 정상 Catalog에 Info Message 표시
- 잘못된 Catalog에 Error HelpBox 표시
- 여러 Validation Issue 동시 표시
- 별도 대형 EditorWindow 비사용
- Catalog 편집과 Validation을 Inspector에서 직접 수행하는 구조 적용

#### Basic Usage Sample

- Package Manager용 `Basic Usage` Sample 추가
- Sample 전용 Assembly `CDG.Audio.Samples.BasicUsage` 추가
- 실행 가능한 `BasicUsage` Scene 추가
- Sample AudioCatalog 추가
- 테스트용 BGM AudioClip 2개 추가
- 테스트용 SFX AudioClip 1개 추가
- `AudioBasicUsageSample` 추가
- Direct AudioClip BGM 재생 예제 제공
- Direct AudioClip SFX 재생 예제 제공
- AudioId 기반 BGM 재생 예제 제공
- AudioId 기반 SFX 재생 예제 제공
- BGM Cross Fade 예제 제공
- BGM Fade Stop 예제 제공
- BGM Pause / Resume 예제 제공
- Master / BGM / SFX Volume 조절 예제 제공
- Master / BGM / SFX Mute 예제 제공
- Stop All 예제 제공
- Current BGM 상태 표시
- BGM Playing 상태 표시
- BGM Paused 상태 표시
- BGM Fading 상태 표시
- Active SFX Voice 상태 표시
- SFX 중첩 재생 확인 지원
- 별도 UI Framework 의존성을 만들지 않기 위해 IMGUI 조작 패널 사용
- Sample Import 후 별도 Asset 준비 없이 즉시 실행 가능
- Sample 제거 후 재Import 검증

#### Package Structure

- Package Name `com.chodogyu.audio`
- Display Name `ChoDogyu Audio Framework`
- Package Version `1.0.0`
- Runtime Assembly `CDG.Audio`
- Editor Assembly `CDG.Audio.Editor`
- Runtime Test Assembly `CDG.Audio.Tests.Runtime`
- Editor Test Assembly `CDG.Audio.Tests.Editor`
- Sample Assembly `CDG.Audio.Samples.BasicUsage`
- Runtime Namespace `CDG.Audio`
- Editor Namespace `CDG.Audio.Editor`
- Sample Namespace `CDG.Audio.Samples.BasicUsage`
- Unity 6.3 기준 패키지 구성
- ChoDogyu Core 1.0.0 사용
- ChoDogyu Object Pooling 비의존
- ChoDogyu Data Framework 비의존
- ChoDogyu Save / Load Framework 비의존
- ChoDogyu UI Framework 비의존
- ChoDogyu General Editor Tools 비의존
- AudioMixer 필수 의존성 없음
- Addressables 비의존
- DOTween 비의존
- 특정 게임 및 장르 코드 비포함

#### Tests

- AudioId 동등성 및 유효성 검증
- AudioCategory 검증
- AudioCatalog 등록 및 조회 검증
- Catalog Lookup 검증
- Catalog 중복 ID 검증
- Catalog 잘못된 Entry 검증
- AudioVolumeState 검증
- Master / BGM / SFX Volume 검증
- Master / BGM / SFX Mute 검증
- BGM 기본 재생 검증
- BGM Stop 검증
- BGM Pause / Resume 검증
- BGM Fade In 검증
- BGM Fade Out 검증
- BGM Cross Fade 검증
- 동일 BGM 재생 요청 검증
- Fade Out 중 동일 BGM 재요청 검증
- SFX Voice 재생 및 재사용 검증
- SFX 동시 재생 검증
- 동일 SFX 중첩 재생 검증
- `RejectNew` Overflow 검증
- `StopOldest` Overflow 검증
- Oldest Voice 결정 순서 검증
- 고정 AudioSource 수 유지 검증
- AudioController Direct 재생 검증
- AudioController AudioId 재생 검증
- BGM Category Mismatch 검증
- SFX Category Mismatch 검증
- Catalog 미지정 오류 검증
- 존재하지 않는 AudioId 오류 검증
- Catalog Entry Volume Scale 적용 검증
- SFX Request Volume Scale 적용 검증
- Cross Fade 중 Volume 변경 검증
- Master Mute 후 Volume 복원 검증
- BGM과 SFX 동시 사용 검증
- Runtime Tick 기반 SFX Voice 반환 검증
- Catalog Editor Validation 검증
- 여러 Editor Validation Issue 동시 검증
- AudioController 제거 시 Runtime Source 정리 검증
- AudioController 재추가 시 Runtime Source 중복 방지 검증
- Runtime Tests 전체 Passed 확인
- Editor Tests 전체 Passed 확인
- Failed 0 확인

#### UPM Verification

- 완전히 새로운 Unity 6.3 프로젝트에서 Git UPM 설치 검증
- Unity 6.3 LTS `6000.3.9f1` 환경 검증
- ChoDogyu Core v1.0.0 선설치 검증
- ChoDogyu Audio Framework Git 설치 검증
- Package Version `1.0.0` 확인
- Runtime Assembly Compile 검증
- Editor Assembly Compile 검증
- Basic Usage Sample 표시 검증
- Basic Usage Sample Import 검증
- Sample Scene Missing Script 없음 확인
- Sample Scene Missing Reference 없음 확인
- Direct BGM 재생 검증
- Direct SFX 재생 검증
- AudioId BGM 재생 검증
- AudioId SFX 재생 검증
- BGM Cross Fade 검증
- Volume 동작 검증
- Mute 동작 검증
- Pause / Resume 검증
- Fade Stop 검증
- Stop All 검증
- Audio Framework Package 제거 검증
- Audio 제거 후 ChoDogyu Core 유지 확인
- Package 제거 후 Console Error 없음 확인
- Audio Framework 재설치 검증
- Basic Usage Sample 재Import 검증
- 재설치 후 Sample Scene 실행 검증
- 개발 프로젝트 및 로컬 Package 경로 비의존 검증

#### Documentation

- Package README 추가
- CHANGELOG 추가
- 상세 Documentation 추가
- 설치 방법 문서화
- Runtime 구조 문서화
- AudioController Public API 문서화
- AudioId 및 AudioCatalog 규칙 문서화
- BGM Fade 및 Cross Fade 정책 문서화
- SFX Voice 재사용 구조 문서화
- SFX Overflow 정책 문서화
- Volume 계산 규칙 문서화
- Mute 정책 문서화
- Runtime Error Code 문서화
- Audio Catalog Validation 문서화
- Runtime Source 생명주기 문서화
- Scene 생명주기 책임 범위 문서화
- Save Framework 비의존 정책 문서화
- Basic Usage Sample 문서화
- v1.0 책임 범위 문서화
- UPM 독립 설치 및 재설치 검증 결과 문서화