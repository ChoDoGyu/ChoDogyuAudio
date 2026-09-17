using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioControllerCatalogTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();
        private readonly List<AudioCatalog> createdCatalogs = new List<AudioCatalog>();

        private GameObject gameObject;
        private AudioController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("AudioControllerCatalogTests");
            controller = gameObject.AddComponent<AudioController>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (AudioClip clip in createdClips)
            {
                if (clip != null)
                {
                    Object.DestroyImmediate(clip);
                }
            }

            createdClips.Clear();

            foreach (AudioCatalog catalog in createdCatalogs)
            {
                if (catalog != null)
                {
                    Object.DestroyImmediate(catalog);
                }
            }

            createdCatalogs.Clear();

            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Catalog_DefaultsToNull()
        {
            Assert.That(controller.Catalog, Is.Null);
        }

        [Test]
        public void PlayBgm_ByIdWithoutCatalog_ReturnsCatalogNotAssigned()
        {
            Result result = controller.PlayBgm(new AudioId("bgm.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.CatalogNotAssigned));
            Assert.That(controller.CurrentBgmClip, Is.Null);
        }

        [Test]
        public void PlaySfx_ByIdWithoutCatalog_ReturnsCatalogNotAssigned()
        {
            Result result = controller.PlaySfx(new AudioId("sfx.attack"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.CatalogNotAssigned));
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
        }

        [Test]
        public void PlayBgm_EmptyId_ReturnsInvalidIdBeforeCatalogCheck()
        {
            Result result = controller.PlayBgm(default(AudioId));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidId));
        }

        [Test]
        public void PlaySfx_EmptyId_ReturnsInvalidIdBeforeCatalogCheck()
        {
            Result result = controller.PlaySfx(default(AudioId));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidId));
        }

        [Test]
        public void PlayBgm_ValidId_PlaysCatalogClip()
        {
            AudioClip clip = CreateClip("bgm.main");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, clip));

            controller.SetCatalog(catalog);

            Result result = controller.PlayBgm(new AudioId("bgm.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void PlaySfx_ValidId_PlaysCatalogClip()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, clip));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("sfx.attack"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void PlayBgm_UnknownId_ReturnsNotFound()
        {
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, CreateClip("bgm.main")));

            controller.SetCatalog(catalog);

            Result result = controller.PlayBgm(new AudioId("bgm.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));
        }

        [Test]
        public void PlaySfx_UnknownId_ReturnsNotFound()
        {
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, CreateClip("sfx.attack")));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("sfx.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));
        }

        [Test]
        public void PlayBgm_WithSfxEntry_ReturnsCategoryMismatch()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("shared.id"), AudioCategory.Sfx, clip));

            controller.SetCatalog(catalog);

            Result result = controller.PlayBgm(new AudioId("shared.id"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.CategoryMismatch));
            Assert.That(controller.CurrentBgmClip, Is.Null);
        }

        [Test]
        public void PlaySfx_WithBgmEntry_ReturnsCategoryMismatch()
        {
            AudioClip clip = CreateClip("bgm.main");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("shared.id"), AudioCategory.Bgm, clip));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("shared.id"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.CategoryMismatch));
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
        }

        [Test]
        public void PlayBgm_ById_AppliesEntryVolumeScale()
        {
            AudioClip clip = CreateClip("bgm.main");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, clip, 0.4f));

            controller.SetCatalog(catalog);

            Result result = controller.PlayBgm(new AudioId("bgm.main"));

            Assert.That(result.IsSuccess, Is.True);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[0].volume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void PlaySfx_ById_AppliesEntryAndPlayVolumeScale()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, clip, 0.5f));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("sfx.attack"), 0.4f);

            Assert.That(result.IsSuccess, Is.True);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[2].volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void PlaySfx_ById_NormalizesPlayVolumeBeforeEntryScale()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, clip, 0.5f));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("sfx.attack"), 2f);

            Assert.That(result.IsSuccess, Is.True);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[2].volume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void PlaySfx_ById_NaNPlayVolumeUsesZero()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, clip, 0.5f));

            controller.SetCatalog(catalog);

            Result result = controller.PlaySfx(new AudioId("sfx.attack"), float.NaN);

            Assert.That(result.IsSuccess, Is.True);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[2].volume, Is.Zero);
        }

        [Test]
        public void DirectClipPlayback_DoesNotRequireCatalog()
        {
            AudioClip bgm = CreateClip("bgm.direct");
            AudioClip sfx = CreateClip("sfx.direct");

            Result bgmResult = controller.PlayBgm(bgm);
            Result sfxResult = controller.PlaySfx(sfx);

            Assert.That(bgmResult.IsSuccess, Is.True);
            Assert.That(sfxResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(bgm));
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(1));
        }

        private AudioCatalog CreateCatalog(params AudioEntry[] entries)
        {
            AudioCatalog catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            createdCatalogs.Add(catalog);

            Result replaceResult = catalog.ReplaceEntries(entries);
            Assert.That(replaceResult.IsSuccess, Is.True);

            return catalog;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }

        private AudioSource[] GetRuntimeSources()
        {
            Transform sourceRoot = controller.transform.Find("Audio Runtime Sources");

            Assert.That(sourceRoot, Is.Not.Null);

            return sourceRoot.GetComponents<AudioSource>();
        }
    }
}