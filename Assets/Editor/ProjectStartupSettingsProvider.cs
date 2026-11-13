// =============================================================================
//  ProjectStartupSettingsProvider.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    Edit → Project Settings に「VantanSchoolFestival2026」のページを追加して、
//    ProjectStartupSettings（プロジェクトを開いたときの動き）を変えられるようにする。
//
//  【SettingsProvider とは？】
//    Project Settings や Preferences の画面に、自分のページを足すための Unity のしくみです。
//    [SettingsProvider] を付けた関数が返したページが、画面の左の一覧に表示されます。
// =============================================================================

using UnityEditor;
using UnityEngine;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>Project Settings に「VantanSchoolFestival2026」のページを追加する。</summary>
    public static class ProjectStartupSettingsProvider
    {
        /// <summary>Project Settings の画面を開くときに、Unity から呼ばれる。</summary>
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/VantanSchoolFestival2026", SettingsScope.Project)
            {
                label = "VantanSchoolFestival2026",
                guiHandler = DrawGUI,
                // 検索欄に入れたときに、このページが見つかるようにする言葉
                keywords = new[] { "マニュアル", "教材", "サンプル", "レイアウト", "起動" },
            };
        }

        /// <summary>ページの中身を描く。</summary>
        private static void DrawGUI(string searchContext)
        {
            ProjectStartupSettings settings = ProjectStartupSettings.instance;

            // SerializedObject … 設定の値を Inspector と同じ見た目で編集するための入れ物
            var serialized = new SerializedObject(settings);

            EditorGUILayout.LabelField("プロジェクトを開いたときの動き", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "ここで変えた設定は ProjectSettings/VantanSchoolFestival2026Settings.asset に保存されます。" +
                "Git に入れると、プロジェクトを受け取った全員に同じ設定が使われます。", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serialized.FindProperty("_openManual"), new GUIContent("マニュアルを開く"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.FindProperty("_openSampleSceneWhenEmpty"), new GUIContent("空のときサンプルシーンを開く"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_sampleScene"), new GUIContent("サンプルシーン"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serialized.FindProperty("_applyLayoutFirstTime"), new GUIContent("初回にレイアウトを読み込む"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_layout"), new GUIContent("レイアウト（.wlt）"));
            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                settings.SaveSettings();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("今すぐ試す", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("マニュアルを開く")) DocsWindow.OpenIndex();
                if (GUILayout.Button("サンプルシーンを開く")) ProjectStartup.OpenSampleFromMenu();
                if (GUILayout.Button("レイアウトを読み込む")) ProjectStartup.ApplyLayout();
            }
        }
    }
}
