using System;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// 하나의 AudioSource를 재사용하여 SFX 한 개의 재생 상태를 관리합니다.
    /// SfxPlayer는 여러 SfxVoice를 조합하여 동시 재생과 Voice 제한을 관리합니다.
    /// </summary>
    internal sealed class SfxVoice
    {
        private readonly AudioSource source;
        private readonly AudioVolumeState volumeState;

        private float currentVolumeScale = 1f;
        private bool isPaused;

        /// <summary>
        /// 현재 Voice에 할당된 AudioClip을 반환합니다.
        /// 사용 중이지 않으면 null을 반환합니다.
        /// </summary>
        public AudioClip CurrentClip => source.clip;

        /// <summary>
        /// 현재 Voice에 AudioClip이 할당되어 있는지 여부를 반환합니다.
        /// Pause 상태도 사용 중인 Voice로 취급합니다.
        /// </summary>
        public bool IsActive => CurrentClip != null;

        /// <summary>
        /// 현재 Voice가 실제 재생 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsPlaying => CurrentClip != null && !isPaused && source.isPlaying;

        /// <summary>
        /// 현재 Voice가 Pause 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsPaused => CurrentClip != null && isPaused;

        /// <summary>
        /// SFX 재생에 사용할 AudioSource와 Volume State를 지정합니다.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// AudioSource 또는 Volume State가 null인 경우 발생합니다.
        /// </exception>
        public SfxVoice(AudioSource source, AudioVolumeState volumeState)
        {
            this.source = source != null ? source : throw new ArgumentNullException(nameof(source));
            this.volumeState = volumeState ?? throw new ArgumentNullException(nameof(volumeState));

            ConfigureSource(this.source);
        }

        /// <summary>
        /// 지정된 AudioClip을 SFX로 재생합니다.
        /// 이미 사용 중인 Voice라면 기존 재생을 중지하고 새 Clip으로 교체합니다.
        /// </summary>
        /// <param name="clip">재생할 SFX AudioClip입니다.</param>
        /// <param name="volumeScale">이번 재생에 추가로 적용할 볼륨 배율입니다.</param>
        /// <returns>재생 요청의 성공 또는 실패 결과입니다.</returns>
        public Result Play(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                return Result.Failure(new ResultError(AudioErrorCodes.InvalidClip, "재생할 SFX AudioClip이 지정되지 않았습니다."));
            }

            source.Stop();

            currentVolumeScale = NormalizeVolumeScale(volumeScale);
            isPaused = false;

            source.clip = clip;
            source.loop = false;

            ApplyCurrentVolume();
            source.Play();

            return Result.Success();
        }

        /// <summary>
        /// 현재 SFX를 Pause 상태로 전환합니다.
        /// 사용 중인 Clip이 없거나 이미 Pause 상태라면 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void Pause()
        {
            if (CurrentClip == null || isPaused)
            {
                return;
            }

            source.Pause();
            isPaused = true;
        }

        /// <summary>
        /// Pause 상태인 SFX를 다시 재생합니다.
        /// Pause 상태가 아니라면 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void Resume()
        {
            if (CurrentClip == null || !isPaused)
            {
                return;
            }

            ApplyCurrentVolume();
            source.UnPause();
            isPaused = false;
        }

        /// <summary>
        /// 현재 SFX를 즉시 중지하고 Voice를 비어 있는 상태로 초기화합니다.
        /// </summary>
        public void Stop()
        {
            source.Stop();
            ClearPlaybackState();
        }

        /// <summary>
        /// 최신 Master/SFX Volume과 Mute 상태를 현재 Voice에 다시 적용합니다.
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
        /// 자연스럽게 재생이 종료된 Voice를 비어 있는 상태로 정리합니다.
        /// Pause 상태는 재생 종료로 취급하지 않습니다.
        /// </summary>
        public void Tick()
        {
            if (CurrentClip == null || isPaused || source.isPlaying)
            {
                return;
            }

            ClearPlaybackState();
        }

        private void ApplyCurrentVolume()
        {
            source.volume = volumeState.SfxGain * currentVolumeScale;
        }

        private void ClearPlaybackState()
        {
            source.clip = null;
            currentVolumeScale = 1f;
            isPaused = false;
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
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
    }
}