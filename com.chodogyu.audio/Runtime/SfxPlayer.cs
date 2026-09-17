using System;
using System.Collections.Generic;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// 고정된 수의 SfxVoice를 관리하여 SFX 동시 재생과 Voice 제한을 처리합니다.
    /// Voice가 모두 사용 중일 때는 지정된 Overflow 정책에 따라 새 요청을 거절하거나 가장 오래된 Voice를 교체합니다.
    /// </summary>
    internal sealed class SfxPlayer
    {
        private sealed class VoiceSlot
        {
            public SfxVoice Voice { get; }
            public long Sequence { get; set; }

            public VoiceSlot(SfxVoice voice)
            {
                Voice = voice;
            }
        }

        private readonly List<VoiceSlot> slots;
        private readonly SfxOverflowPolicy overflowPolicy;

        private long nextSequence = 1;

        /// <summary>
        /// Player가 관리하는 전체 SFX Voice 수를 반환합니다.
        /// </summary>
        public int VoiceCount => slots.Count;

        /// <summary>
        /// 현재 AudioClip이 할당되어 사용 중인 Voice 수를 반환합니다.
        /// </summary>
        public int ActiveVoiceCount
        {
            get
            {
                int count = 0;

                foreach (VoiceSlot slot in slots)
                {
                    if (slot.Voice.IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Voice가 모두 사용 중일 때 적용되는 Overflow 정책을 반환합니다.
        /// </summary>
        public SfxOverflowPolicy OverflowPolicy => overflowPolicy;

        /// <summary>
        /// 지정된 AudioSource들을 고정 SFX Voice로 구성합니다.
        /// AudioSource는 한 개 이상이어야 하며 동일한 Source를 중복으로 전달할 수 없습니다.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// AudioSource 목록 또는 Volume State가 null인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// AudioSource 목록이 비어 있거나 null 또는 중복된 Source가 포함된 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 정의되지 않은 Overflow 정책을 전달한 경우 발생합니다.
        /// </exception>
        public SfxPlayer(IReadOnlyList<AudioSource> sources, AudioVolumeState volumeState, SfxOverflowPolicy overflowPolicy)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            if (volumeState == null)
            {
                throw new ArgumentNullException(nameof(volumeState));
            }

            if (sources.Count == 0)
            {
                throw new ArgumentException("SFX Player에는 한 개 이상의 AudioSource가 필요합니다.", nameof(sources));
            }

            if (!Enum.IsDefined(typeof(SfxOverflowPolicy), overflowPolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(overflowPolicy));
            }

            HashSet<AudioSource> uniqueSources = new HashSet<AudioSource>();
            slots = new List<VoiceSlot>(sources.Count);

            for (int i = 0; i < sources.Count; i++)
            {
                AudioSource source = sources[i];

                if (source == null)
                {
                    throw new ArgumentException($"SFX AudioSource 목록의 {i}번 항목이 null입니다.", nameof(sources));
                }

                if (!uniqueSources.Add(source))
                {
                    throw new ArgumentException("동일한 AudioSource를 여러 SFX Voice에 사용할 수 없습니다.", nameof(sources));
                }

                slots.Add(new VoiceSlot(new SfxVoice(source, volumeState)));
            }

            this.overflowPolicy = overflowPolicy;
        }

        /// <summary>
        /// 사용 가능한 Voice를 찾아 지정된 AudioClip을 SFX로 재생합니다.
        /// 동일한 AudioClip도 서로 다른 Voice를 사용하여 동시에 재생할 수 있습니다.
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

            VoiceSlot slot = FindAvailableSlot();

            if (slot == null)
            {
                if (overflowPolicy == SfxOverflowPolicy.RejectNew)
                {
                    return Result.Failure(new ResultError(AudioErrorCodes.NoAvailableSfxVoice, "사용 가능한 SFX Voice가 없습니다."));
                }

                slot = FindOldestActiveSlot();
            }

            Result result = slot.Voice.Play(clip, volumeScale);

            if (result.IsFailure)
            {
                return result;
            }

            slot.Sequence = nextSequence++;
            return Result.Success();
        }

        /// <summary>
        /// 현재 사용 중인 모든 SFX Voice를 Pause 상태로 전환합니다.
        /// </summary>
        public void PauseAll()
        {
            foreach (VoiceSlot slot in slots)
            {
                slot.Voice.Pause();
            }
        }

        /// <summary>
        /// Pause 상태인 모든 SFX Voice를 다시 재생합니다.
        /// </summary>
        public void ResumeAll()
        {
            foreach (VoiceSlot slot in slots)
            {
                slot.Voice.Resume();
            }
        }

        /// <summary>
        /// 모든 SFX를 즉시 중지하고 전체 Voice를 사용 가능한 상태로 초기화합니다.
        /// </summary>
        public void StopAll()
        {
            foreach (VoiceSlot slot in slots)
            {
                slot.Voice.Stop();
                slot.Sequence = 0;
            }
        }

        /// <summary>
        /// 최신 Master/SFX Volume과 Mute 상태를 모든 활성 Voice에 다시 적용합니다.
        /// </summary>
        public void RefreshVolume()
        {
            foreach (VoiceSlot slot in slots)
            {
                slot.Voice.RefreshVolume();
            }
        }

        /// <summary>
        /// 자연스럽게 재생이 종료된 Voice를 정리하여 다시 사용할 수 있도록 합니다.
        /// </summary>
        public void Tick()
        {
            foreach (VoiceSlot slot in slots)
            {
                slot.Voice.Tick();

                if (!slot.Voice.IsActive)
                {
                    slot.Sequence = 0;
                }
            }
        }

        private VoiceSlot FindAvailableSlot()
        {
            foreach (VoiceSlot slot in slots)
            {
                if (!slot.Voice.IsActive)
                {
                    return slot;
                }
            }

            return null;
        }

        private VoiceSlot FindOldestActiveSlot()
        {
            VoiceSlot oldest = null;

            foreach (VoiceSlot slot in slots)
            {
                if (!slot.Voice.IsActive)
                {
                    continue;
                }

                if (oldest == null || slot.Sequence < oldest.Sequence)
                {
                    oldest = slot;
                }
            }

            return oldest;
        }
    }
}