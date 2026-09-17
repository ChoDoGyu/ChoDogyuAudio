using System;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// Audio Catalog와 Runtime에서 오디오 항목을 식별하기 위한 고유 ID입니다.
    /// 빈 문자열이나 공백만 포함된 문자열은 유효한 Audio ID로 취급하지 않습니다.
    /// </summary>
    [Serializable]
    public struct AudioId : IEquatable<AudioId>
    {
        [SerializeField]
        private string value;

        /// <summary>
        /// ID의 문자열 값을 반환합니다.
        /// 값이 설정되지 않은 경우 빈 문자열을 반환합니다.
        /// </summary>
        public string Value => value ?? string.Empty;

        /// <summary>
        /// 현재 ID가 비어 있거나 공백만 포함하고 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// 지정된 문자열로 Audio ID를 생성합니다.
        /// 문자열 정규화, 대소문자 변경 또는 공백 제거는 수행하지 않습니다.
        /// </summary>
        public AudioId(string value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public bool Equals(AudioId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AudioId other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(AudioId left, AudioId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioId left, AudioId right)
        {
            return !left.Equals(right);
        }
    }
}