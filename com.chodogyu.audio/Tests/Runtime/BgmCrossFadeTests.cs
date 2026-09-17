using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class BgmCrossFadeTests
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
            gameObject = new GameObject("BgmCrossFadeTests");

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
        public void Play_DifferentClipWithFade_StartsCrossFade()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            Result result = player.Play(second, 1f, true, 2f);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(player.CurrentClip, Is.SameAs(second));
            Assert.That(player.IsFading, Is.True);
            Assert.That(player.IsCrossFading, Is.True);

            Assert.That(sourceA.clip, Is.SameAs(first));
            Assert.That(sourceB.clip, Is.SameAs(second));

            Assert.That(sourceA.volume, Is.EqualTo(1f));
            Assert.That(sourceB.volume, Is.Zero);
        }

        [Test]
        public void CrossFade_Halfway_BlendsBothSourcesEqually()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 2f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(player.IsCrossFading, Is.True);
        }

        [Test]
        public void CrossFade_CompletesAndClearsOutgoingSource()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 2f);
            player.Tick(2f);

            Assert.That(player.IsFading, Is.False);
            Assert.That(player.IsCrossFading, Is.False);
            Assert.That(player.CurrentClip, Is.SameAs(second));

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceB.volume, Is.EqualTo(1f));
        }

        [Test]
        public void CrossFade_UsesIndependentVolumeScales()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first, 0.5f);
            player.Play(second, 0.8f, true, 2f);
            player.Tick(1f);

            Assert.That(sourceA.volume, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void RefreshVolume_DuringCrossFade_AppliesToBothSources()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 2f);
            player.Tick(1f);

            volumeState.SetMasterVolume(0.5f);
            volumeState.SetBgmVolume(0.8f);

            player.RefreshVolume();

            Assert.That(sourceA.volume, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void Pause_DuringCrossFade_PreservesTransitionProgress()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);
            player.Tick(1f);

            float outgoingVolume = sourceA.volume;
            float incomingVolume = sourceB.volume;

            player.Pause();
            player.Tick(2f);

            Assert.That(player.IsPaused, Is.True);
            Assert.That(player.IsCrossFading, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(outgoingVolume));
            Assert.That(sourceB.volume, Is.EqualTo(incomingVolume));
        }

        [Test]
        public void Resume_AfterPausedCrossFade_ContinuesTransition()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");

            player.Play(first);
            player.Play(second, 1f, true, 4f);

            player.Tick(1f);
            player.Pause();
            player.Tick(2f);
            player.Resume();
            player.Tick(1f);

            Assert.That(player.IsPaused, Is.False);
            Assert.That(player.IsCrossFading, Is.True);
            Assert.That(sourceA.volume, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(sourceB.volume, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CrossFade_ReusesSourcesInOppositeDirection()
        {
            AudioClip first = CreateClip("bgm.first");
            AudioClip second = CreateClip("bgm.second");
            AudioClip third = CreateClip("bgm.third");

            player.Play(first);

            player.Play(second, 1f, true, 1f);
            player.Tick(1f);

            Assert.That(sourceA.clip, Is.Null);
            Assert.That(sourceB.clip, Is.SameAs(second));

            player.Play(third, 1f, true, 1f);

            Assert.That(player.CurrentClip, Is.SameAs(third));
            Assert.That(sourceA.clip, Is.SameAs(third));
            Assert.That(sourceB.clip, Is.SameAs(second));
            Assert.That(sourceA.volume, Is.Zero);
            Assert.That(sourceB.volume, Is.EqualTo(1f));
        }

        private AudioClip CreateClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 4410, 1, 44100, false);
            createdClips.Add(clip);
            return clip;
        }
    }
}