using System;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// 두 개의 AudioSource를 사용하여 BGM 재생, Fade In/Out과 Cross Fade를 관리합니다.
    /// 재생 요청에 따라 Active Source와 Inactive Source의 역할을 교환하여 AudioSource를 재사용합니다.
    /// </summary>
    internal sealed class BgmPlayer
    {
        private enum TransitionMode
        {
            None = 0,
            FadeIn = 1,
            FadeOut = 2,
            CrossFade = 3
        }

        private readonly AudioSource sourceA;
        private readonly AudioSource sourceB;
        private readonly AudioVolumeState volumeState;

        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private float activeVolumeScale = 1f;
        private float inactiveVolumeScale = 1f;

        private float activeGain = 1f;
        private float inactiveGain;

        private bool isPaused;

        private TransitionMode transitionMode;
        private bool stopAfterTransition;

        private float transitionElapsed;
        private float transitionDuration;

        private float activeStartGain;
        private float activeTargetGain = 1f;

        private float inactiveStartGain;
        private float inactiveTargetGain;

        /// <summary>
        /// 현재 BGM으로 취급되는 AudioClip을 반환합니다.
        /// Cross Fade 중에는 새로 요청된 BGM을 반환합니다.
        /// </summary>
        public AudioClip CurrentClip => activeSource.clip;

        /// <summary>
        /// 현재 Active Source가 실제 재생 중인지 여부를 반환합니다.
        /// Pause 상태에서는 false를 반환합니다.
        /// </summary>
        public bool IsPlaying => CurrentClip != null && !isPaused && activeSource.isPlaying;

        /// <summary>
        /// 현재 BGM이 Pause 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsPaused => CurrentClip != null && isPaused;

        /// <summary>
        /// Fade In, Fade Out 또는 Cross Fade가 진행 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsFading => transitionMode != TransitionMode.None;

        /// <summary>
        /// 현재 Active BGM에 적용되는 Fade Gain을 반환합니다.
        /// </summary>
        internal float FadeGain => activeGain;

        /// <summary>
        /// 현재 두 BGM 사이의 Cross Fade가 진행 중인지 여부를 반환합니다.
        /// </summary>
        internal bool IsCrossFading => transitionMode == TransitionMode.CrossFade;

        /// <summary>
        /// BGM 재생에 사용할 두 AudioSource와 Volume State를 지정합니다.
        /// 두 AudioSource는 서로 다른 인스턴스여야 합니다.
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
        /// 기존 BGM이 있고 Fade Duration이 양수라면 두 AudioSource를 이용해 Cross Fade합니다.
        /// </summary>
        /// <param name="clip">재생할 BGM AudioClip입니다.</param>
        /// <param name="volumeScale">해당 BGM에 추가로 적용할 볼륨 배율입니다.</param>
        /// <param name="loop">BGM 반복 재생 여부입니다.</param>
        /// <param name="fadeDuration">0보다 크면 Fade In 또는 Cross Fade에 사용할 시간입니다.</param>
        /// <returns>재생 요청의 성공 또는 실패 결과입니다.</returns>
        public Result Play(AudioClip clip, float volumeScale = 1f, bool loop = true, float fadeDuration = 0f)
        {
            if (clip == null)
            {
                return Result.Failure(new ResultError(AudioErrorCodes.InvalidClip, "재생할 BGM AudioClip이 지정되지 않았습니다."));
            }

            if (ReferenceEquals(CurrentClip, clip))
            {
                if (transitionMode == TransitionMode.FadeOut)
                {
                    CancelPendingStop(fadeDuration);
                }

                return Result.Success();
            }

            float normalizedVolumeScale = NormalizeVolumeScale(volumeScale);

            if (CurrentClip == null)
            {
                PlayFromSilence(clip, normalizedVolumeScale, loop, fadeDuration);
                return Result.Success();
            }

            if (IsPositiveFinite(fadeDuration))
            {
                StartCrossFade(clip, normalizedVolumeScale, loop, fadeDuration);
                return Result.Success();
            }

            PlayImmediately(clip, normalizedVolumeScale, loop);
            return Result.Success();
        }

        /// <summary>
        /// 현재 BGM을 Pause 상태로 전환합니다.
        /// Cross Fade 중이라면 두 AudioSource를 함께 일시정지합니다.
        /// </summary>
        public void Pause()
        {
            if (CurrentClip == null || isPaused)
            {
                return;
            }

            activeSource.Pause();

            if (inactiveSource.clip != null)
            {
                inactiveSource.Pause();
            }

            isPaused = true;
        }

        /// <summary>
        /// Pause 상태인 BGM을 다시 재생합니다.
        /// Cross Fade 중이라면 두 AudioSource를 함께 재개합니다.
        /// </summary>
        public void Resume()
        {
            if (CurrentClip == null || !isPaused)
            {
                return;
            }

            ApplyVolumes();
            activeSource.UnPause();

            if (inactiveSource.clip != null)
            {
                inactiveSource.UnPause();
            }

            isPaused = false;
        }

        /// <summary>
        /// 현재 BGM을 중지합니다.
        /// Fade Duration이 양수라면 현재 활성 Gain부터 0까지 Fade Out한 뒤 모든 BGM을 정지합니다.
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

            if (inactiveSource.clip != null)
            {
                inactiveSource.UnPause();
            }

            StartTransition(TransitionMode.FadeOut, activeGain, 0f, inactiveGain, 0f, fadeDuration, true);
        }

        /// <summary>
        /// 현재 Volume State를 재생 중인 모든 BGM AudioSource에 다시 적용합니다.
        /// </summary>
        public void RefreshVolume()
        {
            if (CurrentClip == null)
            {
                return;
            }

            ApplyVolumes();
        }

        /// <summary>
        /// 진행 중인 Fade 또는 Cross Fade 상태를 지정된 시간만큼 갱신합니다.
        /// 0 이하, NaN 또는 Infinity 값은 무시합니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (transitionMode == TransitionMode.None || isPaused)
            {
                return;
            }

            if (!IsPositiveFinite(deltaTime))
            {
                return;
            }

            transitionElapsed = Mathf.Min(transitionElapsed + deltaTime, transitionDuration);

            float progress = transitionDuration <= 0f ? 1f : transitionElapsed / transitionDuration;

            activeGain = Mathf.Lerp(activeStartGain, activeTargetGain, progress);
            inactiveGain = Mathf.Lerp(inactiveStartGain, inactiveTargetGain, progress);

            ApplyVolumes();

            if (transitionElapsed < transitionDuration)
            {
                return;
            }

            TransitionMode completedMode = transitionMode;
            bool shouldStop = stopAfterTransition;

            activeGain = activeTargetGain;
            inactiveGain = inactiveTargetGain;

            ClearTransitionState();
            ApplyVolumes();

            if (shouldStop)
            {
                StopImmediately();
                return;
            }

            if (completedMode == TransitionMode.CrossFade)
            {
                StopAndClearInactiveSource();
            }
        }

        private void PlayFromSilence(AudioClip clip, float volumeScale, bool loop, float fadeDuration)
        {
            StopSources();
            ClearTransitionState();

            isPaused = false;

            activeVolumeScale = volumeScale;
            inactiveVolumeScale = 1f;

            activeSource.clip = clip;
            activeSource.loop = loop;

            inactiveGain = 0f;

            if (IsPositiveFinite(fadeDuration))
            {
                activeGain = 0f;
                ApplyVolumes();
                activeSource.Play();

                StartTransition(TransitionMode.FadeIn, 0f, 1f, 0f, 0f, fadeDuration, false);
                return;
            }

            activeGain = 1f;
            ApplyVolumes();
            activeSource.Play();
        }

        private void PlayImmediately(AudioClip clip, float volumeScale, bool loop)
        {
            StopSources();
            ClearTransitionState();

            isPaused = false;

            activeVolumeScale = volumeScale;
            inactiveVolumeScale = 1f;

            activeGain = 1f;
            inactiveGain = 0f;

            activeSource.clip = clip;
            activeSource.loop = loop;

            ApplyVolumes();
            activeSource.Play();
        }

        private void StartCrossFade(AudioClip clip, float volumeScale, bool loop, float duration)
        {
            StopAndClearInactiveSource();

            AudioSource outgoingSource = activeSource;
            AudioSource incomingSource = inactiveSource;

            float outgoingGain = activeGain;
            float outgoingVolumeScale = activeVolumeScale;

            outgoingSource.UnPause();

            incomingSource.clip = clip;
            incomingSource.loop = loop;

            activeSource = incomingSource;
            inactiveSource = outgoingSource;

            activeVolumeScale = volumeScale;
            inactiveVolumeScale = outgoingVolumeScale;

            activeGain = 0f;
            inactiveGain = outgoingGain;

            isPaused = false;

            ApplyVolumes();
            activeSource.Play();

            StartTransition(TransitionMode.CrossFade, 0f, 1f, inactiveGain, 0f, duration, false);
        }

        private void CancelPendingStop(float requestedFadeDuration)
        {
            stopAfterTransition = false;
            isPaused = false;

            activeSource.UnPause();
            StopAndClearInactiveSource();

            if (IsPositiveFinite(requestedFadeDuration))
            {
                StartTransition(TransitionMode.FadeIn, activeGain, 1f, 0f, 0f, requestedFadeDuration, false);
                return;
            }

            activeGain = 1f;
            inactiveGain = 0f;

            ClearTransitionState();
            ApplyVolumes();
        }

        private void StartTransition(TransitionMode mode, float newActiveStartGain, float newActiveTargetGain, float newInactiveStartGain, float newInactiveTargetGain, float duration, bool shouldStopAfterTransition)
        {
            transitionMode = mode;
            transitionElapsed = 0f;
            transitionDuration = duration;

            activeStartGain = Mathf.Clamp01(newActiveStartGain);
            activeTargetGain = Mathf.Clamp01(newActiveTargetGain);

            inactiveStartGain = Mathf.Clamp01(newInactiveStartGain);
            inactiveTargetGain = Mathf.Clamp01(newInactiveTargetGain);

            stopAfterTransition = shouldStopAfterTransition;

            activeGain = activeStartGain;
            inactiveGain = inactiveStartGain;

            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (activeSource.clip != null)
            {
                activeSource.volume = volumeState.BgmGain * activeVolumeScale * activeGain;
            }

            if (inactiveSource.clip != null)
            {
                inactiveSource.volume = volumeState.BgmGain * inactiveVolumeScale * inactiveGain;
            }
        }

        private void StopAndClearInactiveSource()
        {
            inactiveSource.Stop();
            inactiveSource.clip = null;

            inactiveGain = 0f;
            inactiveVolumeScale = 1f;
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

            activeVolumeScale = 1f;
            inactiveVolumeScale = 1f;

            activeGain = 1f;
            inactiveGain = 0f;

            isPaused = false;

            ClearTransitionState();
        }

        private void ClearTransitionState()
        {
            transitionMode = TransitionMode.None;
            stopAfterTransition = false;

            transitionElapsed = 0f;
            transitionDuration = 0f;

            activeStartGain = activeGain;
            activeTargetGain = activeGain;

            inactiveStartGain = inactiveGain;
            inactiveTargetGain = inactiveGain;
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