using System.Collections.Generic;
using UnityEditor;

namespace CDG.Audio.Editor
{
    /// <summary>
    /// Audio Catalog의 기본 Inspector와 유효성 검사 결과를 함께 표시합니다.
    /// 잘못된 Entry가 있으면 해당 문제와 오류 코드를 Inspector 하단에서 즉시 확인할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(AudioCatalog))]
    internal sealed class AudioCatalogInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            AudioCatalog catalog = (AudioCatalog)target;
            IReadOnlyList<AudioCatalogValidationIssue> issues = AudioCatalogValidator.Validate(catalog);

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Audio Catalog 유효성 검사 통과.", MessageType.Info);
                return;
            }

            foreach (AudioCatalogValidationIssue issue in issues)
            {
                EditorGUILayout.HelpBox($"[{issue.Code}] {issue.Message}", MessageType.Error);
            }
        }
    }
}