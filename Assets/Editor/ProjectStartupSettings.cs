// =============================================================================
//  ProjectStartupSettings.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    「プロジェクトを開いたときに何をするか」の設定を保存する。
//    Edit → Project Settings → VantanSchoolFestival2026 の画面で変えられます。
//
//        ・マニュアルを自動で開くか
//        ・シーンが何も開いていないとき、サンプルシーンを開くか
//        ・初めて開いたとき、エディタのレイアウト（ウィンドウの並び）を読み込むか
//
//  【保存場所】
//    ProjectSettings/VantanSchoolFestival2026Settings.asset に保存されます。
//    Git に入れておけば、プロジェクトを受け取った全員が同じ設定になります。
//
//  【ScriptableSingleton とは？】
//    プロジェクトに 1 つだけ存在する設定データを作るための、Unity のしくみです。
//    instance で、どこからでも同じ設定を読めます。
// =============================================================================

using UnityEditor;
using UnityEngine;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>プロジェクトを開いたときの動きの設定。</summary>
    [FilePath("ProjectSettings/VantanSchoolFestival2026Settings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ProjectStartupSettings : ScriptableSingleton<ProjectStartupSettings>
    {
        /// <summary>マニュアルを自動で開くタイミング。</summary>
        public enum ManualOpenMode
        {
            /// <summary>起動するたびに、開いていなければ開く。</summary>
            [InspectorName("起動するたび（開いていなければ）")] EveryStartup,
            /// <summary>このパソコンで初めて開いたときだけ。</summary>
            [InspectorName("初めて開いたときだけ")] FirstTimeOnly,
            /// <summary>自動では開かない。</summary>
            [InspectorName("自動で開かない")] Never,
        }

        [Header("マニュアル・教材")]
        [Tooltip("プロジェクトを開いたときに、マニュアル・教材のウィンドウを自動で開くか。")]
        [SerializeField] private ManualOpenMode _openManual = ManualOpenMode.EveryStartup;

        [Header("サンプルシーン")]
        [Tooltip("プロジェクトを開いたときに、シーンが何も開いていなければ（Untitled なら）サンプルシーンを開くか。")]
        [SerializeField] private bool _openSampleSceneWhenEmpty = true;

        [Tooltip("自動で開くシーン。")]
        [SerializeField] private SceneAsset _sampleScene;

        [Header("エディタのレイアウト")]
        [Tooltip("このパソコンで初めてプロジェクトを開いたときに、下のレイアウト（ウィンドウの並び）を読み込むか。")]
        [SerializeField] private bool _applyLayoutFirstTime = true;

        [Tooltip("読み込むレイアウトのファイル（.wlt）。")]
        [SerializeField] private DefaultAsset _layout;

        // 設定ファイルがまだ無いときに使う、最初の値
        private const string DefaultScenePath = "Assets/Scenes/00_Sample.unity";
        private const string DefaultLayoutPath = "Assets/Settings/vsf-layout.wlt";

        /// <summary>マニュアルを自動で開くタイミング。</summary>
        public ManualOpenMode OpenManual => _openManual;

        /// <summary>シーンが何も開いていないとき、サンプルシーンを開くか。</summary>
        public bool OpenSampleSceneWhenEmpty => _openSampleSceneWhenEmpty;

        /// <summary>サンプルシーンの場所（設定が空なら 00_Sample）。</summary>
        public string SampleScenePath => _sampleScene != null ? AssetDatabase.GetAssetPath(_sampleScene) : DefaultScenePath;

        /// <summary>初めて開いたときにレイアウトを読み込むか。</summary>
        public bool ApplyLayoutFirstTime => _applyLayoutFirstTime;

        /// <summary>レイアウトのファイルの場所（設定が空なら vsf-layout.wlt）。</summary>
        public string LayoutPath => _layout != null ? AssetDatabase.GetAssetPath(_layout) : DefaultLayoutPath;

        /// <summary>
        /// OnEnable … 設定が読み込まれたときに呼ばれる。
        /// シーンやレイアウトが空なら、最初の値を入れておく。
        /// </summary>
        private void OnEnable()
        {
            // ScriptableSingleton は普通は編集できない状態で作られるので、Project Settings の画面で編集できるようにする
            hideFlags &= ~HideFlags.NotEditable;

            if (_sampleScene == null) _sampleScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(DefaultScenePath);
            if (_layout == null) _layout = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultLayoutPath);
        }

        /// <summary>設定をファイルに保存する。</summary>
        public void SaveSettings()
        {
            // true … 人が読める文字（テキスト）の形で保存する（Git で差分が見やすい）
            Save(true);
        }
    }
}
