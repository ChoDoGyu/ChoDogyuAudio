using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class SfxPlayerEdgeCaseTests
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
            gameObject = new GameObject("SfxPlayerEdgeCaseTests");
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
        public void StopOldest_RepeatedOverflow_ReplacesVoicesInSequenceOrder()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip fourth = CreateClip("sfx.fourth");
            AudioClip fifth = CreateClip("sfx.fifth");
            AudioClip sixth = CreateClip("sfx.sixth");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            player.Play(fourth);

            Assert.That(sourceA.clip, Is.SameAs(fourth));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));

            player.Play(fifth);

            Assert.That(sourceA.clip, Is.SameAs(fourth));
            Assert.That(sourceB.clip, Is.SameAs(fifth));
            Assert.That(sourceC.clip, Is.SameAs(third));

            player.Play(sixth);

            Assert.That(sourceA.clip, Is.SameAs(fourth));
            Assert.That(sourceB.clip, Is.SameAs(fifth));
            Assert.That(sourceC.clip, Is.SameAs(sixth));
        }

        [Test]
        public void StopOldest_ReleasedAndReusedVoice_BecomesNewest()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip fourth = CreateClip("sfx.fourth");
            AudioClip fifth = CreateClip("sfx.fifth");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            sourceA.Stop();
            player.Tick();

            player.Play(fourth);

            Assert.That(sourceA.clip, Is.SameAs(fourth));

            player.Play(fifth);

            Assert.That(sourceA.clip, Is.SameAs(fourth));
            Assert.That(sourceB.clip, Is.SameAs(fifth));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void StopOldest_AllVoicesPaused_ReplacesOldestPausedVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip replacement = CreateClip("sfx.replacement");

            player.Play(first);
            player.Play(second);
            player.Play(third);
            player.PauseAll();

            Result result = player.Play(replacement);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));
            Assert.That(sourceA.clip, Is.SameAs(replacement));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void RejectNew_AllVoicesPaused_ReturnsNoAvailableVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            player.Play(CreateClip("sfx.first"));
            player.Play(CreateClip("sfx.second"));
            player.Play(CreateClip("sfx.third"));
            player.PauseAll();

            Result result = player.Play(CreateClip("sfx.rejected"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.NoAvailableSfxVoice));
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));
        }

        [Test]
        public void RejectNew_RejectedRequest_DoesNotAffectLaterVoiceSelection()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip rejected = CreateClip("sfx.rejected");
            AudioClip replacement = CreateClip("sfx.replacement");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            Result rejectedResult = player.Play(rejected);

            Assert.That(rejectedResult.IsFailure, Is.True);

            sourceB.Stop();
            player.Tick();

            Result replacementResult = player.Play(replacement);

            Assert.That(replacementResult.IsSuccess, Is.True);
            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(replacement));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void StopAll_AfterOverflow_ClearsAndReusesFirstVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"));
            player.Play(CreateClip("sfx.second"));
            player.Play(CreateClip("sfx.third"));
            player.Play(CreateClip("sfx.fourth"));

            player.StopAll();

            Assert.That(player.ActiveVoiceCount, Is.Zero);

            AudioClip next = CreateClip("sfx.next");
            Result result = player.Play(next);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.clip, Is.SameAs(next));
            Assert.That(sourceB.clip, Is.Null);
            Assert.That(sourceC.clip, Is.Null);
        }

        [Test]
        public void PauseAll_WithFreeVoice_AllowsNewPlaybackOnFreeVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.RejectNew);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");

            player.Play(first);
            player.Play(second);
            player.PauseAll();

            Result result = player.Play(third);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));
            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void ResumeAll_AfterPauseAll_PreservesAssignedClips()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");

            player.Play(first);
            player.Play(second);

            player.PauseAll();
            player.ResumeAll();

            Assert.That(player.ActiveVoiceCount, Is.EqualTo(2));
            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));
        }

        [Test]
        public void RefreshVolume_MuteAndUnmute_RestoresIndependentVoiceVolumes()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            player.Play(CreateClip("sfx.first"), 0.5f);
            player.Play(CreateClip("sfx.second"), 0.8f);

            volumeState.SetSfxMuted(true);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.Zero);
            Assert.That(sourceB.volume, Is.Zero);

            volumeState.SetSfxMuted(false);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void Tick_MultipleStoppedSources_ReleasesAllCompletedVoices()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            sourceA.Stop();
            sourceC.Stop();

            player.Tick();

            Assert.That(player.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceC.clip, Is.Null);
        }

        [Test]
        public void StopOldest_AfterMultipleReleasedVoices_UsesFirstAvailableVoice()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            AudioClip first = CreateClip("sfx.first");
            AudioClip second = CreateClip("sfx.second");
            AudioClip third = CreateClip("sfx.third");
            AudioClip replacement = CreateClip("sfx.replacement");

            player.Play(first);
            player.Play(second);
            player.Play(third);

            sourceA.Stop();
            sourceB.Stop();
            player.Tick();

            Result result = player.Play(replacement);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sourceA.clip, Is.SameAs(replacement));
            Assert.That(sourceB.clip, Is.Null);
            Assert.That(sourceC.clip, Is.SameAs(third));
        }

        [Test]
        public void StopOldest_ManySequentialRequests_MaintainsFixedVoiceCount()
        {
            SfxPlayer player = CreatePlayer(SfxOverflowPolicy.StopOldest);

            for (int i = 0; i < 20; i++)
            {
                Result result = player.Play(CreateClip($"sfx.{i}"));
                Assert.That(result.IsSuccess, Is.True);
            }

            Assert.That(player.VoiceCount, Is.EqualTo(3));
            Assert.That(player.ActiveVoiceCount, Is.EqualTo(3));
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