using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class BgmPlayerTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();

        private GameObject gameObject;
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioVolumeState volumeState;
        private BgmPlayer player;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("BgmPlayerTests");

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();

            volumeState = new AudioVolumeState();
            player = new BgmPlayer(sourceA, sourceB, volumeState);
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
        public void Constructor_ConfiguresSourcesFor2DAudio()
        {
            Assert.That(sourceA.playOnAwake, Is.False);
            Assert.That(sourceB.playOnAwake, Is.False);
            Assert.That(sourceA.spatialBlend, Is.Zero);
            Assert.That(sourceB.spatialBlend, Is.Zero);
        }

        [Test]
        public void Constructor_NullFirstSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BgmPlayer(null, sourceB, volumeState));
        }

        [Test]
        public void Constructor_NullSecondSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BgmPlayer(sourceA, null, volumeState));
        }

        [Test]
        public void Constructor_NullVolumeState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BgmPlayer(sourceA, sourceB, null));
        }

        [Test]
        public void Constructor_SameSource_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new BgmPlayer(sourceA, sourceA, volumeState));
        }

        [Test]
        public void Play_NullClip_ReturnsInvalidClip()
        {
            Result result = player.Play(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));
            Assert.That(player.CurrentClip, Is.Null);
        }

        [Test]
        public void Play_ValidClip_AssignsClipAndLoop()
        {
            AudioClip clip = CreateClip("bgm.main");

            Result result = player.Play(clip, 1f, true);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(sourceA.clip, Is.SameAs(clip));
            Assert.That(sourceA.loop, Is.True);
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Play_WithLoopDisabled_DisablesLoop()
        {
            AudioClip clip = CreateClip("bgm.once");

            Result result = player.Play(clip, 1f, false);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.loop, Is.False);
        }

        [Test]
        public void Play_AppliesBgmGainAndVolumeScale()
        {
            AudioClip clip = CreateClip("bgm.volume");

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetBgmVolume(0.8f);

            Result result = player.Play(clip, 0.5f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [TestCase(-1f, 0f)]
        [TestCase(2f, 1f)]
        public void Play_OutOfRangeVolumeScale_ClampsValue(float input, float expected)
        {
            AudioClip clip = CreateClip("bgm.volume");

            Result result = player.Play(clip, input);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(expected));
        }

        [Test]
        public void Play_NaNVolumeScale_UsesZero()
        {
            AudioClip clip = CreateClip("bgm.volume");

            Result result = player.Play(clip, float.NaN);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.volume, Is.Zero);
        }

        [Test]
        public void Play_NewClip_ReplacesCurrentClip()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            Result firstResult = player.Play(first);
            Result secondResult = player.Play(second);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(sourceA.clip, Is.SameAs(second));
        }

        [Test]
        public void Play_SameClip_ReturnsSuccessWithoutChangingSettings()
        {
            AudioClip clip = CreateClip("bgm.main");

            Result firstResult = player.Play(clip, 0.5f, true);
            Result secondResult = player.Play(clip, 1f, false);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(sourceA.volume, Is.EqualTo(0.5f));
            Assert.That(sourceA.loop, Is.True);
        }

        [Test]
        public void Pause_WithCurrentClip_SetsPausedState()
        {
            AudioClip clip = CreateClip("bgm.main");

            Result result = player.Play(clip);

            Assert.That(result.IsSuccess, Is.True);

            player.Pause();

            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(player.IsPaused, Is.True);
        }

        [Test]
        public void Pause_WithoutCurrentClip_DoesNothing()
        {
            player.Pause();

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Pause_WhenAlreadyPaused_RemainsPaused()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);
            player.Pause();
            player.Pause();

            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(player.IsPaused, Is.True);
        }

        [Test]
        public void Play_SameClipWhilePaused_DoesNotResume()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);
            player.Pause();

            Result result = player.Play(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(player.IsPaused, Is.True);
        }

        [Test]
        public void Resume_WhenPaused_ClearsPausedState()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);
            player.Pause();

            Assert.That(player.IsPaused, Is.True);

            player.Resume();

            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Resume_WithoutCurrentClip_DoesNothing()
        {
            player.Resume();

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Resume_WhenNotPaused_DoesNothing()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);
            player.Resume();

            Assert.That(player.CurrentClip, Is.SameAs(clip));
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void RefreshVolume_AppliesLatestVolumeState()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip, 0.5f);

            Assert.That(sourceA.volume, Is.EqualTo(0.5f));

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetBgmVolume(0.8f);

            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void RefreshVolume_WithBgmMute_AppliesZero()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);

            volumeState.SetBgmMuted(true);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.Zero);
        }

        [Test]
        public void RefreshVolume_AfterUnmute_RestoresGain()
        {
            AudioClip clip = CreateClip("bgm.main");

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetBgmVolume(0.8f);

            player.Play(clip);

            volumeState.SetBgmMuted(true);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.Zero);

            volumeState.SetBgmMuted(false);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void RefreshVolume_WithoutCurrentClip_DoesNothing()
        {
            Assert.DoesNotThrow(() => player.RefreshVolume());
            Assert.That(player.CurrentClip, Is.Null);
        }

        [Test]
        public void Stop_ClearsCurrentPlaybackState()
        {
            AudioClip clip = CreateClip("bgm.main");

            Result result = player.Play(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(clip));

            player.Stop();

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Null);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Stop_WhilePaused_ClearsPausedState()
        {
            AudioClip clip = CreateClip("bgm.main");

            player.Play(clip);
            player.Pause();

            Assert.That(player.IsPaused, Is.True);

            player.Stop();

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(player.IsPaused, Is.False);
        }

        [Test]
        public void Play_NewClipWhilePaused_ReplacesAndClearsPausedState()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Pause();

            Result result = player.Play(second);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsPaused, Is.False);
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);

            return clip;
        }
    }
}