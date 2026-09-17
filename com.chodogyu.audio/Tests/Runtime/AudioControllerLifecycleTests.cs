using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioControllerLifecycleTests
    {
        private GameObject gameObject;
        private AudioController controller;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("AudioControllerLifecycleTests");
            controller = gameObject.AddComponent<AudioController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [UnityTest]
        public IEnumerator DestroyingController_RemovesOwnedRuntimeSourceRoot()
        {
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);
            Assert.That(gameObject.transform.Find("Audio Runtime Sources"), Is.Not.Null);

            if (Application.isPlaying)
            {
                Object.Destroy(controller);
            }
            else
            {
                Object.DestroyImmediate(controller);
            }

            yield return null;

            Assert.That(gameObject.transform.Find("Audio Runtime Sources"), Is.Null);
        }

        [UnityTest]
        public IEnumerator ReaddingController_AfterRemoval_CreatesOnlyOneRuntimeSourceRoot()
        {
            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);

            if (Application.isPlaying)
            {
                Object.Destroy(controller);
            }
            else
            {
                Object.DestroyImmediate(controller);
            }

            yield return null;

            controller = gameObject.AddComponent<AudioController>();

            Assert.That(controller.ActiveSfxVoiceCount, Is.Zero);

            Transform runtimeSourceRoot = gameObject.transform.Find("Audio Runtime Sources");

            Assert.That(runtimeSourceRoot, Is.Not.Null);
            Assert.That(runtimeSourceRoot.GetComponents<AudioSource>().Length, Is.EqualTo(18));

            int runtimeSourceRootCount = 0;

            foreach (Transform child in gameObject.transform)
            {
                if (child.name == "Audio Runtime Sources")
                {
                    runtimeSourceRootCount++;
                }
            }

            Assert.That(runtimeSourceRootCount, Is.EqualTo(1));
        }
    }
}