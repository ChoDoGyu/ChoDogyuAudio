using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// Audio Framework에서 사용하는 Master, BGM, SFX 볼륨과 Mute 상태를 관리합니다.
    /// 사용자 설정 볼륨과 실제 출력 Gain을 분리하여 Mute가 기존 볼륨 값을 변경하지 않도록 합니다.
    /// </summary>
    internal sealed class AudioVolumeState
    {
        /// <summary>
        /// 전체 오디오에 적용되는 Master Volume입니다.
        /// </summary>
        public float MasterVolume { get; private set; } = 1f;

        /// <summary>
        /// BGM 채널에 적용되는 Volume입니다.
        /// </summary>
        public float BgmVolume { get; private set; } = 1f;

        /// <summary>
        /// SFX 채널에 적용되는 Volume입니다.
        /// </summary>
        public float SfxVolume { get; private set; } = 1f;

        /// <summary>
        /// 전체 오디오가 Mute 상태인지 여부입니다.
        /// </summary>
        public bool IsMasterMuted { get; private set; }

        /// <summary>
        /// BGM이 Mute 상태인지 여부입니다.
        /// </summary>
        public bool IsBgmMuted { get; private set; }

        /// <summary>
        /// SFX가 Mute 상태인지 여부입니다.
        /// </summary>
        public bool IsSfxMuted { get; private set; }

        /// <summary>
        /// 현재 Master와 BGM 상태를 반영한 BGM 출력 Gain을 반환합니다.
        /// Entry Volume과 Fade Gain은 포함하지 않습니다.
        /// </summary>
        public float BgmGain => IsMasterMuted || IsBgmMuted ? 0f : MasterVolume * BgmVolume;

        /// <summary>
        /// 현재 Master와 SFX 상태를 반영한 SFX 출력 Gain을 반환합니다.
        /// Entry Volume과 개별 재생 Volume Scale은 포함하지 않습니다.
        /// </summary>
        public float SfxGain => IsMasterMuted || IsSfxMuted ? 0f : MasterVolume * SfxVolume;

        /// <summary>
        /// Master Volume을 설정합니다.
        /// 입력값은 0 이상 1 이하로 제한되며 NaN은 0으로 처리합니다.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = NormalizeVolume(volume);
        }

        /// <summary>
        /// BGM Volume을 설정합니다.
        /// 입력값은 0 이상 1 이하로 제한되며 NaN은 0으로 처리합니다.
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            BgmVolume = NormalizeVolume(volume);
        }

        /// <summary>
        /// SFX Volume을 설정합니다.
        /// 입력값은 0 이상 1 이하로 제한되며 NaN은 0으로 처리합니다.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = NormalizeVolume(volume);
        }

        /// <summary>
        /// 전체 오디오의 Mute 상태를 설정합니다.
        /// 기존 Master, BGM, SFX Volume 값은 변경하지 않습니다.
        /// </summary>
        public void SetMasterMuted(bool muted)
        {
            IsMasterMuted = muted;
        }

        /// <summary>
        /// BGM의 Mute 상태를 설정합니다.
        /// 기존 BGM Volume 값은 변경하지 않습니다.
        /// </summary>
        public void SetBgmMuted(bool muted)
        {
            IsBgmMuted = muted;
        }

        /// <summary>
        /// SFX의 Mute 상태를 설정합니다.
        /// 기존 SFX Volume 값은 변경하지 않습니다.
        /// </summary>
        public void SetSfxMuted(bool muted)
        {
            IsSfxMuted = muted;
        }

        private static float NormalizeVolume(float volume)
        {
            if (float.IsNaN(volume))
            {
                return 0f;
            }

            return Mathf.Clamp01(volume);
        }
    }
}