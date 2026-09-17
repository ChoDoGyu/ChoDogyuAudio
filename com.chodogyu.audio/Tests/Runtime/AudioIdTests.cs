using NUnit.Framework;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioIdTests
    {
        [Test]
        public void Default_HasEmptyValue()
        {
            AudioId id = default;

            Assert.That(id.Value, Is.EqualTo(string.Empty));
            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void Constructor_StoresOriginalValue()
        {
            AudioId id = new AudioId("battle_bgm");

            Assert.That(id.Value, Is.EqualTo("battle_bgm"));
            Assert.That(id.IsEmpty, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void IsEmpty_WithEmptyOrWhitespaceValue_ReturnsTrue(string value)
        {
            AudioId id = new AudioId(value);

            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            AudioId first = new AudioId("battle_bgm");
            AudioId second = new AudioId("battle_bgm");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            AudioId first = new AudioId("battle_bgm");
            AudioId second = new AudioId("menu_bgm");

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first == second, Is.False);
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void Equals_WithDifferentCase_ReturnsFalse()
        {
            AudioId first = new AudioId("battle_bgm");
            AudioId second = new AudioId("Battle_Bgm");

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void SameValue_HasSameHashCode()
        {
            AudioId first = new AudioId("battle_bgm");
            AudioId second = new AudioId("battle_bgm");

            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsValue()
        {
            AudioId id = new AudioId("battle_bgm");

            Assert.That(id.ToString(), Is.EqualTo("battle_bgm"));
        }
    }
}