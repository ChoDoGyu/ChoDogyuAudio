using System;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// BGM 재생에 사용되는 AudioSource를 관리하고 기본 재생 및 Fade 상태를 제어합니다.
    /// 두 개의 AudioSource를 보유하며 두 번째 Source는 이후 Cross Fade에 사용됩니다.
    /// </summary>
    internal sealed class BgmPlayer
    {
        private readonly AudioSource sourceA;
        private readonly AudioSource sourceB;
        private readonly AudioVolumeState volumeState;

        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private float currentVolumeScale = 1f;
        private bool isPaused;

        private bool isFading;
        private bool stopAfterFade;
        private float fadeElapsed;
        private float fadeDuration;
        private float fadeStartGain;
        private float fadeTargetGain = 1f;
        private float fadeGain = 1f;

        /// <summary>
        /// 현재 BGM으로 지정된 AudioClip을 반환합니다.
        /// 재생 중인 BGM이 없으면 null을 반환합니다.
        /// </summary>
        public AudioClip CurrentClip => activeSource.clip;

        /// <summary>
        /// 현재 활성 AudioSource가 실제로 BGM을 재생 중인지 여부를 반환합니다.
        /// Pause 상태에서는 false를 반환합니다.
        /// </summary>
        public bool IsPlaying => CurrentClip != null && !isPaused && activeSource.isPlaying;

        /// <summary>
        /// 현재 BGM이 Pause 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsPaused => CurrentClip != null && isPaused;

        /// <summary>
        /// 현재 BGM이 Fade 처리 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsFading => isFading;

        /// <summary>
        /// 현재 BGM에 적용되는 Fade Gain을 반환합니다.
        /// </summary>
        internal float FadeGain => fadeGain;

        /// <summary>
        /// BGM 재생에 사용되는 두 AudioSource를 지정합니다.
        /// 두 Source는 서로 다른 인스턴스여야 합니다.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// AudioSource 또는 Volume State가 null인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 동일한 AudioSource를 두 위치에 전달한 경우 발생합니다.
        /// </exception>
        public BgmPlayer(AudioSource sourceA, AudioSource sourceB, AudioVolumeState volumeState)
        {
            this.sourceA = sourceA != null ? sourceA : throw new ArgumentNullException(nameof(sourceA));
            this.sourceB = sourceB != null ? sourceB : throw new ArgumentNullException(nameof(sourceB));
            this.volumeState = volumeState ?? throw new ArgumentNullException(nameof(volumeState));

            if (ReferenceEquals(sourceA, sourceB))
            {
                throw new ArgumentException("BGM 재생에 사용하는 두 AudioSource는 서로 다른 인스턴스여야 합니다.");
            }

            ConfigureSource(this.sourceA);
            ConfigureSource(this.sourceB);

            activeSource = this.sourceA;
            inactiveSource = this.sourceB;
        }

        /// <summary>
        /// 지정된 AudioClip을 BGM으로 재생합니다.
        /// 현재 BGM과 동일한 Clip이면 일반적으로 재시작하지 않으며,
        /// Fade Out으로 정지 예약된 상태라면 최신 Play 요청을 우선하여 정지 예약을 취소합니다.
        /// </summary>
        /// <param name="clip">재생할 BGM AudioClip입니다.</param>
        /// <param name="volumeScale">해당 BGM에 추가로 적용할 볼륨 배율입니다.</param>
        /// <param name="loop">BGM 반복 재생 여부입니다.</param>
        /// <param name="fadeDuration">0보다 크면 해당 시간 동안 Fade In합니다.</param>
        /// <returns>재생 요청의 성공 또는 실패 결과입니다.</returns>
        public Result Play(AudioClip clip, float volumeScale = 1f, bool loop = true, float fadeDuration = 0f)
        {
            if (clip == null)
            {
                return Result.Failure(new ResultError(
                    AudioErrorCodes.InvalidClip,
                    "재생할 BGM AudioClip이 지정되지 않았습니다."));
            }

            if (ReferenceEquals(CurrentClip, clip))
            {
                if (isFading && stopAfterFade)
                {
                    CancelPendingStop(fadeDuration);
                }

                return Result.Success();
            }

            StopSources();
            ResetFadeState();

            currentVolumeScale = NormalizeVolumeScale(volumeScale);
            isPaused = false;

            activeSource.clip = clip;
            activeSource.loop = loop;

            if (IsPositiveFinite(fadeDuration))
            {
                fadeGain = 0f;
                ApplyCurrentVolume();
                activeSource.Play();

                StartFade(0f, 1f, fadeDuration, false);

                return Result.Success();
            }

            fadeGain = 1f;
            ApplyCurrentVolume();
            activeSource.Play();

            return Result.Success();
        }

        /// <summary>
        /// 현재 BGM을 Pause 상태로 전환합니다.
        /// 재생 중인 BGM이 없거나 이미 Pause 상태라면 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void Pause()
        {
            if (CurrentClip == null || isPaused)
            {
                return;
            }

            activeSource.Pause();
            isPaused = true;
        }

        /// <summary>
        /// Pause 상태인 현재 BGM을 다시 재생합니다.
        /// Pause 상태가 아니라면 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void Resume()
        {
            if (CurrentClip == null || !isPaused)
            {
                return;
            }

            ApplyCurrentVolume();
            activeSource.UnPause();
            isPaused = false;
        }

        /// <summary>
        /// 현재 BGM을 중지합니다.
        /// Fade Duration이 0보다 크면 현재 Gain에서 0까지 Fade Out한 뒤 중지합니다.
        /// 진행 중인 Fade가 있다면 최신 Stop 요청의 Duration으로 다시 계산합니다.
        /// </summary>
        public void Stop(float fadeDuration = 0f)
        {
            if (CurrentClip == null)
            {
                ResetPlaybackState();
                return;
            }

            if (!IsPositiveFinite(fadeDuration))
            {
                StopImmediately();
                return;
            }

            isPaused = false;
            activeSource.UnPause();

            StartFade(fadeGain, 0f, fadeDuration, true);
        }

        /// <summary>
        /// 현재 Volume State를 재생 중인 BGM AudioSource에 다시 적용합니다.
        /// BGM이 없는 경우에도 안전하게 호출할 수 있습니다.
        /// </summary>
        public void RefreshVolume()
        {
            if (CurrentClip == null)
            {
                return;
            }

            ApplyCurrentVolume();
        }

        /// <summary>
        /// 진행 중인 BGM Fade 상태를 지정된 시간만큼 갱신합니다.
        /// 음수, 0, NaN 또는 Infinity 값은 진행 시간으로 사용하지 않습니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!isFading || isPaused)
            {
                return;
            }

            if (!IsPositiveFinite(deltaTime))
            {
                return;
            }

            fadeElapsed = Mathf.Min(fadeElapsed + deltaTime, fadeDuration);

            float progress = fadeDuration <= 0f
                ? 1f
                : fadeElapsed / fadeDuration;

            fadeGain = Mathf.Lerp(fadeStartGain, fadeTargetGain, progress);
            ApplyCurrentVolume();

            if (fadeElapsed < fadeDuration)
            {
                return;
            }

            bool shouldStop = stopAfterFade;

            isFading = false;
            stopAfterFade = false;
            fadeElapsed = 0f;

            fadeGain = fadeTargetGain;
            ApplyCurrentVolume();

            if (shouldStop)
            {
                StopImmediately();
            }
        }

        private void CancelPendingStop(float requestedFadeDuration)
        {
            stopAfterFade = false;
            isPaused = false;
            activeSource.UnPause();

            if (IsPositiveFinite(requestedFadeDuration))
            {
                StartFade(fadeGain, 1f, requestedFadeDuration, false);
                return;
            }

            ResetFadeState();
            ApplyCurrentVolume();
        }

        private void StartFade(float startGain, float targetGain, float duration, bool shouldStopAfterFade)
        {
            fadeStartGain = Mathf.Clamp01(startGain);
            fadeTargetGain = Mathf.Clamp01(targetGain);
            fadeDuration = duration;
            fadeElapsed = 0f;
            stopAfterFade = shouldStopAfterFade;
            isFading = true;

            fadeGain = fadeStartGain;
            ApplyCurrentVolume();
        }

        private void ApplyCurrentVolume()
        {
            activeSource.volume = volumeState.BgmGain * currentVolumeScale * fadeGain;
        }

        private void StopImmediately()
        {
            StopSources();
            ResetPlaybackState();
        }

        private void StopSources()
        {
            sourceA.Stop();
            sourceB.Stop();

            sourceA.clip = null;
            sourceB.clip = null;
        }

        private void ResetPlaybackState()
        {
            activeSource = sourceA;
            inactiveSource = sourceB;

            currentVolumeScale = 1f;
            isPaused = false;

            ResetFadeState();
        }

        private void ResetFadeState()
        {
            isFading = false;
            stopAfterFade = false;

            fadeElapsed = 0f;
            fadeDuration = 0f;
            fadeStartGain = 1f;
            fadeTargetGain = 1f;
            fadeGain = 1f;
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private static float NormalizeVolumeScale(float volumeScale)
        {
            if (float.IsNaN(volumeScale))
            {
                return 0f;
            }

            return Mathf.Clamp01(volumeScale);
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}