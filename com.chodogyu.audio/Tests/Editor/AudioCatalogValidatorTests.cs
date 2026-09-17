using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Editor.Tests
{
    public sealed class AudioCatalogValidatorTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();
        private readonly List<AudioCatalog> createdCatalogs = new List<AudioCatalog>();

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

            foreach (AudioCatalog catalog in createdCatalogs)
            {
                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }
            }

            createdCatalogs.Clear();
        }

        [Test]
        public void Validate_NullCatalog_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => AudioCatalogValidator.Validate(null));
        }

        [Test]
        public void Validate_ValidCatalog_ReturnsNoIssues()
        {
            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, CreateClip("bgm.main")),
                new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack"), 0.5f));

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void Validate_NullEntry_ReturnsInvalidCatalog()
        {
            AudioCatalog catalog = CreateCatalog((AudioEntry)null);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[0].EntryIndex, Is.Zero);
        }

        [Test]
        public void Validate_EmptyId_ReturnsInvalidId()
        {
            AudioCatalog catalog = CreateCatalog(new AudioEntry(default, AudioCategory.Bgm, CreateClip("bgm.main")));

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidId));
            Assert.That(issues[0].EntryIndex, Is.Zero);
        }

        [Test]
        public void Validate_NullClip_ReturnsInvalidCatalog()
        {
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, null));

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[0].EntryIndex, Is.Zero);
        }

        [Test]
        public void Validate_DuplicateId_ReturnsDuplicateIdForSecondEntry()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.shared"), AudioCategory.Bgm, first),
                new AudioEntry(new AudioId("bgm.shared"), AudioCategory.Bgm, second));

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.DuplicateId));
            Assert.That(issues[0].EntryIndex, Is.EqualTo(1));
        }

        [Test]
        public void Validate_InvalidCategory_ReturnsInvalidCatalog()
        {
            AudioEntry entry = new AudioEntry(new AudioId("audio.invalid"), (AudioCategory)999, CreateClip("audio.invalid"));
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[0].EntryIndex, Is.Zero);
        }

        [TestCase(-1f)]
        [TestCase(-0.1f)]
        [TestCase(1.1f)]
        [TestCase(2f)]
        public void Validate_OutOfRangeVolumeScale_ReturnsInvalidCatalog(float volumeScale)
        {
            AudioEntry entry = new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack"), volumeScale);
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[0].EntryIndex, Is.Zero);
        }

        [Test]
        public void Validate_NaNVolumeScale_ReturnsInvalidCatalog()
        {
            AudioEntry entry = new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack"), float.NaN);
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void Validate_PositiveInfinityVolumeScale_ReturnsInvalidCatalog()
        {
            AudioEntry entry = new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack"), float.PositiveInfinity);
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void Validate_NegativeInfinityVolumeScale_ReturnsInvalidCatalog()
        {
            AudioEntry entry = new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack"), float.NegativeInfinity);
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        [Test]
        public void Validate_EntryWithMultipleProblems_ReturnsAllApplicableIssues()
        {
            AudioEntry entry = new AudioEntry(default, (AudioCategory)999, null, 2f);
            AudioCatalog catalog = CreateCatalog(entry);

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(4));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidId));
            Assert.That(issues[1].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[2].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
            Assert.That(issues[3].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));

            foreach (AudioCatalogValidationIssue issue in issues)
            {
                Assert.That(issue.EntryIndex, Is.Zero);
            }
        }

        [Test]
        public void Validate_MultipleEntries_PreservesIssueEntryIndexes()
        {
            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.valid"), AudioCategory.Bgm, CreateClip("bgm.valid")),
                new AudioEntry(default, AudioCategory.Sfx, CreateClip("sfx.invalid-id")),
                new AudioEntry(new AudioId("sfx.no-clip"), AudioCategory.Sfx, null));

            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            Assert.That(issues.Count, Is.EqualTo(2));
            Assert.That(issues[0].EntryIndex, Is.EqualTo(1));
            Assert.That(issues[0].Code, Is.EqualTo(AudioErrorCodes.InvalidId));
            Assert.That(issues[1].EntryIndex, Is.EqualTo(2));
            Assert.That(issues[1].Code, Is.EqualTo(AudioErrorCodes.InvalidCatalog));
        }

        private AudioCatalog CreateCatalog(params AudioEntry[] entries)
        {
            AudioCatalog catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            createdCatalogs.Add(catalog);

            FieldInfo entriesField = typeof(AudioCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(entriesField, Is.Not.Null);

            entriesField.SetValue(catalog, new List<AudioEntry>(entries));

            return catalog;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}