using CDG.Core.Results;
using UnityEngine;

namespace CDG.Audio.Samples.BasicUsage
{
    /// <summary>
    /// ChoDogyu Audio Framework의 기본 Runtime API를 직접 실행해 볼 수 있는 Sample입니다.
    /// Direct AudioClip 재생과 AudioId 기반 재생, BGM 전환, SFX, Volume 및 Mute 제어를 확인할 수 있습니다.
    /// </summary>
    [RequireComponent(typeof(AudioController))]
    public sealed class AudioBasicUsageSample : MonoBehaviour
    {
        [Header("Direct AudioClip Sample")]

        [SerializeField]
        private AudioClip directBgmA;

        [SerializeField]
        private AudioClip directBgmB;

        [SerializeField]
        private AudioClip directSfx;

        [Header("Audio Catalog Sample")]

        [SerializeField]
        private AudioId catalogBgmAId = new AudioId("bgm.sample.a");

        [SerializeField]
        private AudioId catalogBgmBId = new AudioId("bgm.sample.b");

        [SerializeField]
        private AudioId catalogSfxId = new AudioId("sfx.sample");

        [Header("Transition")]

        [SerializeField]
        [Min(0f)]
        private float crossFadeDuration = 1f;

        private AudioController audioController;

        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;

        private void Awake()
        {
            audioController = GetComponent<AudioController>();

            masterVolume = audioController.MasterVolume;
            bgmVolume = audioController.BgmVolume;
            sfxVolume = audioController.SfxVolume;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 380f, 680f), GUI.skin.box);

            GUILayout.Label("ChoDogyu Audio Framework - Basic Usage");
            GUILayout.Space(10f);

            DrawDirectClipSection();
            GUILayout.Space(10f);

            DrawCatalogSection();
            GUILayout.Space(10f);

            DrawBgmControlSection();
            GUILayout.Space(10f);

            DrawVolumeSection();
            GUILayout.Space(10f);

            DrawMuteSection();
            GUILayout.Space(10f);

            DrawRuntimeState();

            GUILayout.EndArea();
        }

        private void DrawDirectClipSection()
        {
            GUILayout.Label("Direct AudioClip");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("BGM A"))
            {
                PlayDirectBgm(directBgmA);
            }

            if (GUILayout.Button("BGM B"))
            {
                PlayDirectBgm(directBgmB);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Play SFX"))
            {
                PlayDirectSfx();
            }
        }

        private void DrawCatalogSection()
        {
            GUILayout.Label("Audio Catalog / AudioId");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Catalog BGM A"))
            {
                PlayCatalogBgm(catalogBgmAId);
            }

            if (GUILayout.Button("Catalog BGM B"))
            {
                PlayCatalogBgm(catalogBgmBId);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Catalog SFX"))
            {
                PlayCatalogSfx();
            }
        }

        private void DrawBgmControlSection()
        {
            GUILayout.Label("BGM Control");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Pause"))
            {
                audioController.PauseBgm();
            }

            if (GUILayout.Button("Resume"))
            {
                audioController.ResumeBgm();
            }

            if (GUILayout.Button("Fade Stop"))
            {
                audioController.StopBgm(crossFadeDuration);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Stop All"))
            {
                audioController.StopAll();
            }
        }

        private void DrawVolumeSection()
        {
            GUILayout.Label($"Master Volume: {masterVolume:0.00}");
            float newMasterVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);

            if (!Mathf.Approximately(masterVolume, newMasterVolume))
            {
                masterVolume = newMasterVolume;
                audioController.SetMasterVolume(masterVolume);
            }

            GUILayout.Label($"BGM Volume: {bgmVolume:0.00}");
            float newBgmVolume = GUILayout.HorizontalSlider(bgmVolume, 0f, 1f);

            if (!Mathf.Approximately(bgmVolume, newBgmVolume))
            {
                bgmVolume = newBgmVolume;
                audioController.SetBgmVolume(bgmVolume);
            }

            GUILayout.Label($"SFX Volume: {sfxVolume:0.00}");
            float newSfxVolume = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f);

            if (!Mathf.Approximately(sfxVolume, newSfxVolume))
            {
                sfxVolume = newSfxVolume;
                audioController.SetSfxVolume(sfxVolume);
            }
        }

        private void DrawMuteSection()
        {
            GUILayout.Label("Mute");

            bool masterMuted = GUILayout.Toggle(audioController.IsMasterMuted, "Master Mute");

            if (masterMuted != audioController.IsMasterMuted)
            {
                audioController.SetMasterMuted(masterMuted);
            }

            bool bgmMuted = GUILayout.Toggle(audioController.IsBgmMuted, "BGM Mute");

            if (bgmMuted != audioController.IsBgmMuted)
            {
                audioController.SetBgmMuted(bgmMuted);
            }

            bool sfxMuted = GUILayout.Toggle(audioController.IsSfxMuted, "SFX Mute");

            if (sfxMuted != audioController.IsSfxMuted)
            {
                audioController.SetSfxMuted(sfxMuted);
            }
        }

        private void DrawRuntimeState()
        {
            GUILayout.Label("Runtime State");

            string bgmName = audioController.CurrentBgmClip != null
                ? audioController.CurrentBgmClip.name
                : "None";

            GUILayout.Label($"Current BGM: {bgmName}");
            GUILayout.Label($"BGM Playing: {audioController.IsBgmPlaying}");
            GUILayout.Label($"BGM Paused: {audioController.IsBgmPaused}");
            GUILayout.Label($"BGM Fading: {audioController.IsBgmFading}");
            GUILayout.Label($"Active SFX Voices: {audioController.ActiveSfxVoiceCount} / {audioController.SfxVoiceCount}");
        }

        private void PlayDirectBgm(AudioClip clip)
        {
            Result result = audioController.PlayBgm(clip, crossFadeDuration);
            LogResult("Direct BGM 재생", result);
        }

        private void PlayDirectSfx()
        {
            Result result = audioController.PlaySfx(directSfx);
            LogResult("Direct SFX 재생", result);
        }

        private void PlayCatalogBgm(AudioId id)
        {
            Result result = audioController.PlayBgm(id, crossFadeDuration);
            LogResult($"Catalog BGM '{id}' 재생", result);
        }

        private void PlayCatalogSfx()
        {
            Result result = audioController.PlaySfx(catalogSfxId);
            LogResult($"Catalog SFX '{catalogSfxId}' 재생", result);
        }

        private static void LogResult(string operation, Result result)
        {
            if (result.IsSuccess)
            {
                Debug.Log($"{operation} 성공");
                return;
            }

            Debug.LogError($"{operation} 실패 - {result.Error.Code}: {result.Error.Message}");
        }
    }
}