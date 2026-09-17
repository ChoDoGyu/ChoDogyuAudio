using NUnit.Framework;

namespace CDG.Audio.Tests.Runtime
{
    public sealed class AudioVolumeStateTests
    {
        [Test]
        public void NewState_UsesDefaultValues()
        {
            AudioVolumeState state = new AudioVolumeState();

            Assert.That(state.MasterVolume, Is.EqualTo(1f));
            Assert.That(state.BgmVolume, Is.EqualTo(1f));
            Assert.That(state.SfxVolume, Is.EqualTo(1f));

            Assert.That(state.IsMasterMuted, Is.False);
            Assert.That(state.IsBgmMuted, Is.False);
            Assert.That(state.IsSfxMuted, Is.False);

            Assert.That(state.BgmGain, Is.EqualTo(1f));
            Assert.That(state.SfxGain, Is.EqualTo(1f));
        }

        [Test]
        public void SetMasterVolume_StoresValue()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.5f);

            Assert.That(state.MasterVolume, Is.EqualTo(0.5f));
        }

        [Test]
        public void SetBgmVolume_StoresValue()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetBgmVolume(0.4f);

            Assert.That(state.BgmVolume, Is.EqualTo(0.4f));
        }

        [Test]
        public void SetSfxVolume_StoresValue()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetSfxVolume(0.3f);

            Assert.That(state.SfxVolume, Is.EqualTo(0.3f));
        }

        [TestCase(-1f, 0f)]
        [TestCase(-0.1f, 0f)]
        [TestCase(1.1f, 1f)]
        [TestCase(2f, 1f)]
        public void SetMasterVolume_OutOfRange_ClampsValue(float input, float expected)
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(input);

            Assert.That(state.MasterVolume, Is.EqualTo(expected));
        }

        [TestCase(-1f, 0f)]
        [TestCase(-0.1f, 0f)]
        [TestCase(1.1f, 1f)]
        [TestCase(2f, 1f)]
        public void SetBgmVolume_OutOfRange_ClampsValue(float input, float expected)
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetBgmVolume(input);

            Assert.That(state.BgmVolume, Is.EqualTo(expected));
        }

        [TestCase(-1f, 0f)]
        [TestCase(-0.1f, 0f)]
        [TestCase(1.1f, 1f)]
        [TestCase(2f, 1f)]
        public void SetSfxVolume_OutOfRange_ClampsValue(float input, float expected)
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetSfxVolume(input);

            Assert.That(state.SfxVolume, Is.EqualTo(expected));
        }

        [Test]
        public void SetVolumes_NaN_UsesZero()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(float.NaN);
            state.SetBgmVolume(float.NaN);
            state.SetSfxVolume(float.NaN);

            Assert.That(state.MasterVolume, Is.Zero);
            Assert.That(state.BgmVolume, Is.Zero);
            Assert.That(state.SfxVolume, Is.Zero);
        }

        [Test]
        public void SetVolumes_Infinity_ClampsToBounds()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(float.PositiveInfinity);
            state.SetBgmVolume(float.NegativeInfinity);
            state.SetSfxVolume(float.PositiveInfinity);

            Assert.That(state.MasterVolume, Is.EqualTo(1f));
            Assert.That(state.BgmVolume, Is.Zero);
            Assert.That(state.SfxVolume, Is.EqualTo(1f));
        }

        [Test]
        public void BgmGain_MultipliesMasterAndBgmVolume()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.5f);
            state.SetBgmVolume(0.8f);

            Assert.That(state.BgmGain, Is.EqualTo(0.4f).Within(0.0001f));
        }

        [Test]
        public void SfxGain_MultipliesMasterAndSfxVolume()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.5f);
            state.SetSfxVolume(0.6f);

            Assert.That(state.SfxGain, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void MasterMute_SetsBothOutputGainsToZero()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.8f);
            state.SetBgmVolume(0.7f);
            state.SetSfxVolume(0.6f);

            state.SetMasterMuted(true);

            Assert.That(state.BgmGain, Is.Zero);
            Assert.That(state.SfxGain, Is.Zero);
        }

        [Test]
        public void BgmMute_SetsOnlyBgmGainToZero()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetBgmMuted(true);

            Assert.That(state.BgmGain, Is.Zero);
            Assert.That(state.SfxGain, Is.EqualTo(1f));
        }

        [Test]
        public void SfxMute_SetsOnlySfxGainToZero()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetSfxMuted(true);

            Assert.That(state.BgmGain, Is.EqualTo(1f));
            Assert.That(state.SfxGain, Is.Zero);
        }

        [Test]
        public void Mute_DoesNotChangeStoredVolumes()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.8f);
            state.SetBgmVolume(0.7f);
            state.SetSfxVolume(0.6f);

            state.SetMasterMuted(true);
            state.SetBgmMuted(true);
            state.SetSfxMuted(true);

            Assert.That(state.MasterVolume, Is.EqualTo(0.8f));
            Assert.That(state.BgmVolume, Is.EqualTo(0.7f));
            Assert.That(state.SfxVolume, Is.EqualTo(0.6f));
        }

        [Test]
        public void Unmute_RestoresGainFromStoredVolumes()
        {
            AudioVolumeState state = new AudioVolumeState();

            state.SetMasterVolume(0.5f);
            state.SetBgmVolume(0.8f);

            state.SetBgmMuted(true);

            Assert.That(state.BgmGain, Is.Zero);

            state.SetBgmMuted(false);

            Assert.That(state.BgmGain, Is.EqualTo(0.4f).Within(0.0001f));
        }
    }
}