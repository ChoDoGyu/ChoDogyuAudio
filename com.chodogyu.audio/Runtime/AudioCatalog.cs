using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio
{
    /// <summary>
    /// AudioId를 기준으로 BGM과 SFX의 AudioEntry를 조회할 수 있는 Catalog Asset입니다.
    /// 등록 데이터의 유효성을 검증하며 외부에서는 Catalog 내용을 직접 수정할 수 없습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "CDG/Audio/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AudioEntry> entries = new List<AudioEntry>();

        private List<AudioEntry> readOnlyEntriesSource;
        private ReadOnlyCollection<AudioEntry> readOnlyEntries;

        private Dictionary<AudioId, AudioEntry> entriesById;
        private ResultError lookupBuildError;

        /// <summary>
        /// Catalog에 등록된 Audio Entry 수를 반환합니다.
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// Catalog에 등록된 Audio Entry를 직렬화된 순서대로 제공합니다.
        /// 반환된 컬렉션을 통해 항목을 추가, 제거 또는 교체할 수 없습니다.
        /// </summary>
        public IReadOnlyList<AudioEntry> Entries
        {
            get
            {
                if (!ReferenceEquals(readOnlyEntriesSource, entries))
                {
                    readOnlyEntriesSource = entries;
                    readOnlyEntries = entries.AsReadOnly();
                }

                return readOnlyEntries;
            }
        }

        /// <summary>
        /// 지정한 Audio ID가 Catalog에 정상적으로 등록되어 있는지 확인합니다.
        /// ID가 유효하지 않거나 Catalog 구성에 오류가 있으면 false를 반환합니다.
        /// </summary>
        public bool Contains(AudioId id)
        {
            if (id.IsEmpty)
            {
                return false;
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return false;
            }

            return entriesById.ContainsKey(id);
        }

        /// <summary>
        /// 지정한 Audio ID의 Entry를 조회합니다.
        /// ID가 유효하지 않거나 Catalog 구성에 오류가 있거나 항목이 없으면 false를 반환합니다.
        /// </summary>
        public bool TryGet(AudioId id, out AudioEntry entry)
        {
            entry = null;

            if (id.IsEmpty)
            {
                return false;
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return false;
            }

            return entriesById.TryGetValue(id, out entry);
        }

        /// <summary>
        /// 지정한 Audio ID의 Entry를 조회하고 성공 또는 실패 결과를 반환합니다.
        /// 잘못된 ID, Catalog 구성 오류와 미등록 ID를 서로 다른 오류 코드로 구분할 수 있습니다.
        /// </summary>
        public Result<AudioEntry> Get(AudioId id)
        {
            if (id.IsEmpty)
            {
                return Result<AudioEntry>.Failure(new ResultError(
                    AudioErrorCodes.InvalidId,
                    "Audio ID는 비어 있을 수 없습니다."));
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return Result<AudioEntry>.Failure(buildResult.Error);
            }

            if (!entriesById.TryGetValue(id, out AudioEntry entry))
            {
                return Result<AudioEntry>.Failure(new ResultError(
                    AudioErrorCodes.NotFound,
                    $"Catalog에 등록되지 않은 Audio ID입니다: '{id}'"));
            }

            return Result<AudioEntry>.Success(entry);
        }

        /// <summary>
        /// 현재 Catalog의 모든 Entry를 지정한 목록으로 교체합니다.
        /// 새 목록은 독립된 스냅샷으로 생성한 뒤 먼저 검증하며,
        /// 검증에 실패하면 기존 Catalog 데이터는 변경하지 않습니다.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/>가 null인 경우 발생합니다.
        /// </exception>
        internal Result ReplaceEntries(IEnumerable<AudioEntry> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            List<AudioEntry> snapshot = new List<AudioEntry>(source);
            Result<Dictionary<AudioId, AudioEntry>> lookupResult = BuildLookup(snapshot);

            if (lookupResult.IsFailure)
            {
                return Result.Failure(lookupResult.Error);
            }

            entries = snapshot;

            readOnlyEntriesSource = null;
            readOnlyEntries = null;

            entriesById = lookupResult.Value;
            lookupBuildError = null;

            return Result.Success();
        }

        private Result EnsureLookup()
        {
            if (entriesById != null)
            {
                return lookupBuildError == null
                    ? Result.Success()
                    : Result.Failure(lookupBuildError);
            }

            Result<Dictionary<AudioId, AudioEntry>> lookupResult = BuildLookup(entries);

            if (lookupResult.IsFailure)
            {
                entriesById = new Dictionary<AudioId, AudioEntry>();
                lookupBuildError = lookupResult.Error;

                return Result.Failure(lookupBuildError);
            }

            entriesById = lookupResult.Value;
            lookupBuildError = null;

            return Result.Success();
        }

        private Result<Dictionary<AudioId, AudioEntry>> BuildLookup(IReadOnlyList<AudioEntry> source)
        {
            Dictionary<AudioId, AudioEntry> lookup = new Dictionary<AudioId, AudioEntry>();

            for (int i = 0; i < source.Count; i++)
            {
                AudioEntry entry = source[i];

                if (entry == null)
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.InvalidCatalog,
                        $"Catalog의 {i}번 항목에 Audio Entry가 지정되지 않았습니다."));
                }

                if (entry.Id.IsEmpty)
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.InvalidId,
                        $"Catalog의 {i}번 항목에 유효한 Audio ID가 지정되지 않았습니다."));
                }

                if (entry.Clip == null)
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.InvalidCatalog,
                        $"Audio ID '{entry.Id}'에 AudioClip이 지정되지 않았습니다."));
                }

                if (!Enum.IsDefined(typeof(AudioCategory), entry.Category))
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.InvalidCatalog,
                        $"Audio ID '{entry.Id}'에 유효하지 않은 Audio Category가 지정되었습니다."));
                }

                if (float.IsNaN(entry.VolumeScale) || float.IsInfinity(entry.VolumeScale) || entry.VolumeScale < 0f || entry.VolumeScale > 1f)
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.InvalidCatalog,
                        $"Audio ID '{entry.Id}'의 Volume Scale은 0 이상 1 이하의 유한한 값이어야 합니다."));
                }

                if (!lookup.TryAdd(entry.Id, entry))
                {
                    return Result<Dictionary<AudioId, AudioEntry>>.Failure(new ResultError(
                        AudioErrorCodes.DuplicateId,
                        $"Catalog에 중복된 Audio ID가 등록되어 있습니다: '{entry.Id}'"));
                }
            }

            return Result<Dictionary<AudioId, AudioEntry>>.Success(lookup);
        }

        private void InvalidateCaches()
        {
            readOnlyEntriesSource = null;
            readOnlyEntries = null;
            entriesById = null;
            lookupBuildError = null;
        }

        private void OnEnable()
        {
            InvalidateCaches();
        }

        private void OnValidate()
        {
            InvalidateCaches();
        }
    }
}