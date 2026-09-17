using NUnit.Framework;
using UnityEngine;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioEntryTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            AudioClip clip = AudioClip.Create("battle_bgm", 4410, 1, 44100, false);
            AudioId id = new AudioId("battle_bgm");

            AudioEntry entry = new AudioEntry(id, AudioCategory.Bgm, clip, 0.75f);

            Assert.That(entry.Id, Is.EqualTo(id));
            Assert.That(entry.Category, Is.EqualTo(AudioCategory.Bgm));
            Assert.That(entry.Clip, Is.SameAs(clip));
            Assert.That(entry.VolumeScale, Is.EqualTo(0.75f));
        }

        [Test]
        public void Constructor_WithoutVolumeScale_UsesOne()
        {
            AudioClip clip = AudioClip.Create("click", 4410, 1, 44100, false);

            AudioEntry entry = new AudioEntry(new AudioId("click"), AudioCategory.Sfx, clip);

            Assert.That(entry.VolumeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_WithOutOfRangeVolumeScale_PreservesOriginalValue()
        {
            AudioClip clip = AudioClip.Create("click", 4410, 1, 44100, false);

            AudioEntry entry = new AudioEntry(new AudioId("click"), AudioCategory.Sfx, clip, 1.5f);

            Assert.That(entry.VolumeScale, Is.EqualTo(1.5f));
        }

        [Test]
        public void Constructor_WithNullClip_PreservesNull()
        {
            AudioEntry entry = new AudioEntry(new AudioId("missing"), AudioCategory.Sfx, null);

            Assert.That(entry.Clip, Is.Null);
        }
    }
}