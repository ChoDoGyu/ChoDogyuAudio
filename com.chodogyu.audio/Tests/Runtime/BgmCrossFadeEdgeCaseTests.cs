using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class BgmCrossFadeEdgeCaseTests
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
            gameObject = new GameObject("BgmCrossFadeEdgeCaseTests");

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
        public void Play_NewClipDuringCrossFade_UsesLatestRequest()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");
            AudioClip third = CreateClip("bgm.third");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            Result result = player.Play(third, 1f, true, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(third));
            Assert.That(player.IsCrossFading, Is.True);

            Assert.That(sourceA.clip, Is.SameAs(third));
            Assert.That(sourceB.clip, Is.SameAs(second));

            Assert.That(sourceA.volume, Is.Zero);
            Assert.That(sourceB.volume, Is.EqualTo(0.25f).Within(0.0001f));

            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.125f).Within(0.0001f));
        }

        [Test]
        public void Play_NewClipDuringCrossFade_WithoutFade_ReplacesImmediately()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");
            AudioClip third = CreateClip("bgm.third");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            Result result = player.Play(third);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(third));
            Assert.That(player.IsFading, Is.False);
            Assert.That(player.IsCrossFading, Is.False);

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(third));
            Assert.That(sourceB.volume, Is.EqualTo(1f));
        }

        [Test]
        public void Play_OutgoingClipAgainDuringCrossFade_UsesLatestRequest()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(2f);

            Result result = player.Play(first, 1f, true, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(first));
            Assert.That(player.IsCrossFading, Is.True);

            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));

            Assert.That(sourceA.volume, Is.Zero);
            Assert.That(sourceB.volume, Is.EqualTo(0.5f).Within(0.0001f));

            player.Tick(2f);

            Assert.That(player.CurrentClip, Is.SameAs(first));
            Assert.That(player.IsCrossFading, Is.False);
            Assert.That(sourceA.volume, Is.EqualTo(1f));
            Assert.That(sourceB.clip, Is.Null);
        }

        [Test]
        public void Play_SameCurrentClipDuringCrossFade_PreservesTransition()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            float outgoingVolume = sourceA.volume;
            float incomingVolume = sourceB.volume;

            Result result = player.Play(second);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsCrossFading, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(outgoingVolume));
            Assert.That(sourceB.volume, Is.EqualTo(incomingVolume));
        }

        [Test]
        public void Play_NullClipDuringCrossFade_DoesNotInterruptTransition()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            float outgoingVolume = sourceA.volume;
            float incomingVolume = sourceB.volume;

            Result result = player.Play(null);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(AudioErrorCodes.InvalidClip));

            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsCrossFading, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(outgoingVolume));
            Assert.That(sourceB.volume, Is.EqualTo(incomingVolume));
        }

        [Test]
        public void Stop_DuringCrossFade_WithFade_FadesBothSourcesOut()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.25f).Within(0.0001f));

            player.Stop(2f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.375f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.125f).Within(0.0001f));

            player.Tick(1f);

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(player.IsFading, Is.False);
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Null);
        }

        [Test]
        public void Stop_DuringCrossFade_WithoutFade_ClearsBothSourcesImmediately()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            player.Stop();

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(player.IsFading, Is.False);
            Assert.That(player.IsCrossFading, Is.False);
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Null);
        }

        [Test]
        public void Stop_DuringPausedCrossFade_StartsFadeOutAndClearsPause()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            player.Pause();

            Assert.That(player.IsPaused, Is.True);

            player.Stop(2f);

            Assert.That(player.IsPaused, Is.False);
            Assert.That(player.IsFading, Is.True);
            Assert.That(player.IsCrossFading, Is.False);

            player.Tick(2f);

            Assert.That(player.CurrentClip, Is.Null);
            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.Null);
        }

        [Test]
        public void MuteAndUnmute_DuringCrossFade_RestoresBothSourceVolumes()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first, 0.5f);
            player.Play(second, 0.8f, true, 2f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.4f).Within(0.0001f));

            volumeState.SetBgmMuted(true);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.Zero);
            Assert.That(sourceB.volume, Is.Zero);

            volumeState.SetBgmMuted(false);
            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void CrossFade_OvershootTick_CompletesTransition()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 2f);

            player.Tick(10f);

            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsFading, Is.False);
            Assert.That(player.IsCrossFading, Is.False);

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceB.volume, Is.EqualTo(1f));
        }

        [Test]
        public void CrossFade_MultipleSequentialRequests_ReusesTwoSources()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");
            AudioClip third = CreateClip("bgm.third");
            AudioClip fourth = CreateClip("bgm.fourth");

            player.Play(first);

            player.Play(second, 1f, true, 1f);
            player.Tick(1f);

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(second));

            player.Play(third, 1f, true, 1f);
            player.Tick(1f);

            Assert.That(sourceA.clip, Is.SameAs(third));
            Assert.That(sourceB.clip, Is.Null);

            player.Play(fourth, 1f, true, 1f);
            player.Tick(1f);

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(fourth));
            Assert.That(player.CurrentClip, Is.SameAs(fourth));
        }

        [Test]
        public void Play_NewClipDuringFadeIn_StartsCrossFadeFromCurrentGain()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first, 1f, true, 4f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.25f).Within(0.0001f));

            Result result = player.Play(second, 1f, true, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsCrossFading, Is.True);

            Assert.That(sourceA.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.Zero);

            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.125f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}