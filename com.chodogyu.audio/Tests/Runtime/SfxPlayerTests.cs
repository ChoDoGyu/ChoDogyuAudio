using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class SfxPlayerTests
    {
        private readonly List<AudioClip> createdClips = new List<AudioClip>();

        private GameObject gameObject;
        private AudioVolumeState volumeState;
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource sourceC;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("SfxPlayerTests");
            volumeState = new AudioVolumeState();

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();
            sourceC = gameObject.AddComponent<AudioSource>();
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
        public void Constructor_CreatesVoiceForEachSource()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            Assert.That(player.VoiceCount, Is.EqualTo(3));
            Assert.That(player.ActiveVoiceCount, Is.Zero);
            Assert.That(player.OverflowPolicy, Is.EqualTo(SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_NullSources_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SfxPlayer(null, volumeState, SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_EmptySources_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SfxPlayer(Array.Empty<AudioSource>(), volumeState, SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_NullSourceElement_ThrowsArgumentException()
        {
            AudioSource[] sources = { sourceA, null };

            Assert.Throws<ArgumentException>(() => new SfxPlayer(sources, volumeState, SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_DuplicateSource_ThrowsArgumentException()
        {
            AudioSource[] sources = { sourceA, sourceA };

            Assert.Throws<ArgumentException>(() => new SfxPlayer(sources, volumeState, SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_NullVolumeState_ThrowsArgumentNullException()
        {
            AudioSource[] sources = { sourceA };

            Assert.Throws<ArgumentNullException>(() => new SfxPlayer(sources, null, SfxOverflowPolicy.RejectNew));
        }

        [Test]
        public void Constructor_InvalidOverflowPolicy_ThrowsArgumentOutOfRangeException()
        {
            AudioSource[] sources = { sourceA };

            Assert.Throws<ArgumentOutOfRangeException>(() => new SfxPlayer(sources, volumeState, (SfxOverflowPolicy)999));
        }

        [Test]
        public void Play_FirstClip_UsesFirstAvailableVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);
            AudioClip clip = CreateClip("sfx.first");

            Result result = player.Play(clip);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(sourceA.clip, Is.SameAs(clip));
            Assert.That(sourceB.clip, Is.Null);
            Assert.That(sourceC.clip, Is.Null);
        }

        [Test]
        public void Play_SameClipTwice_UsesDifferentVoices()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);
            AudioClip clip = CreateClip("sfx.attack");

            Result firstResult = player.Play(clip);
            Result secondResult = player.Play(clip);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(2));
            Assert.That(sourceA.clip, Is.SameAs(clip));
            Assert.That(sourceB.clip, Is.SameAs(clip));
        }

        [Test]
        public void Play_MultipleClips_UsesAllAvailableVoices()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");

            Assert.That(player.Play(first).IsSuccess, Is.True);
            Assert.That(player.Play(second).IsSuccess, Is.True);
            Assert.That(player.Play(third).IsSuccess, Is.True);

            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));
            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void Play_WhenFullWithRejectNew_ReturnsNoAvailableVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip fourth = CreateClip("sfx.fourth");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            Result result = player.Play(fourth);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NoAvailableSfxVoice));
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));

            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void Play_WhenFullWithStopOldest_ReplacesOldestVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip fourth = CreateClip("sfx.fourth");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            Result result = player.Play(fourth);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));

            Assert.That(sourceA.clip, Is.SameAs(fourth));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void Play_NullClip_ReturnsInvalidClipBeforeOverflowHandling()
        {
            AudioSource[] sources = { sourceA };
            SfxPlayer player = new SfxPlayer(sources, volumeState, SfxOverflowPolicy.RejectNew);
            AudioClip clip = CreateClip("sfx.active");

            player.Play(clip);

            Result result = player.Play(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));
            Assert.That(sourceA.clip, Is.SameAs(clip));
        }

        [Test]
        public void StopAll_ClearsEveryVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"));
            player.Play(CreateClip("sfx.second"));
            player.Play(CreateClip("sfx.third"));

            player.StopAll();

            Assert.That(player.ActiveVoiceCount, Is.Zero);
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Null);
            Assert.That(sourceC.clip, Is.Null);
        }

        [Test]
        public void PauseAll_KeepsPausedVoicesActiveDuringTick()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"));
            player.Play(CreateClip("sfx.second"));

            player.PauseAll();
            player.Tick();

            Assert.That(player.ActiveVoiceCount, Is.EqualTo(2));
            Assert.That(sourceA.clip, Is.Not.Null);
            Assert.That(sourceB.clip, Is.Not.Null);
        }

        [Test]
        public void RefreshVolume_AppliesLatestStateToAllVoices()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"), 0.5f);
            player.Play(CreateClip("sfx.second"), 0.8f);

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetSfxVolume(0.5f);

            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.125f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void Tick_StoppedSource_ReleasesVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"));
            player.Play(CreateClip("sfx.second"));

            sourceA.Stop();
            player.Tick();

            Assert.That(player.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Not.Null);
        }

        [Test]
        public void Play_AfterReleasedVoice_ReusesAvailableSource()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip replacement = CreateClip("sfx.replacement");

            player.Play(first);
            player.Play(second);

            sourceA.Stop();
            player.Tick();

            Result result = player.Play(replacement);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.clip, Is.SameAs(replacement));
            Assert.That(sourceB.clip, Is.SameAs(second));
        }

        [Test]
        public void Tick_WithoutActiveVoices_DoesNothing()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            Assert.DoesNotThrow(() => player.Tick());
            Assert.That(player.ActiveVoiceCount, Is.Zero);
        }

        private SfxPlayer CreatePlayer(SfxOverflowPolicy overflowPolicy)
        {
            AudioSource[] sources = { sourceA, sourceB, sourceC };
            return new SfxPlayer(sources, volumeState, overflowPolicy);
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}