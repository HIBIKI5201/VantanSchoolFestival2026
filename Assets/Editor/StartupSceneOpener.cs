// =============================================================================
//  StartupSceneOpener.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    プロジェクトを開いたときに、シーンが何も開いていなければ 00_Sample を開く。
//
//  【なぜ必要？】
//    Unity は「前回開いていたシーン」を覚えていて、次に起動したときにそれを開きます。
//    でも、GitHub から受け取ったばかり（clone したて）のプロジェクトには、その記録がありません。
//    そのままだと、何も入っていない「Untitled」のシーンが開いてしまい、初めての人は戸惑います。
//    そこで、起動したときに何も開いていなければ、完成版の 00_Sample を開くようにしています。
//
//    ・前回開いていたシーンの記録があるときは、そちらを優先します（勝手に切り替えません）。
//    ・メニュー「VantanSchoolFestival2026 → サンプルシーンを開く」でも、いつでも開けます。
// =============================================================================

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>起動時に、シーンが何も開いていなければ 00_Sample を開く。</summary>
    // InitializeOnLoad … エディタの起動時（とスクリプトの再読み込み時）に、static コンストラクタを呼ぶ
    [InitializeOnLoad]
    public static class StartupSceneOpener
    {
        /// <summary>最初に開くシーン。</summary>
        private const string SampleScenePath = "Assets/Scenes/00_Sample.unity";

        // 「このエディタの起動で、もう確認したか」を覚えておくキー
        // SessionState … エディタを閉じるまで覚えている保存場所（スクリプトを読み込み直しても消えない）
        private const string CheckedKey = "VantanSchoolFestival2026.StartupSceneOpener.Checked";

        /// <summary>static コンストラクタ … このクラスが初めて使われるときに 1 回だけ呼ばれる。</summary>
        static StartupSceneOpener()
        {
            // スクリプトを読み込み直すたびではなく、エディタを起動したときの 1 回だけ確認する
            if (SessionState.GetBool(CheckedKey, false)) return;
            SessionState.SetBool(CheckedKey, true);

            // delayCall … エディタの準備（前回のシーンを開くなど）が終わってから実行する
            EditorApplication.delayCall += OpenSampleIfNothingIsOpen;
        }

        /// <summary>何も入っていない「Untitled」のシーンだけが開いていたら、00_Sample を開く。</summary>
        private static void OpenSampleIfNothingIsOpen()
        {
            // コマンドラインで動かしているときや、再生しようとしているときは何もしない
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 保存されたシーンが開いている（path がある）、または何か編集中なら、そのままにする
            Scene active = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path) || active.isDirty || SceneManager.sceneCount > 1) return;

            OpenSampleScene();
        }

        /// <summary>メニューから 00_Sample を開く。</summary>
        [MenuItem("VantanSchoolFestival2026/サンプルシーンを開く (00_Sample)", priority = 20)]
        public static void OpenSampleFromMenu()
        {
            // 編集中のシーンに保存していない変更があれば、保存するか聞く（キャンセルされたら開かない）
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            OpenSampleScene();
        }

        private static void OpenSampleScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) == null)
            {
                Debug.LogWarning("[StartupSceneOpener] " + SampleScenePath + " が見つかりません。");
                return;
            }
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }
    }
}
