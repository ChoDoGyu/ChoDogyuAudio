using System;
using System.Collections.Generic;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// BGM과 SFX 재생, Volume, Mute 상태를 통합 관리하는 Audio Framework의 Runtime 진입점입니다.
    /// 직접 AudioClip을 사용하거나 AudioCatalog의 AudioId를 통해 오디오를 재생할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioController : MonoBehaviour
    {
        [SerializeField]
        private AudioCatalog catalog;

        [SerializeField]
        [Min(1)]
        private int sfxVoiceCount = 16;

        [SerializeField]
        private SfxOverflowPolicy sfxOverflowPolicy = SfxOverflowPolicy.StopOldest;

        private readonly AudioVolumeState volumeState = new AudioVolumeState();

        private BgmPlayer bgmPlayer;
        private SfxPlayer sfxPlayer;
        private GameObject runtimeSourceRoot;
        private bool initialized;

        /// <summary>
        /// ID 기반 재생에 사용하는 Audio Catalog를 반환합니다.
        /// Catalog는 선택 사항이며 직접 AudioClip 재생에는 필요하지 않습니다.
        /// </summary>
        public AudioCatalog Catalog => catalog;

        /// <summary>
        /// 현재 Master Volume을 반환합니다.
        /// </summary>
        public float MasterVolume => volumeState.MasterVolume;

        /// <summary>
        /// 현재 BGM Volume을 반환합니다.
        /// </summary>
        public float BgmVolume => volumeState.BgmVolume;

        /// <summary>
        /// 현재 SFX Volume을 반환합니다.
        /// </summary>
        public float SfxVolume => volumeState.SfxVolume;

        /// <summary>
        /// Master Mute 상태를 반환합니다.
        /// </summary>
        public bool IsMasterMuted => volumeState.IsMasterMuted;

        /// <summary>
        /// BGM Mute 상태를 반환합니다.
        /// </summary>
        public bool IsBgmMuted => volumeState.IsBgmMuted;

        /// <summary>
        /// SFX Mute 상태를 반환합니다.
        /// </summary>
        public bool IsSfxMuted => volumeState.IsSfxMuted;

        /// <summary>
        /// 현재 BGM으로 관리 중인 AudioClip을 반환합니다.
        /// </summary>
        public AudioClip CurrentBgmClip => bgmPlayer?.CurrentClip;

        /// <summary>
        /// 현재 BGM이 실제 재생 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsBgmPlaying => bgmPlayer != null && bgmPlayer.IsPlaying;

        /// <summary>
        /// 현재 BGM이 Pause 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsBgmPaused => bgmPlayer != null && bgmPlayer.IsPaused;

        /// <summary>
        /// 현재 BGM Fade 또는 Cross Fade가 진행 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsBgmFading => bgmPlayer != null && bgmPlayer.IsFading;

        /// <summary>
        /// Controller가 사용하는 고정 SFX Voice 수를 반환합니다.
        /// </summary>
        public int SfxVoiceCount => Mathf.Max(1, sfxVoiceCount);

        /// <summary>
        /// 현재 사용 중인 SFX Voice 수를 반환합니다.
        /// </summary>
        public int ActiveSfxVoiceCount
        {
            get
            {
                EnsureInitialized();
                return sfxPlayer.ActiveVoiceCount;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (!initialized)
            {
                return;
            }

            if (runtimeSourceRoot != null)
            {
                bgmPlayer.Stop();
                sfxPlayer.StopAll();

                if (Application.isPlaying)
                {
                    Destroy(runtimeSourceRoot);
                }
                else
                {
                    DestroyImmediate(runtimeSourceRoot);
                }
            }

            runtimeSourceRoot = null;
            bgmPlayer = null;
            sfxPlayer = null;
            initialized = false;
        }

        /// <summary>
        /// 지정된 AudioClip을 BGM으로 재생합니다.
        /// 기존 BGM이 있고 Fade Duration이 양수이면 Cross Fade가 적용됩니다.
        /// </summary>
        public Result PlayBgm(AudioClip clip, float fadeDuration = 0f, bool loop = true)
        {
            EnsureInitialized();
            return bgmPlayer.Play(clip, 1f, loop, fadeDuration);
        }

        /// <summary>
        /// Audio Catalog에서 지정된 ID의 BGM을 찾아 재생합니다.
        /// Catalog Entry의 Volume Scale이 BGM 출력에 함께 적용됩니다.
        /// </summary>
        public Result PlayBgm(AudioId id, float fadeDuration = 0f, bool loop = true)
        {
            EnsureInitialized();

            Result<AudioEntry> entryResult = ResolveEntry(id, AudioCategory.Bgm);

            if (entryResult.IsFailure)
            {
                return Result.Failure(entryResult.Error);
            }

            AudioEntry entry = entryResult.Value;
            return bgmPlayer.Play(entry.Clip, entry.VolumeScale, loop, fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 중지합니다.
        /// Fade Duration이 양수이면 Fade Out 완료 후 중지합니다.
        /// </summary>
        public void StopBgm(float fadeDuration = 0f)
        {
            EnsureInitialized();
            bgmPlayer.Stop(fadeDuration);
        }

        /// <summary>
        /// 현재 BGM을 일시정지합니다.
        /// </summary>
        public void PauseBgm()
        {
            EnsureInitialized();
            bgmPlayer.Pause();
        }

        /// <summary>
        /// Pause 상태인 BGM을 다시 재생합니다.
        /// </summary>
        public void ResumeBgm()
        {
            EnsureInitialized();
            bgmPlayer.Resume();
        }

        /// <summary>
        /// 지정된 AudioClip을 사용 가능한 SFX Voice에서 재생합니다.
        /// </summary>
        public Result PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            EnsureInitialized();
            return sfxPlayer.Play(clip, volumeScale);
        }

        /// <summary>
        /// Audio Catalog에서 지정된 ID의 SFX를 찾아 재생합니다.
        /// Entry Volume Scale과 요청 Volume Scale을 각각 적용한 뒤 최종 출력 배율을 계산합니다.
        /// </summary>
        public Result PlaySfx(AudioId id, float volumeScale = 1f)
        {
            EnsureInitialized();

            Result<AudioEntry> entryResult = ResolveEntry(id, AudioCategory.Sfx);

            if (entryResult.IsFailure)
            {
                return Result.Failure(entryResult.Error);
            }

            AudioEntry entry = entryResult.Value;
            float normalizedPlayVolumeScale = NormalizeVolumeScale(volumeScale);
            float finalVolumeScale = entry.VolumeScale * normalizedPlayVolumeScale;

            return sfxPlayer.Play(entry.Clip, finalVolumeScale);
        }

        /// <summary>
        /// 현재 사용 중인 모든 SFX Voice를 즉시 중지합니다.
        /// </summary>
        public void StopAllSfx()
        {
            EnsureInitialized();
            sfxPlayer.StopAll();
        }

        /// <summary>
        /// 현재 사용 중인 모든 SFX Voice를 일시정지합니다.
        /// </summary>
        public void PauseAllSfx()
        {
            EnsureInitialized();
            sfxPlayer.PauseAll();
        }

        /// <summary>
        /// Pause 상태인 모든 SFX Voice를 다시 재생합니다.
        /// </summary>
        public void ResumeAllSfx()
        {
            EnsureInitialized();
            sfxPlayer.ResumeAll();
        }

        /// <summary>
        /// Master Volume을 설정하고 현재 BGM과 SFX에 즉시 반영합니다.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volumeState.SetMasterVolume(volume);
            RefreshAllVolumes();
        }

        /// <summary>
        /// BGM Volume을 설정하고 현재 BGM에 즉시 반영합니다.
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            volumeState.SetBgmVolume(volume);

            if (bgmPlayer != null)
            {
                bgmPlayer.RefreshVolume();
            }
        }

        /// <summary>
        /// SFX Volume을 설정하고 현재 SFX에 즉시 반영합니다.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            volumeState.SetSfxVolume(volume);

            if (sfxPlayer != null)
            {
                sfxPlayer.RefreshVolume();
            }
        }

        /// <summary>
        /// Master Mute 상태를 설정하고 현재 BGM과 SFX에 즉시 반영합니다.
        /// </summary>
        public void SetMasterMuted(bool muted)
        {
            volumeState.SetMasterMuted(muted);
            RefreshAllVolumes();
        }

        /// <summary>
        /// BGM Mute 상태를 설정하고 현재 BGM에 즉시 반영합니다.
        /// </summary>
        public void SetBgmMuted(bool muted)
        {
            volumeState.SetBgmMuted(muted);

            if (bgmPlayer != null)
            {
                bgmPlayer.RefreshVolume();
            }
        }

        /// <summary>
        /// SFX Mute 상태를 설정하고 현재 SFX에 즉시 반영합니다.
        /// </summary>
        public void SetSfxMuted(bool muted)
        {
            volumeState.SetSfxMuted(muted);

            if (sfxPlayer != null)
            {
                sfxPlayer.RefreshVolume();
            }
        }

        /// <summary>
        /// BGM과 모든 SFX를 즉시 중지합니다.
        /// </summary>
        public void StopAll()
        {
            EnsureInitialized();
            bgmPlayer.Stop();
            sfxPlayer.StopAll();
        }

        /// <summary>
        /// BGM 전환과 SFX Voice 종료 상태를 지정된 시간 기준으로 갱신합니다.
        /// Runtime에서는 unscaledDeltaTime을 사용하여 자동 호출됩니다.
        /// </summary>
        internal void Tick(float deltaTime)
        {
            if (!initialized)
            {
                return;
            }

            bgmPlayer.Tick(deltaTime);
            sfxPlayer.Tick();
        }

        internal void SetCatalog(AudioCatalog value)
        {
            catalog = value;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            runtimeSourceRoot = new GameObject("Audio Runtime Sources");
            runtimeSourceRoot.transform.SetParent(transform, false);

            AudioSource bgmSourceA = runtimeSourceRoot.AddComponent<AudioSource>();
            AudioSource bgmSourceB = runtimeSourceRoot.AddComponent<AudioSource>();

            List<AudioSource> sfxSources = new List<AudioSource>(SfxVoiceCount);

            for (int i = 0; i < SfxVoiceCount; i++)
            {
                sfxSources.Add(runtimeSourceRoot.AddComponent<AudioSource>());
            }

            SfxOverflowPolicy policy = Enum.IsDefined(typeof(SfxOverflowPolicy), sfxOverflowPolicy) ? sfxOverflowPolicy : SfxOverflowPolicy.StopOldest;

            bgmPlayer = new BgmPlayer(bgmSourceA, bgmSourceB, volumeState);
            sfxPlayer = new SfxPlayer(sfxSources, volumeState, policy);

            initialized = true;
        }

        private Result<AudioEntry> ResolveEntry(AudioId id, AudioCategory expectedCategory)
        {
            if (id.IsEmpty)
            {
                return Result<AudioEntry>.Failure(new ResultError(AudioErrorCodes.InvalidId, "Audio ID는 비어 있을 수 없습니다."));
            }

            if (catalog == null)
            {
                return Result<AudioEntry>.Failure(new ResultError(AudioErrorCodes.CatalogNotAssigned, "AudioController에 Audio Catalog가 지정되지 않았습니다."));
            }

            Result<AudioEntry> entryResult = catalog.Get(id);

            if (entryResult.IsFailure)
            {
                return Result<AudioEntry>.Failure(entryResult.Error);
            }

            AudioEntry entry = entryResult.Value;

            if (entry.Category != expectedCategory)
            {
                return Result<AudioEntry>.Failure(new ResultError(AudioErrorCodes.CategoryMismatch, $"Audio ID '{id}'의 Category '{entry.Category}'는 요청한 '{expectedCategory}' 재생 방식과 일치하지 않습니다."));
            }

            return Result<AudioEntry>.Success(entry);
        }

        private void RefreshAllVolumes()
        {
            if (bgmPlayer != null)
            {
                bgmPlayer.RefreshVolume();
            }

            if (sfxPlayer != null)
            {
                sfxPlayer.RefreshVolume();
            }
        }

        private static float NormalizeVolumeScale(float volumeScale)
        {
            if (float.IsNaN(volumeScale))
            {
                return 0f;
            }

            return Mathf.Clamp01(volumeScale);
        }
    }
}