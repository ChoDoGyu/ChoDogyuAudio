using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioCatalogTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();
        private AudioCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (AudioClip clip in createdClips)
            {
                if (clip != null)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }
            }

            createdClips.Clear();

            if (catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void NewCatalog_IsEmpty()
        {
            Assert.That(catalog.Count, Is.Zero);
            Assert.That(catalog.Entries, Is.Empty);
        }

        [Test]
        public void ReplaceEntries_ValidSource_ReplacesAllEntriesInOrder()
        {
            AudioEntry bgm = CreateEntry("bgm.battle", AudioCategory.Bgm);
            AudioEntry sfx = CreateEntry("sfx.click", AudioCategory.Sfx);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                bgm,
                sfx
            });

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.Entries[0], Is.SameAs(bgm));
            Assert.That(catalog.Entries[1], Is.SameAs(sfx));
        }

        [Test]
        public void ReplaceEntries_EmptySource_ClearsCatalog()
        {
            AudioEntry entry = CreateEntry("bgm.battle", AudioCategory.Bgm);

            Result initialResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(initialResult.IsSuccess, Is.True);
            Assert.That(catalog.Count, Is.EqualTo(1));

            Result result = catalog.ReplaceEntries(Array.Empty<AudioEntry>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(catalog.Count, Is.Zero);
            Assert.That(catalog.Entries, Is.Empty);
        }

        [Test]
        public void ReplaceEntries_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => catalog.ReplaceEntries(null));
        }

        [Test]
        public void ReplaceEntries_NullEntry_ReturnsInvalidCatalog()
        {
            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                null
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_EmptyId_ReturnsInvalidId()
        {
            AudioEntry entry = CreateEntry(string.Empty, AudioCategory.Sfx);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidId));
        }

        [Test]
        public void ReplaceEntries_NullClip_ReturnsInvalidCatalog()
        {
            AudioEntry entry = new AudioEntry(
                new AudioId("sfx.missing"),
                AudioCategory.Sfx,
                null);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_InvalidCategory_ReturnsInvalidCatalog()
        {
            AudioEntry entry = CreateEntry(
                "audio.invalid",
                (AudioCategory)999);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_VolumeBelowZero_ReturnsInvalidCatalog()
        {
            AudioEntry entry = CreateEntry(
                "sfx.invalid",
                AudioCategory.Sfx,
                -0.1f);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_VolumeAboveOne_ReturnsInvalidCatalog()
        {
            AudioEntry entry = CreateEntry(
                "sfx.invalid",
                AudioCategory.Sfx,
                1.1f);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_NaNVolume_ReturnsInvalidCatalog()
        {
            AudioEntry entry = CreateEntry(
                "sfx.invalid",
                AudioCategory.Sfx,
                float.NaN);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_InfiniteVolume_ReturnsInvalidCatalog()
        {
            AudioEntry entry = CreateEntry(
                "sfx.invalid",
                AudioCategory.Sfx,
                float.PositiveInfinity);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void ReplaceEntries_DuplicateId_ReturnsDuplicateId()
        {
            AudioEntry first = CreateEntry("sfx.click", AudioCategory.Sfx);
            AudioEntry second = CreateEntry("sfx.click", AudioCategory.Sfx);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                first,
                second
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.DuplicateId));
        }

        [Test]
        public void ReplaceEntries_InvalidSource_PreservesExistingCatalog()
        {
            AudioEntry original = CreateEntry("bgm.original", AudioCategory.Bgm);

            Result initialResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                original
            });

            Assert.That(initialResult.IsSuccess, Is.True);

            AudioEntry first = CreateEntry("sfx.duplicate", AudioCategory.Sfx);
            AudioEntry second = CreateEntry("sfx.duplicate", AudioCategory.Sfx);

            Result result = catalog.ReplaceEntries(new AudioEntry[]
            {
                first,
                second
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.DuplicateId));

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.Entries[0], Is.SameAs(original));

            Result<AudioEntry> lookupResult = catalog.Get(new AudioId("bgm.original"));

            Assert.That(lookupResult.IsSuccess, Is.True);
            Assert.That(lookupResult.Value, Is.SameAs(original));
        }

        [Test]
        public void ReplaceEntries_CreatesSnapshotIndependentFromSourceChanges()
        {
            AudioEntry original = CreateEntry("bgm.main", AudioCategory.Bgm);

            List<AudioEntry> source = new List<AudioEntry>
            {
                original
            };

            Result result = catalog.ReplaceEntries(source);

            Assert.That(result.IsSuccess, Is.True);

            AudioEntry addedLater = CreateEntry("sfx.click", AudioCategory.Sfx);

            source[0] = addedLater;
            source.Add(original);

            Assert.That(catalog.Count, Is.EqualTo(1));
            Assert.That(catalog.Entries[0], Is.SameAs(original));
        }

        [Test]
        public void ReplaceEntries_AfterEntriesAccess_RebuildsReadOnlyView()
        {
            AudioEntry first = CreateEntry("bgm.first", AudioCategory.Bgm);

            Result firstResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                first
            });

            Assert.That(firstResult.IsSuccess, Is.True);

            IReadOnlyList<AudioEntry> firstView = catalog.Entries;

            AudioEntry second = CreateEntry("bgm.second", AudioCategory.Bgm);

            Result secondResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                second
            });

            Assert.That(secondResult.IsSuccess, Is.True);

            IReadOnlyList<AudioEntry> secondView = catalog.Entries;

            Assert.That(secondView, Is.Not.SameAs(firstView));

            Assert.That(firstView.Count, Is.EqualTo(1));
            Assert.That(firstView[0], Is.SameAs(first));

            Assert.That(secondView.Count, Is.EqualTo(1));
            Assert.That(secondView[0], Is.SameAs(second));
        }

        [Test]
        public void Contains_RegisteredId_ReturnsTrue()
        {
            AudioEntry entry = CreateEntry("bgm.main", AudioCategory.Bgm);

            Result replaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Assert.That(catalog.Contains(new AudioId("bgm.main")), Is.True);
        }

        [Test]
        public void Contains_InvalidOrMissingId_ReturnsFalse()
        {
            AudioEntry entry = CreateEntry("bgm.main", AudioCategory.Bgm);

            Result replaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Assert.That(catalog.Contains(default), Is.False);
            Assert.That(catalog.Contains(new AudioId("bgm.missing")), Is.False);
        }

        [Test]
        public void TryGet_RegisteredId_ReturnsEntry()
        {
            AudioEntry entry = CreateEntry("sfx.click", AudioCategory.Sfx);

            Result replaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            bool result = catalog.TryGet(new AudioId("sfx.click"), out AudioEntry foundEntry);

            Assert.That(result, Is.True);
            Assert.That(foundEntry, Is.SameAs(entry));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            bool result = catalog.TryGet(new AudioId("sfx.missing"), out AudioEntry entry);

            Assert.That(result, Is.False);
            Assert.That(entry, Is.Null);
        }

        [Test]
        public void Get_RegisteredId_ReturnsSuccessWithEntry()
        {
            AudioEntry entry = CreateEntry("bgm.main", AudioCategory.Bgm);

            Result replaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Result<AudioEntry> result = catalog.Get(new AudioId("bgm.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.SameAs(entry));
        }

        [Test]
        public void Get_InvalidId_ReturnsInvalidId()
        {
            Result<AudioEntry> result = catalog.Get(default);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidId));
        }

        [Test]
        public void Get_MissingId_ReturnsNotFound()
        {
            Result<AudioEntry> result = catalog.Get(new AudioId("bgm.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));
        }

        [Test]
        public void ReplaceEntries_AfterLookup_UsesNewLookup()
        {
            AudioEntry first = CreateEntry("bgm.first", AudioCategory.Bgm);

            Result firstReplaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                first
            });

            Assert.That(firstReplaceResult.IsSuccess, Is.True);

            Result<AudioEntry> firstLookupResult = catalog.Get(new AudioId("bgm.first"));

            Assert.That(firstLookupResult.IsSuccess, Is.True);
            Assert.That(firstLookupResult.Value, Is.SameAs(first));

            AudioEntry second = CreateEntry("bgm.second", AudioCategory.Bgm);

            Result secondReplaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                second
            });

            Assert.That(secondReplaceResult.IsSuccess, Is.True);

            Result<AudioEntry> oldLookupResult = catalog.Get(new AudioId("bgm.first"));
            Result<AudioEntry> newLookupResult = catalog.Get(new AudioId("bgm.second"));

            Assert.That(oldLookupResult.IsFailure, Is.True);
            Assert.That(oldLookupResult.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));

            Assert.That(newLookupResult.IsSuccess, Is.True);
            Assert.That(newLookupResult.Value, Is.SameAs(second));
        }

        [Test]
        public void Lookup_IsCaseSensitive()
        {
            AudioEntry entry = CreateEntry("bgm.main", AudioCategory.Bgm);

            Result replaceResult = catalog.ReplaceEntries(new AudioEntry[]
            {
                entry
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Result<AudioEntry> result = catalog.Get(new AudioId("Bgm.Main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));
        }

        private AudioEntry CreateEntry(string id, AudioCategory category, float volumeScale = 1f)
        {
            AudioClip clip = AudioClip.Create(
                id,
                4410,
                1,
                44100,
                false);

            createdClips.Add(clip);

            return new AudioEntry(
                new AudioId(id),
                category,
                clip,
                volumeScale);
        }
    }
}