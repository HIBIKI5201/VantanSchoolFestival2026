// =============================================================================
//  ProjectStartup.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    プロジェクトを開いたときに、次の 3 つを順番に行う。
//    （どれも Edit → Project Settings → VantanSchoolFestival2026 で ON / OFF できます）
//
//      1. このパソコンで初めて開いたときは、エディタのレイアウト（ウィンドウの並び）を読み込む
//      2. シーンが何も開いていなければ（Untitled なら）、サンプルシーン（00_Sample）を開く
//      3. マニュアル・教材のウィンドウが開いていなければ、開く
//
//  【なぜ必要？】
//    GitHub から受け取ったばかりのプロジェクトは、何も入っていないシーンと
//    いつものウィンドウの並びで開きます。初めての人が迷わないように、
//    授業で使う画面をそろえて、マニュアルを最初に見せるようにしています。
// =============================================================================

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>プロジェクトを開いたときに、レイアウト・サンプルシーン・マニュアルを準備する。</summary>
    // InitializeOnLoad … エディタの起動時（とスクリプトの再読み込み時）に、static コンストラクタを呼ぶ
    [InitializeOnLoad]
    public static class ProjectStartup
    {
        // 「このエディタの起動で、もう準備したか」を覚えておくキー
        // SessionState … エディタを閉じるまで覚えている保存場所（スクリプトを読み込み直しても消えない）
        private const string StartedKey = "VantanSchoolFestival2026.ProjectStartup.Started";

        // 「このパソコンで、もうやったか」を覚えておくキーの先頭
        // EditorPrefs はパソコン全体で共有されるので、プロジェクトの場所をくっつけて使う
        private const string LayoutAppliedKeyPrefix = "VantanSchoolFestival2026.LayoutApplied.";
        private const string ManualShownKeyPrefix = "VantanSchoolFestival2026.DocsWindow.Opened.";

        /// <summary>static コンストラクタ … このクラスが初めて使われるときに 1 回だけ呼ばれる。</summary>
        static ProjectStartup()
        {
            // スクリプトを読み込み直すたびではなく、エディタを起動したときの 1 回だけ行う
            if (SessionState.GetBool(StartedKey, false)) return;

            // コマンドラインで動かしているときは何もしない
            if (Application.isBatchMode) return;

            // delayCall … エディタの準備（前回のシーンを開くなど）が終わってから実行する
            // ※ 起動の直後はスクリプトの読み込み直しが起きることがあり、そのときは予約が消えてしまう。
            //    そのため「もう準備した」という印は、ここではなく Run の中で付ける。
            EditorApplication.delayCall += Run;
        }

        /// <summary>起動したときの準備をする。</summary>
        private static void Run()
        {
            if (SessionState.GetBool(StartedKey, false)) return;

            // スクリプトのコンパイル中や再生中は、終わってからもう一度ためす
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            SessionState.SetBool(StartedKey, true);

            ProjectStartupSettings settings = ProjectStartupSettings.instance;

            // 1. レイアウト（初めてのときだけ）
            string layoutKey = LayoutAppliedKeyPrefix + Application.dataPath;
            if (settings.ApplyLayoutFirstTime && !EditorPrefs.GetBool(layoutKey, false))
            {
                EditorPrefs.SetBool(layoutKey, true);
                ApplyLayout();
            }

            // レイアウトを読み込むとウィンドウが作り直されるので、そのあとで続きを行う
            EditorApplication.delayCall += () =>
            {
                // 2. サンプルシーン
                if (settings.OpenSampleSceneWhenEmpty) OpenSampleIfNothingIsOpen();

                // 3. マニュアル
                OpenManualIfNeeded(settings.OpenManual);
            };
        }

        // ---------------------------------------------------------------------
        // レイアウト
        // ---------------------------------------------------------------------

        /// <summary>設定されたレイアウト（.wlt）を読み込む。</summary>
        public static void ApplyLayout()
        {
            string path = ProjectStartupSettings.instance.LayoutPath;
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[ProjectStartup] レイアウトのファイルが見つかりません：" + path);
                return;
            }
            // LoadWindowLayout … 保存しておいたウィンドウの並びを読み込む
            EditorUtility.LoadWindowLayout(fullPath);
        }

        // ---------------------------------------------------------------------
        // サンプルシーン
        // ---------------------------------------------------------------------

        /// <summary>何も入っていない「Untitled」のシーンだけが開いていたら、サンプルシーンを開く。</summary>
        private static void OpenSampleIfNothingIsOpen()
        {
            // 保存されたシーンが開いている（path がある）、または何か編集中なら、そのままにする
            Scene active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path) || active.isDirty || SceneManager.sceneCount > 1) return;

            OpenSampleScene();
        }

        /// <summary>メニューからサンプルシーンを開く。</summary>
        [MenuItem("VantanSchoolFestival2026/サンプルシーンを開く (00_Sample)", priority = 20)]
        public static void OpenSampleFromMenu()
        {
            // 編集中のシーンに保存していない変更があれば、保存するか聞く（キャンセルされたら開かない）
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            OpenSampleScene();
        }

        private static void OpenSampleScene()
        {
            string path = ProjectStartupSettings.instance.SampleScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning("[ProjectStartup] サンプルシーンが見つかりません：" + path);
                return;
            }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        // ---------------------------------------------------------------------
        // マニュアル
        // ---------------------------------------------------------------------

        /// <summary>設定に合わせて、マニュアルが開いていなければ開く。</summary>
        private static void OpenManualIfNeeded(ProjectStartupSettings.ManualOpenMode mode)
        {
            if (mode == ProjectStartupSettings.ManualOpenMode.Never) return;

            string shownKey = ManualShownKeyPrefix + Application.dataPath;
            if (mode == ProjectStartupSettings.ManualOpenMode.FirstTimeOnly && EditorPrefs.GetBool(shownKey, false)) return;
            EditorPrefs.SetBool(shownKey, true);

            // HasOpenInstances … そのウィンドウがもう開いているか（レイアウトで開いていることもある）
            if (EditorWindow.HasOpenInstances<DocsWindow>()) return;
            DocsWindow.OpenIndex();
        }
    }
}
