using System;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// Audio Catalog에 등록되는 하나의 오디오 항목입니다.
    /// ID, 재생 카테고리, AudioClip과 항목별 볼륨 배율을 정의합니다.
    /// </summary>
    [Serializable]
    public sealed class AudioEntry
    {
        [SerializeField]
        private AudioId id;

        [SerializeField]
        private AudioCategory category;

        [SerializeField]
        private AudioClip clip;

        [SerializeField]
        [Range(0f, 1f)]
        private float volumeScale = 1f;

        /// <summary>
        /// Catalog에서 항목을 식별하는 Audio ID를 반환합니다.
        /// </summary>
        public AudioId Id => id;

        /// <summary>
        /// 항목이 BGM인지 SFX인지 나타내는 카테고리를 반환합니다.
        /// </summary>
        public AudioCategory Category => category;

        /// <summary>
        /// 항목에 연결된 AudioClip을 반환합니다.
        /// </summary>
        public AudioClip Clip => clip;

        /// <summary>
        /// 항목별 재생 볼륨 배율을 반환합니다.
        /// 유효한 Catalog에서는 0 이상 1 이하의 값을 사용합니다.
        /// </summary>
        public float VolumeScale => volumeScale;

        /// <summary>
        /// 지정된 값으로 Audio Entry를 생성합니다.
        /// 값의 유효성 검사는 Audio Catalog에서 수행합니다.
        /// </summary>
        internal AudioEntry(AudioId id, AudioCategory category, AudioClip clip, float volumeScale = 1f)
        {
            this.id = id;
            this.category = category;
            this.clip = clip;
            this.volumeScale = volumeScale;
        }
    }
}