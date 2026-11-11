// =============================================================================
//  StageCheckerWindow.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    今開いているシーンを調べて、「ゲームとして遊べる状態か」をチェックする専用ウィンドウ。
//    メニューの「VantanSchoolFestival2026 > ステージチェッカー」で開きます。
//
//  【チェックする内容】
//    ✖ エラー … このままだと遊べない（プレイヤーやゴールが無い など）
//    ⚠ 警告   … 遊べるけど困ることがある（落下ゾーンが無い など）
//    ✔ OK     … 問題なし
//
//    各行の「選択」ボタンで問題のオブジェクトを Hierarchy で選択、
//    「修正」ボタンで自動的に直すことができます（Ctrl+Z で元に戻せます）。
//
//  【エディタ拡張のしくみ】
//    EditorWindow を継承したクラスを作り、OnGUI の中で
//    GUILayout / EditorGUILayout を使ってボタンや文字を並べていきます。
//    （この書き方を IMGUI と呼びます）
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>シーンにプレイヤーやゴールがあるかなどを調べるエディタウィンドウ。</summary>
    public class StageCheckerWindow : EditorWindow
    {
        // ---------------------------------------------------------------------
        // 定数：「修正」ボタンで生成するプレハブの場所
        // ---------------------------------------------------------------------
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/RCCar.prefab";
        private const string GoalPrefabPath = "Assets/Prefabs/Stage/Goal.prefab";
        private const string StartPointPrefabPath = "Assets/Prefabs/Stage/StartPoint.prefab";
        private const string KillZonePrefabPath = "Assets/Prefabs/Stage/KillZone.prefab";
        private const string GameManagerPrefabPath = "Assets/Prefabs/System/GameManager.prefab";
        private const string GameUIPrefabPath = "Assets/Prefabs/System/GameUI.prefab";
        private const string PlayerControllerPrefabPath = "Assets/Prefabs/System/PlayerController.prefab";
        private const string FollowCameraPrefabPath = "Assets/Prefabs/System/FollowCamera.prefab";
        private const string GlobalVolumePrefabPath = "Assets/Prefabs/System/GlobalVolume.prefab";
        private const string InputActionsPath = "Assets/Settings/RCInputActions.inputactions";

        // ---------------------------------------------------------------------
        // チェック結果 1 件分のデータ
        // ---------------------------------------------------------------------

        /// <summary>結果の重さ。</summary>
        private enum Severity { Ok, Info, Warning, Error }

        /// <summary>チェック結果 1 行分。</summary>
        private class CheckResult
        {
            public Severity Severity;
            public string Title;      // 何をチェックしたか
            public string Message;    // 結果の説明
            public Object Target;     // 「選択」ボタンで選ぶオブジェクト（無ければ null）
            public string FixLabel;   // 「修正」ボタンの文字
            public Action Fix;        // 「修正」ボタンを押したときの処理（無ければ null）
        }

        // 結果の一覧
        private readonly List<CheckResult> _results = new List<CheckResult>();

        // スクロール位置
        private Vector2 _scroll;

        // シーンが変わったら自動で再チェックするか
        private bool _autoRefresh = true;

        // ---------------------------------------------------------------------
        // ウィンドウを開く
        // ---------------------------------------------------------------------

        /// <summary>
        /// MenuItem … Unity 上部のメニューに項目を追加する。
        /// "VantanSchoolFestival2026/ステージチェッカー" と書くと「VantanSchoolFestival2026」メニューの中に表示される。
        /// </summary>
        [MenuItem("VantanSchoolFestival2026/ステージチェッカー", priority = 0)]
        public static void Open()
        {
            // GetWindow … 既に開いていればそれを、無ければ新しく作って返す
            StageCheckerWindow window = GetWindow<StageCheckerWindow>();
            window.titleContent = new GUIContent("ステージチェッカー", EditorGUIUtility.IconContent("d_SceneViewTools").image);
            window.minSize = new Vector2(360, 300);
            window.RunChecks();
        }

        /// <summary>
        /// Build Settings に登録されているすべてのシーンをチェックし、結果を Console に出す。
        /// 先生が配布前に「全部のシーンが遊べる状態か」をまとめて確かめるときに便利。
        /// </summary>
        [MenuItem("VantanSchoolFestival2026/全シーンをチェック (Console に出力)", priority = 1)]
        public static void CheckAllScenes()
        {
            // 編集中のシーンに未保存の変更があれば、保存するか聞く（キャンセルされたら中止）
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string originalScene = EditorSceneManager.GetActiveScene().path;
            StageCheckerWindow checker = CreateInstance<StageCheckerWindow>();

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled) continue;

                EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                checker.RunChecks();

                int errors = checker._results.FindAll(r => r.Severity == Severity.Error).Count;
                int warnings = checker._results.FindAll(r => r.Severity == Severity.Warning).Count;
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(buildScene.path);
                Debug.Log("[ステージチェッカー] " + sceneName + " : エラー " + errors + " 件 / 警告 " + warnings + " 件");

                foreach (CheckResult r in checker._results)
                {
                    string line = "[ステージチェッカー] " + sceneName + " : " + r.Title + " … " + r.Message;
                    if (r.Severity == Severity.Error) Debug.LogError(line);
                    else if (r.Severity == Severity.Warning) Debug.LogWarning(line);
                }
            }

            DestroyImmediate(checker);

            // 元のシーンに戻す
            if (!string.IsNullOrEmpty(originalScene)) EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }

        // ---------------------------------------------------------------------
        // Unity のイベント
        // ---------------------------------------------------------------------

        private void OnEnable()
        {
            // Hierarchy の中身が変わったとき・シーンを開いたときに呼ばれるよう登録
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            RunChecks();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnHierarchyChanged()
        {
            if (_autoRefresh) RunChecks();
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            RunChecks();
        }

        // ---------------------------------------------------------------------
        // 画面を描く（OnGUI は画面の更新が必要なたびに何度も呼ばれる）
        // ---------------------------------------------------------------------

        private void OnGUI()
        {
            // --- 上部のツールバー ---
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("再チェック", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RunChecks();
                }

                _autoRefresh = GUILayout.Toggle(_autoRefresh, "自動で再チェック", EditorStyles.toolbarButton, GUILayout.Width(110));

                // FlexibleSpace … 残りの幅を空ける（以降の要素を右に寄せる）
                GUILayout.FlexibleSpace();
            }

            // --- まとめ ---
            int errors = _results.FindAll(r => r.Severity == Severity.Error).Count;
            int warnings = _results.FindAll(r => r.Severity == Severity.Warning).Count;

            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName)) sceneName = "(未保存のシーン)";

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("シーン: " + sceneName, EditorStyles.boldLabel);

            if (errors > 0)
            {
                EditorGUILayout.HelpBox("エラーが " + errors + " 件あります。このままでは遊べません。「修正」ボタンを試してみましょう。", MessageType.Error);
            }
            else if (warnings > 0)
            {
                EditorGUILayout.HelpBox("遊べますが、警告が " + warnings + " 件あります。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("問題はありません。再生ボタン ▶ で遊んでみましょう！", MessageType.Info);
            }

            EditorGUILayout.Space();

            // --- 結果の一覧（スクロールできるようにする） ---
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CheckResult result in _results)
            {
                DrawResult(result);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>チェック結果 1 行を描く。</summary>
        private void DrawResult(CheckResult result)
        {
            // "box" スタイルで枠を付けて、1 件ずつ見やすくする
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                // アイコン
                GUILayout.Label(GetIcon(result.Severity), GUILayout.Width(20), GUILayout.Height(20));

                // タイトルと説明
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(result.Title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedMiniLabel);
                }

                // 「選択」ボタン
                if (result.Target != null && GUILayout.Button("選択", GUILayout.Width(44)))
                {
                    Selection.activeObject = result.Target;   // Hierarchy で選択
                    EditorGUIUtility.PingObject(result.Target); // 黄色く光らせて場所を教える
                    if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
                }

                // 「修正」ボタン
                if (result.Fix != null && GUILayout.Button(result.FixLabel ?? "修正", GUILayout.Width(80)))
                {
                    result.Fix();
                    // シーンが変更されたことを Unity に伝える（保存し忘れ防止の * マークが付く）
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    RunChecks();
                    // ボタンの処理中にリストが変わったので、今回の描画はここで打ち切る
                    GUIUtility.ExitGUI();
                }
            }
        }

        /// <summary>重さに合ったアイコンを返す（Unity 内蔵のアイコンを使用）。</summary>
        private static GUIContent GetIcon(Severity severity)
        {
            switch (severity)
            {
                case Severity.Error: return EditorGUIUtility.IconContent("console.erroricon.sml");
                case Severity.Warning: return EditorGUIUtility.IconContent("console.warnicon.sml");
                case Severity.Info: return EditorGUIUtility.IconContent("console.infoicon.sml");
                default: return EditorGUIUtility.IconContent("TestPassed");
            }
        }

        // ---------------------------------------------------------------------
        // チェック本体
        // ---------------------------------------------------------------------

        /// <summary>すべてのチェックをやり直す。</summary>
        private void RunChecks()
        {
            _results.Clear();

            CheckPlayer();
            CheckPlayerInput();
            CheckStartPoint();
            CheckGoal();
            CheckGameManager();
            CheckCamera();
            CheckKillZone();
            CheckUI();
            CheckPostProcess();
            CheckMissingScripts();
            CheckCheckpoints();

            // エラー → 警告 → 情報 → OK の順に並べ替える
            _results.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            Repaint();   // 画面を描き直す
        }

        /// <summary>プレイヤー（RCCar）のチェック。</summary>
        private void CheckPlayer()
        {
            RCCar[] cars = FindInScene<RCCar>();

            if (cars.Length == 0)
            {
                Add(Severity.Error, "プレイヤー", "RCCar が付いたオブジェクトがシーンにありません。",
                    null, "車を置く", () => SpawnPrefab(PlayerPrefabPath));
                return;
            }

            if (cars.Length > 1)
            {
                // 2 台目以降をそれぞれ報告する
                for (int i = 1; i < cars.Length; i++)
                {
                    Add(Severity.Error, "プレイヤーが " + cars.Length + " 台", "RCCar はシーンに 1 台だけにしてください。「" + cars[i].name + "」を削除するか、RCCar を外しましょう。",
                        cars[i].gameObject);
                }
            }

            RCCar car = cars[0];
            Add(Severity.Ok, "プレイヤー", "「" + car.name + "」がプレイヤーです。", car.gameObject);

            // --- 駆動方式 ---
            DriveBase[] drives = car.GetComponents<DriveBase>();
            if (drives.Length == 0)
            {
                Add(Severity.Error, "駆動方式", "駆動方式の部品が付いていないので車が動きません。Add Component > VantanSchoolFestival2026 > 駆動方式 から 1 つ付けてください。",
                    car.gameObject, "Arcadeを付ける", () => Undo.AddComponent<ArcadeDrive>(car.gameObject));
            }
            else if (drives.Length > 1)
            {
                Add(Severity.Error, "駆動方式が " + drives.Length + " 個", "駆動方式は 1 つだけ付けてください。複数あると動きがおかしくなります。",
                    car.gameObject);
            }
            else
            {
                Add(Severity.Ok, "駆動方式", drives[0].GetType().Name + " で走ります。", car.gameObject);
            }

            // --- 当たり判定 ---
            if (car.GetComponentInChildren<Collider>() == null)
            {
                Add(Severity.Error, "車の当たり判定", "車に Collider がありません。床をすり抜けて落ちてしまいます。",
                    car.gameObject, "Box Colliderを付ける", () => Undo.AddComponent<BoxCollider>(car.gameObject));
            }
        }

        /// <summary>
        /// 入力（PlayerController）のチェック。
        /// 入力は車とは別のオブジェクトに付いていて、PlayerCarInput が車に命令を送る。
        /// </summary>
        private void CheckPlayerInput()
        {
            PlayerCarInput[] inputs = FindInScene<PlayerCarInput>();

            if (inputs.Length == 0)
            {
                Add(Severity.Error, "入力 (PlayerController)", "PlayerCarInput がありません。キーボードで車を操作できません。",
                    null, "PlayerControllerを置く", () => SpawnPrefab(PlayerControllerPrefabPath));
                return;
            }

            if (inputs.Length > 1)
            {
                Add(Severity.Warning, "入力が " + inputs.Length + " 個", "PlayerCarInput は 1 つにしてください。同じ車に 2 か所から命令が届きます。", inputs[1].gameObject);
            }

            PlayerCarInput input = inputs[0];
            PlayerInput playerInput = input.GetComponent<PlayerInput>();

            if (playerInput.actions == null)
            {
                Add(Severity.Error, "入力 (PlayerInput)", "PlayerInput の Actions が空です。操作できません。",
                    input.gameObject, "Actionsを設定", () =>
                    {
                        Undo.RecordObject(playerInput, "Set Input Actions");
                        playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                        playerInput.defaultActionMap = "Driving";
                    });
            }
            else if (playerInput.notificationBehavior != PlayerNotifications.SendMessages)
            {
                Add(Severity.Warning, "入力 (PlayerInput)", "Behavior が Send Messages ではありません。PlayerCarInput に入力が届きません。",
                    input.gameObject, "Send Messagesに", () =>
                    {
                        Undo.RecordObject(playerInput, "Set Notification Behavior");
                        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
                    });
            }
            else
            {
                // 操作する車が指定されているか（空なら実行時に自動で探す）
                string target = input.TargetCar != null ? "「" + input.TargetCar.name + "」を操作します。" : "Target Car が空なので、再生時にシーンの RCCar を自動で探します。";
                Add(Severity.Ok, "入力 (PlayerController)", "Default Map: " + playerInput.defaultActionMap + "。" + target, input.gameObject);
            }
        }

        /// <summary>スタート地点のチェック。</summary>
        private void CheckStartPoint()
        {
            StartPoint[] points = FindInScene<StartPoint>();

            if (points.Length == 0)
            {
                Add(Severity.Warning, "スタート地点", "StartPoint がありません。車は置いた場所からスタートします。",
                    null, "スタートを置く", () => SpawnPrefab(StartPointPrefabPath));
                return;
            }

            if (points.Length > 1)
            {
                Add(Severity.Warning, "スタート地点が " + points.Length + " 個", "StartPoint は 1 つにしてください。どれが使われるかわかりません。", points[1].gameObject);
            }

            StartPoint start = points[0];

            // スタート地点の下に床があるか、見えない光線で調べる
            bool hasGround = Physics.Raycast(start.transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore);
            if (!hasGround)
            {
                Add(Severity.Warning, "スタート地点の床", "スタート地点の下に床がありません。スタートした瞬間に落ちてしまいます。", start.gameObject);
            }
            else
            {
                Add(Severity.Ok, "スタート地点", "「" + hit.collider.name + "」の上からスタートします。", start.gameObject);
            }
        }

        /// <summary>ゴールのチェック。</summary>
        private void CheckGoal()
        {
            Goal[] goals = FindInScene<Goal>();

            if (goals.Length == 0)
            {
                Add(Severity.Error, "ゴール", "Goal がありません。ゴールできないゲームになってしまいます。",
                    null, "ゴールを置く", () => SpawnPrefab(GoalPrefabPath));
                return;
            }

            foreach (Goal goal in goals)
            {
                Collider collider = goal.GetComponent<Collider>();
                if (collider != null && !collider.isTrigger)
                {
                    Add(Severity.Warning, "ゴールのトリガー", "「" + goal.name + "」の Collider の Is Trigger が OFF です。ゴールが壁のようにぶつかってしまいます（再生中は自動で ON になります）。",
                        goal.gameObject, "Triggerにする", () =>
                        {
                            Undo.RecordObject(collider, "Set Trigger");
                            collider.isTrigger = true;
                        });
                }
                else
                {
                    Add(Severity.Ok, "ゴール", "「" + goal.name + "」があります。", goal.gameObject);
                }
            }
        }

        /// <summary>GameManager のチェック。</summary>
        private void CheckGameManager()
        {
            GameManager[] managers = FindInScene<GameManager>();

            if (managers.Length == 0)
            {
                Add(Severity.Error, "ゲーム進行", "GameManager がありません。タイム計測やゴール判定が動きません。",
                    null, "GameManagerを置く", () => SpawnPrefab(GameManagerPrefabPath));
            }
            else if (managers.Length > 1)
            {
                Add(Severity.Error, "GameManager が " + managers.Length + " 個", "GameManager は 1 つだけにしてください。", managers[1].gameObject);
            }
            else
            {
                Add(Severity.Ok, "ゲーム進行", "GameManager があります。", managers[0].gameObject);
            }
        }

        /// <summary>
        /// カメラのチェック。
        /// カメラは Unity 公式の Cinemachine で車を追いかける。
        ///   Main Camera … CinemachineBrain（Cinemachine の指示どおりに動く「本物のカメラ」）
        ///   FollowCamera … CinemachineCamera（どこから・何を映すかを決める「仮想カメラ」）
        /// </summary>
        private void CheckCamera()
        {
            Camera main = Camera.main;   // Tag が MainCamera のカメラ
            if (main == null)
            {
                Add(Severity.Error, "カメラ", "Tag が MainCamera のカメラがありません。画面に何も映りません。");
                return;
            }

            if (main.GetComponent<CinemachineBrain>() == null)
            {
                Add(Severity.Warning, "カメラ (Brain)", "メインカメラに CinemachineBrain がありません。Cinemachine のカメラが反映されません。",
                    main.gameObject, "Brainを付ける", () => Undo.AddComponent<CinemachineBrain>(main.gameObject));
            }

            CinemachineCamera[] vcams = FindInScene<CinemachineCamera>();
            if (vcams.Length == 0)
            {
                Add(Severity.Warning, "追従カメラ", "CinemachineCamera がありません。カメラが車を追いかけません。",
                    null, "追従カメラを置く", () => SpawnPrefab(FollowCameraPrefabPath));
                return;
            }

            RCCar car = FindInScene<RCCar>().Length > 0 ? FindInScene<RCCar>()[0] : null;
            foreach (CinemachineCamera vcam in vcams)
            {
                // Follow（追いかける対象）が空だと、カメラは動かない
                if (vcam.Follow == null)
                {
                    Add(Severity.Warning, "追従カメラの対象", "「" + vcam.name + "」の Tracking Target が空です。車を追いかけません。",
                        vcam.gameObject, car != null ? "車を設定" : null, car == null ? null : (Action)(() =>
                        {
                            Undo.RecordObject(vcam, "Set Tracking Target");
                            vcam.Follow = car.transform;
                        }));
                }
                else
                {
                    Add(Severity.Ok, "カメラ", "「" + vcam.name + "」が「" + vcam.Follow.name + "」を追いかけます。", vcam.gameObject);
                }
            }
        }

        /// <summary>落下ゾーンのチェック。</summary>
        private void CheckKillZone()
        {
            if (FindInScene<KillZone>().Length == 0)
            {
                Add(Severity.Warning, "落下ゾーン", "KillZone がありません。コースから落ちると戻れなくなります（R キーで戻ることはできます）。",
                    null, "落下ゾーンを置く", () => SpawnPrefab(KillZonePrefabPath));
            }
            else
            {
                Add(Severity.Ok, "落下ゾーン", "落ちても自動でリスポーンします。");
            }
        }

        /// <summary>UI のチェック。</summary>
        private void CheckUI()
        {
            if (FindInScene<HudPresenter>().Length == 0)
            {
                Add(Severity.Info, "画面表示 (HUD)", "HudPresenter がありません。タイムが画面に表示されません（遊ぶことはできます）。",
                    null, "UIを置く", () => SpawnPrefab(GameUIPrefabPath));
            }
            else if (FindInScene<ControlsHintPresenter>().Length == 0)
            {
                Add(Severity.Info, "操作方法の表示", "ControlsHintPresenter がありません。画面左下に操作方法が表示されません。");
            }
            else
            {
                Add(Severity.Ok, "画面表示", "タイムと操作方法が表示されます。");
            }
        }

        /// <summary>
        /// ポストプロセス（画面全体の色味やエフェクト）のチェック。
        /// URP では Volume コンポーネントに Volume Profile を設定して使う。
        /// </summary>
        private void CheckPostProcess()
        {
            Volume[] volumes = FindInScene<Volume>();
            if (volumes.Length == 0)
            {
                Add(Severity.Info, "ポストプロセス", "Volume がありません。ブルームなどの画面効果がかかりません（遊ぶことはできます）。",
                    null, "Volumeを置く", () => SpawnPrefab(GlobalVolumePrefabPath));
                return;
            }

            // カメラ側でポストプロセスが ON になっているか
            Camera main = Camera.main;
            UniversalAdditionalCameraData data = main != null ? main.GetComponent<UniversalAdditionalCameraData>() : null;
            if (data != null && !data.renderPostProcessing)
            {
                Add(Severity.Warning, "ポストプロセス", "メインカメラの Post Processing が OFF です。Volume の効果が画面に出ません。",
                    main.gameObject, "ONにする", () =>
                    {
                        Undo.RecordObject(data, "Enable Post Processing");
                        data.renderPostProcessing = true;
                    });
            }
            else
            {
                Add(Severity.Ok, "ポストプロセス", "「" + volumes[0].name + "」の Volume Profile で画面効果がかかります。", volumes[0].gameObject);
            }
        }

        /// <summary>チェックポイントの情報。</summary>
        private void CheckCheckpoints()
        {
            int count = FindInScene<Checkpoint>().Length;
            if (count > 0)
            {
                Add(Severity.Info, "チェックポイント", count + " 個あります。");
            }
        }

        /// <summary>
        /// 「Missing Script（スクリプトが見つからない）」のチェック。
        /// スクリプトファイルを削除したり名前を変えたりすると発生する。
        /// </summary>
        private void CheckMissingScripts()
        {
            // FindObjectsInactive.Include … 非表示のオブジェクトも含めて探す
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform t in all)
            {
                GameObject go = t.gameObject;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing == 0) continue;

                Add(Severity.Error, "Missing Script", "「" + go.name + "」に見つからないスクリプトが " + missing + " 個付いています。",
                    go, "取り除く", () =>
                    {
                        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    });
            }
        }

        // ---------------------------------------------------------------------
        // 便利メソッド
        // ---------------------------------------------------------------------

        /// <summary>結果を 1 件追加する。</summary>
        private void Add(Severity severity, string title, string message, Object target = null, string fixLabel = null, Action fix = null)
        {
            _results.Add(new CheckResult
            {
                Severity = severity,
                Title = title,
                Message = message,
                Target = target,
                FixLabel = fixLabel,
                Fix = fix,
            });
        }

        /// <summary>シーン内にある、指定した型のコンポーネントをすべて探す（非表示のものは除く）。</summary>
        private static T[] FindInScene<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        /// <summary>プレハブをシーンに置く。Scene ビューの中央あたりに置き、選択状態にする。</summary>
        private static void SpawnPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("ステージチェッカー", "プレハブが見つかりません:\n" + path, "OK");
                return;
            }

            // InstantiatePrefab … プレハブとのつながりを保ったままシーンに置く
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            // Undo に登録しておくと Ctrl+Z で取り消せる
            Undo.RegisterCreatedObjectUndo(instance, "Spawn " + prefab.name);

            Selection.activeGameObject = instance;
        }
    }
}
