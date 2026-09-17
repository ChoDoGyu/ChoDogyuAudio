using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioRuntimeIntegrationTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();
        private readonly List<AudioCatalog> createdCatalogs = new List<AudioCatalog>();

        private GameObject gameObject;
        private AudioController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("AudioRuntimeIntegrationTests");
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
        public void Initialization_CreatesFixedSourcePoolOnlyOnce()
        {
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);

            Transform firstRoot = GetRuntimeSourceRoot();
            AudioSource[] firstSources = firstRoot.GetComponents<AudioSource>();

            Assert.That(firstSources.Length, Is.EqualTo(18));

            controller.PlayBgm(CreateClip("bgm.main"));
            controller.PlaySfx(CreateClip("sfx.first"));
            controller.PlaySfx(CreateClip("sfx.second"));

            Transform secondRoot = GetRuntimeSourceRoot();
            AudioSource[] secondSources = secondRoot.GetComponents<AudioSource>();

            Assert.That(secondRoot, Is.SameAs(firstRoot));
            Assert.That(secondSources.Length, Is.EqualTo(18));
        }

        [Test]
        public void DefaultStopOldest_WhenMoreThanSixteenSfxRequested_MaintainsFixedCapacity()
        {
            List<AudioClip> clips = new List<AudioClip>();

            for (int i = 0; i < 17; i++)
            {
                AudioClip clip = CreateClip($"sfx.{i}");
                clips.Add(clip);

                Result result = controller.PlaySfx(clip);

                Assert.That(result.IsSuccess, Is.True);
            }

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(controller.SfxVoiceCount, Is.EqualTo(16));
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(16));
            Assert.That(sources.Length, Is.EqualTo(18));
            Assert.That(sources[2].clip, Is.SameAs(clips[16]));
        }

        [Test]
        public void StopAll_DoesNotRecreateRuntimeSourcePool()
        {
            controller.PlayBgm(CreateClip("bgm.first"));
            controller.PlaySfx(CreateClip("sfx.first"));

            Transform sourceRoot = GetRuntimeSourceRoot();
            int sourceRootId = sourceRoot.GetInstanceID();

            controller.StopAll();

            controller.PlayBgm(CreateClip("bgm.second"));
            controller.PlaySfx(CreateClip("sfx.second"));

            Transform reusedRoot = GetRuntimeSourceRoot();

            Assert.That(reusedRoot.GetInstanceID(), Is.EqualTo(sourceRootId));
            Assert.That(reusedRoot.GetComponents<AudioSource>().Length, Is.EqualTo(18));
        }

        [Test]
        public void MasterMute_WithActiveBgmAndSfx_MutesAndRestoresBoth()
        {
            controller.PlayBgm(CreateClip("bgm.main"));
            controller.PlaySfx(CreateClip("sfx.attack"), 0.5f);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[0].volume, Is.EqualTo(1f));
            Assert.That(sources[2].volume, Is.EqualTo(0.5f));

            controller.SetMasterMuted(true);

            Assert.That(sources[0].volume, Is.Zero);
            Assert.That(sources[2].volume, Is.Zero);

            controller.SetMasterMuted(false);

            Assert.That(sources[0].volume, Is.EqualTo(1f));
            Assert.That(sources[2].volume, Is.EqualTo(0.5f));
        }

        [Test]
        public void VolumeChanges_DuringCrossFadeAndSfxPlayback_ApplyImmediately()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            controller.PlayBgm(first);
            controller.PlayBgm(second, 2f);
            controller.Tick(1f);

            controller.PlaySfx(CreateClip("sfx.attack"), 0.8f);

            controller.SetMasterVolume(0.5f);
            controller.SetBgmVolume(0.6f);
            controller.SetSfxVolume(0.4f);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[0].volume, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(sources[1].volume, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(sources[2].volume, Is.EqualTo(0.16f).Within(0.0001f));
        }

        [Test]
        public void CatalogBgmCrossFade_UsesEachEntryVolumeScale()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.first"), AudioCategory.Bgm, first, 0.4f),
                new AudioEntry(new AudioId("bgm.second"), AudioCategory.Bgm, second, 0.8f));

            controller.SetCatalog(catalog);

            controller.PlayBgm(new AudioId("bgm.first"));
            controller.PlayBgm(new AudioId("bgm.second"), 2f);
            controller.Tick(1f);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[0].volume, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(sources[1].volume, Is.EqualTo(0.4f).Within(0.0001f));

            controller.Tick(1f);

            Assert.That(sources[0].clip, Is.Null);
            Assert.That(sources[1].clip, Is.SameAs(second));
            Assert.That(sources[1].volume, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void CatalogSfx_AppliesGlobalEntryAndRequestVolumeScales()
        {
            AudioClip clip = CreateClip("sfx.attack");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, clip, 0.5f));

            controller.SetCatalog(catalog);
            controller.SetMasterVolume(0.5f);
            controller.SetSfxVolume(0.8f);

            Result result = controller.PlaySfx(new AudioId("sfx.attack"), 0.25f);

            Assert.That(result.IsSuccess, Is.True);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[2].volume, Is.EqualTo(0.05f).Within(0.0001f));
        }

        [Test]
        public void UnknownId_DoesNotInterruptCurrentBgm()
        {
            AudioClip clip = CreateClip("bgm.main");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, clip));

            controller.SetCatalog(catalog);
            controller.PlayBgm(new AudioId("bgm.main"));

            Result result = controller.PlayBgm(new AudioId("bgm.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NotFound));
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void CategoryMismatch_DoesNotInterruptCurrentBgm()
        {
            AudioClip bgm = CreateClip("bgm.main");
            AudioClip sfx = CreateClip("sfx.attack");

            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, bgm),
                new AudioEntry(new AudioId("sfx.attack"), AudioCategory.Sfx, sfx));

            controller.SetCatalog(catalog);
            controller.PlayBgm(new AudioId("bgm.main"));

            Result result = controller.PlayBgm(new AudioId("sfx.attack"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.CategoryMismatch));
            Assert.That(controller.CurrentBgmClip, Is.SameAs(bgm));
        }

        [Test]
        public void FadeOutThenSameIdPlay_CancelsPendingStop()
        {
            AudioClip clip = CreateClip("bgm.main");
            AudioCatalog catalog = CreateCatalog(new AudioEntry(new AudioId("bgm.main"), AudioCategory.Bgm, clip));

            controller.SetCatalog(catalog);

            controller.PlayBgm(new AudioId("bgm.main"));
            controller.StopBgm(4f);
            controller.Tick(2f);

            AudioSource[] sources = GetRuntimeSources();

            Assert.That(sources[0].volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(controller.IsBgmFading, Is.True);

            Result result = controller.PlayBgm(new AudioId("bgm.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.IsBgmFading, Is.False);
            Assert.That(sources[0].volume, Is.EqualTo(1f));

            controller.Tick(10f);

            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void ControllerTick_ReleasesStoppedSfxVoice()
        {
            controller.PlaySfx(CreateClip("sfx.first"));
            controller.PlaySfx(CreateClip("sfx.second"));

            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(2));

            AudioSource[] sources = GetRuntimeSources();
            sources[2].Stop();

            controller.Tick(0f);

            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(1));
            Assert.That(sources[2].clip, Is.Null);
            Assert.That(sources[3].clip, Is.Not.Null);
        }

        [Test]
        public void DirectAndCatalogPlayback_CanBeUsedTogether()
        {
            AudioClip directBgm = CreateClip("bgm.direct");
            AudioClip catalogBgm = CreateClip("bgm.catalog");
            AudioClip directSfx = CreateClip("sfx.direct");
            AudioClip catalogSfx = CreateClip("sfx.catalog");

            AudioCatalog catalog = CreateCatalog(
                new AudioEntry(new AudioId("bgm.catalog"), AudioCategory.Bgm, catalogBgm),
                new AudioEntry(new AudioId("sfx.catalog"), AudioCategory.Sfx, catalogSfx));

            controller.SetCatalog(catalog);

            Assert.That(controller.PlayBgm(directBgm).IsSuccess, Is.True);
            Assert.That(controller.PlaySfx(directSfx).IsSuccess, Is.True);
            Assert.That(controller.PlayBgm(new AudioId("bgm.catalog"), 1f).IsSuccess, Is.True);
            Assert.That(controller.PlaySfx(new AudioId("sfx.catalog")).IsSuccess, Is.True);

            Assert.That(controller.CurrentBgmClip, Is.SameAs(catalogBgm));
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(2));

            controller.Tick(1f);

            Assert.That(controller.IsBgmFading, Is.False);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(catalogBgm));
        }

        private AudioCatalog CreateCatalog(params AudioEntry[] entries)
        {
            AudioCatalog catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            createdCatalogs.Add(catalog);

            Result result = catalog.ReplaceEntries(entries);

            Assert.That(result.IsSuccess, Is.True);

            return catalog;
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }

        private Transform GetRuntimeSourceRoot()
        {
            Transform sourceRoot = controller.transform.Find("Audio Runtime Sources");

            Assert.That(sourceRoot, Is.Not.Null);

            return sourceRoot;
        }

        private AudioSource[] GetRuntimeSources()
        {
            return GetRuntimeSourceRoot().GetComponents<AudioSource>();
        }
    }
}