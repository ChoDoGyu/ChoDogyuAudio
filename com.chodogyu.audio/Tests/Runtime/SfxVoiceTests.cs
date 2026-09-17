using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class SfxVoiceTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();

        private GameObject gameObject;
        private AudioSource source;
        private AudioVolumeState volumeState;
        private SfxVoice voice;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("SfxVoiceTests");
            source = gameObject.AddComponent<AudioSource>();
            volumeState = new AudioVolumeState();
            voice = new SfxVoice(source, volumeState);
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

            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Constructor_ConfiguresSourceFor2DSfx()
        {
            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.loop, Is.False);
            Assert.That(source.spatialBlend, Is.Zero);
        }

        [Test]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SfxVoice(null, volumeState));
        }

        [Test]
        public void Constructor_NullVolumeState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SfxVoice(source, null));
        }

        [Test]
        public void NewVoice_IsInactive()
        {
            Assert.That(voice.CurrentClip, Is.Null);
            Assert.That(voice.IsActive, Is.False);
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void Play_NullClip_ReturnsInvalidClip()
        {
            Result result = voice.Play(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));
            Assert.That(voice.CurrentClip, Is.Null);
        }

        [Test]
        public void Play_ValidClip_AssignsClip()
        {
            AudioClip clip = CreateClip("sfx.attack");

            Result result = voice.Play(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(voice.CurrentClip, Is.SameAs(clip));
            Assert.That(voice.IsActive, Is.True);
            Assert.That(source.clip, Is.SameAs(clip));
            Assert.That(source.loop, Is.False);
        }

        [Test]
        public void Play_AppliesSfxGainAndVolumeScale()
        {
            AudioClip clip = CreateClip("sfx.attack");

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetSfxVolume(0.8f);

            Result result = voice.Play(clip, 0.5f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(source.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [TestCase(-1f, 0f)]
        [TestCase(-0.1f, 0f)]
        [TestCase(1.1f, 1f)]
        [TestCase(2f, 1f)]
        public void Play_OutOfRangeVolumeScale_ClampsValue(float input, float expected)
        {
            AudioClip clip = CreateClip("sfx.attack");

            Result result = voice.Play(clip, input);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(source.volume, Is.EqualTo(expected));
        }

        [Test]
        public void Play_NaNVolumeScale_UsesZero()
        {
            AudioClip clip = CreateClip("sfx.attack");

            Result result = voice.Play(clip, float.NaN);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(source.volume, Is.Zero);
        }

        [Test]
        public void Play_NewClip_ReplacesCurrentClip()
        {
            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");

            Result firstResult = voice.Play(first);
            Result secondResult = voice.Play(second);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(voice.CurrentClip, Is.SameAs(second));
            Assert.That(source.clip, Is.SameAs(second));
        }

        [Test]
        public void Pause_WithCurrentClip_SetsPausedState()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Pause();

            Assert.That(voice.CurrentClip, Is.SameAs(clip));
            Assert.That(voice.IsPaused, Is.True);
            Assert.That(voice.IsActive, Is.True);
        }

        [Test]
        public void Pause_WithoutCurrentClip_DoesNothing()
        {
            voice.Pause();

            Assert.That(voice.CurrentClip, Is.Null);
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void Resume_WhenPaused_ClearsPausedState()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Pause();
            voice.Resume();

            Assert.That(voice.CurrentClip, Is.SameAs(clip));
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void Resume_WhenNotPaused_DoesNothing()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Resume();

            Assert.That(voice.CurrentClip, Is.SameAs(clip));
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void Play_WhilePaused_ReplacesClipAndClearsPausedState()
        {
            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");

            voice.Play(first);
            voice.Pause();

            Result result = voice.Play(second);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(voice.CurrentClip, Is.SameAs(second));
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void Stop_ClearsPlaybackState()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Stop();

            Assert.That(voice.CurrentClip, Is.Null);
            Assert.That(voice.IsActive, Is.False);
            Assert.That(voice.IsPaused, Is.False);
            Assert.That(source.clip, Is.Null);
        }

        [Test]
        public void Stop_WhilePaused_ClearsPlaybackState()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Pause();
            voice.Stop();

            Assert.That(voice.CurrentClip, Is.Null);
            Assert.That(voice.IsActive, Is.False);
            Assert.That(voice.IsPaused, Is.False);
        }

        [Test]
        public void RefreshVolume_AppliesLatestVolumeState()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip, 0.5f);

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetSfxVolume(0.8f);

            voice.RefreshVolume();

            Assert.That(source.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void RefreshVolume_WithSfxMute_AppliesZero()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);

            volumeState.SetSfxMuted(true);
            voice.RefreshVolume();

            Assert.That(source.volume, Is.Zero);
        }

        [Test]
        public void RefreshVolume_AfterUnmute_RestoresVolume()
        {
            AudioClip clip = CreateClip("sfx.attack");

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetSfxVolume(0.8f);

            voice.Play(clip);

            volumeState.SetSfxMuted(true);
            voice.RefreshVolume();

            Assert.That(source.volume, Is.Zero);

            volumeState.SetSfxMuted(false);
            voice.RefreshVolume();

            Assert.That(source.volume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void RefreshVolume_WithoutCurrentClip_DoesNothing()
        {
            Assert.DoesNotThrow(() => voice.RefreshVolume());
            Assert.That(voice.CurrentClip, Is.Null);
        }

        [Test]
        public void Tick_AfterPlaybackStops_ClearsVoice()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            source.Stop();

            voice.Tick();

            Assert.That(voice.CurrentClip, Is.Null);
            Assert.That(voice.IsActive, Is.False);
        }

        [Test]
        public void Tick_WhilePaused_DoesNotClearVoice()
        {
            AudioClip clip = CreateClip("sfx.attack");

            voice.Play(clip);
            voice.Pause();

            voice.Tick();

            Assert.That(voice.CurrentClip, Is.SameAs(clip));
            Assert.That(voice.IsActive, Is.True);
            Assert.That(voice.IsPaused, Is.True);
        }

        [Test]
        public void Tick_WithoutCurrentClip_DoesNothing()
        {
            Assert.DoesNotThrow(() => voice.Tick());
            Assert.That(voice.IsActive, Is.False);
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}