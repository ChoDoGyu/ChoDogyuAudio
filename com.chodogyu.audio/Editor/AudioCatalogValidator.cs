using System;
using System.Collections.Generic;

namespace CDG.Audio.Editor
{
    /// <summary>
    /// Audio Catalog의 직렬화된 Entry 구성을 Editor에서 검사합니다.
    /// Runtime Catalog와 동일한 핵심 유효성 규칙을 사용하지만 모든 문제를 한 번에 수집하여 표시합니다.
    /// </summary>
    internal static class AudioCatalogValidator
    {
        /// <summary>
        /// 지정된 Audio Catalog의 전체 Entry를 검사하고 발견된 문제 목록을 반환합니다.
        /// 문제가 없으면 빈 목록을 반환합니다.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Catalog가 null인 경우 발생합니다.
        /// </exception>
        public static IReadOnlyList<AudioCatalogValidationIssue> Validate(AudioCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            List<AudioCatalogValidationIssue> issues = new List<AudioCatalogValidationIssue>();
            HashSet<AudioId> registeredIds = new HashSet<AudioId>();

            IReadOnlyList<AudioEntry> entries = catalog.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                AudioEntry entry = entries[i];

                if (entry == null)
                {
                    issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.InvalidCatalog, i, $"Entry {i}: Audio Entry가 지정되지 않았습니다."));
                    continue;
                }

                ValidateId(entry, i, registeredIds, issues);
                ValidateClip(entry, i, issues);
                ValidateCategory(entry, i, issues);
                ValidateVolumeScale(entry, i, issues);
            }

            return issues;
        }

        private static void ValidateId(AudioEntry entry, int index, HashSet<AudioId> registeredIds, List<AudioCatalogValidationIssue> issues)
        {
            if (entry.Id.IsEmpty)
            {
                issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.InvalidId, index, $"Entry {index}: Audio ID는 비어 있을 수 없습니다."));
                return;
            }

            if (!registeredIds.Add(entry.Id))
            {
                issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.DuplicateId, index, $"Entry {index}: 중복된 Audio ID가 등록되어 있습니다: '{entry.Id}'"));
            }
        }

        private static void ValidateClip(AudioEntry entry, int index, List<AudioCatalogValidationIssue> issues)
        {
            if (entry.Clip != null)
            {
                return;
            }

            issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.InvalidCatalog, index, $"Entry {index}: Audio ID '{entry.Id}'에 AudioClip이 지정되지 않았습니다."));
        }

        private static void ValidateCategory(AudioEntry entry, int index, List<AudioCatalogValidationIssue> issues)
        {
            if (Enum.IsDefined(typeof(AudioCategory), entry.Category))
            {
                return;
            }

            issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.InvalidCatalog, index, $"Entry {index}: Audio ID '{entry.Id}'에 유효하지 않은 Audio Category가 지정되었습니다."));
        }

        private static void ValidateVolumeScale(AudioEntry entry, int index, List<AudioCatalogValidationIssue> issues)
        {
            if (!float.IsNaN(entry.VolumeScale) && !float.IsInfinity(entry.VolumeScale) && entry.VolumeScale >= 0f && entry.VolumeScale <= 1f)
            {
                return;
            }

            issues.Add(new AudioCatalogValidationIssue(AudioErrorCodes.InvalidCatalog, index, $"Entry {index}: Audio ID '{entry.Id}'의 Volume Scale은 0 이상 1 이하의 유한한 값이어야 합니다."));
        }
    }
}