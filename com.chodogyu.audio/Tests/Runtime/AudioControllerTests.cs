using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioControllerTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();

        private GameObject gameObject;
        private AudioController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("AudioControllerTests");
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

            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NewController_UsesDefaultAudioState()
        {
            Assert.That(controller.MasterVolume, Is.EqualTo(1f));
            Assert.That(controller.BgmVolume, Is.EqualTo(1f));
            Assert.That(controller.SfxVolume, Is.EqualTo(1f));

            Assert.That(controller.IsMasterMuted, Is.False);
            Assert.That(controller.IsBgmMuted, Is.False);
            Assert.That(controller.IsSfxMuted, Is.False);

            Assert.That(controller.CurrentBgmClip, Is.Null);
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
            Assert.That(controller.SfxVoiceCount, Is.EqualTo(16));
        }

        [Test]
        public void PlayBgm_ValidClip_SetsCurrentBgm()
        {
            AudioClip clip = CreateClip("bgm.main");

            Result result = controller.PlayBgm(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void PlayBgm_NullClip_ReturnsInvalidClip()
        {
            Result result = controller.PlayBgm(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));
            Assert.That(controller.CurrentBgmClip, Is.Null);
        }

        [Test]
        public void PlayBgm_WithFade_UsesControllerTick()
        {
            AudioClip clip = CreateClip("bgm.fade");

            Result result = controller.PlayBgm(clip, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.IsBgmFading, Is.True);

            controller.Tick(2f);

            Assert.That(controller.IsBgmFading, Is.False);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void PlayBgm_NewClipWithFade_UsesCrossFade()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            controller.PlayBgm(first);
            Result result = controller.PlayBgm(second, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(second));
            Assert.That(controller.IsBgmFading, Is.True);

            controller.Tick(2f);

            Assert.That(controller.IsBgmFading, Is.False);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(second));
        }

        [Test]
        public void PauseAndResumeBgm_PreservesCurrentClip()
        {
            AudioClip clip = CreateClip("bgm.main");

            controller.PlayBgm(clip);
            controller.PauseBgm();

            Assert.That(controller.IsBgmPaused, Is.True);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));

            controller.ResumeBgm();

            Assert.That(controller.IsBgmPaused, Is.False);
            Assert.That(controller.CurrentBgmClip, Is.SameAs(clip));
        }

        [Test]
        public void StopBgm_ClearsCurrentBgm()
        {
            AudioClip clip = CreateClip("bgm.main");

            controller.PlayBgm(clip);
            controller.StopBgm();

            Assert.That(controller.CurrentBgmClip, Is.Null);
            Assert.That(controller.IsBgmPaused, Is.False);
            Assert.That(controller.IsBgmFading, Is.False);
        }

        [Test]
        public void PlaySfx_ValidClip_IncreasesActiveVoiceCount()
        {
            AudioClip clip = CreateClip("sfx.attack");

            Result result = controller.PlaySfx(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(1));
        }

        [Test]
        public void PlaySfx_SameClipTwice_UsesMultipleVoices()
        {
            AudioClip clip = CreateClip("sfx.attack");

            Result firstResult = controller.PlaySfx(clip);
            Result secondResult = controller.PlaySfx(clip);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(2));
        }

        [Test]
        public void PlaySfx_NullClip_ReturnsInvalidClip()
        {
            Result result = controller.PlaySfx(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
        }

        [Test]
        public void PauseAndResumeAllSfx_PreservesActiveVoices()
        {
            controller.PlaySfx(CreateClip("sfx.first"));
            controller.PlaySfx(CreateClip("sfx.second"));

            controller.PauseAllSfx();
            controller.Tick(1f);

            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(2));

            controller.ResumeAllSfx();

            Assert.That(controller.ActiveSfxVoiceCount, Is.EqualTo(2));
        }

        [Test]
        public void StopAllSfx_ClearsAllActiveVoices()
        {
            controller.PlaySfx(CreateClip("sfx.first"));
            controller.PlaySfx(CreateClip("sfx.second"));

            controller.StopAllSfx();

            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
        }

        [Test]
        public void SetVolumes_UpdatesStoredValues()
        {
            controller.SetMasterVolume(0.5f);
            controller.SetBgmVolume(0.6f);
            controller.SetSfxVolume(0.7f);

            Assert.That(controller.MasterVolume, Is.EqualTo(0.5f));
            Assert.That(controller.BgmVolume, Is.EqualTo(0.6f));
            Assert.That(controller.SfxVolume, Is.EqualTo(0.7f));
        }

        [Test]
        public void SetVolumes_OutOfRange_UsesNormalizedValues()
        {
            controller.SetMasterVolume(-1f);
            controller.SetBgmVolume(2f);
            controller.SetSfxVolume(float.NaN);

            Assert.That(controller.MasterVolume, Is.Zero);
            Assert.That(controller.BgmVolume, Is.EqualTo(1f));
            Assert.That(controller.SfxVolume, Is.Zero);
        }

        [Test]
        public void SetMuteStates_UpdatesStoredValues()
        {
            controller.SetMasterMuted(true);
            controller.SetBgmMuted(true);
            controller.SetSfxMuted(true);

            Assert.That(controller.IsMasterMuted, Is.True);
            Assert.That(controller.IsBgmMuted, Is.True);
            Assert.That(controller.IsSfxMuted, Is.True);
        }

        [Test]
        public void StopAll_ClearsBgmAndSfx()
        {
            controller.PlayBgm(CreateClip("bgm.main"));
            controller.PlaySfx(CreateClip("sfx.first"));
            controller.PlaySfx(CreateClip("sfx.second"));

            controller.StopAll();

            Assert.That(controller.CurrentBgmClip, Is.Null);
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}