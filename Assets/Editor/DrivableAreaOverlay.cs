// =============================================================================
//  DrivableAreaOverlay.cs   ※ Editor フォルダの中のスクリプトはゲームには含まれません
// -----------------------------------------------------------------------------
//  【役割】
//    Scene ビューに「車が走れる場所」と「段差」を色付きのマスで表示する。
//    ステージチェッカーの「走れる場所を表示」ボタンで ON / OFF します。
//
//        緑 … 走れる
//        黄 … 段差があるけど、車が自動で乗り越えられる
//        赤 … 乗り越えられない段差・急すぎる坂（車が止まる・登れない）
//        紫 … 落ちたら戻される場所（KillZone の中）
//
//  【しくみ】
//    Scene ビューで見ている場所のまわりを、1 マスずつ「真上から光線(Ray)」を当てて
//    地面の高さと傾きを調べます。となりのマスとの高さの差が大きいところを「段差」と判断します。
//    坂のように少しずつ高くなっている所は、傾きから予想した高さと比べて段差とは区別しています。
// =============================================================================

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VantanSchoolFestival2026.EditorTools
{
    /// <summary>Scene ビューに走れる場所と段差を表示する。</summary>
    public static class DrivableAreaOverlay
    {
        // ---------------------------------------------------------------------
        // 設定（ステージチェッカーから変える）
        // ---------------------------------------------------------------------

        /// <summary>表示中か。</summary>
        public static bool Enabled { get; private set; }

        /// <summary>調べる範囲（見ている場所からの半径、メートル）。</summary>
        public static float Radius = 30f;

        /// <summary>マスの大きさ（メートル）。</summary>
        public static float CellSize = 1f;

        /// <summary>車が登れる坂の角度（度）。これより急だと赤。</summary>
        public static float MaxSlope = 35f;

        /// <summary>乗り越えられる段差の高さ（シーンの車から読む。車が無ければこの値）。</summary>
        public static float MaxStep { get; private set; } = 0.3f;

        // 色（半透明）
        public static readonly Color DrivableColor = new Color(0.2f, 0.85f, 0.3f, 0.35f);
        public static readonly Color SmallStepColor = new Color(1f, 0.85f, 0.1f, 0.6f);
        public static readonly Color BlockedColor = new Color(1f, 0.2f, 0.15f, 0.6f);
        public static readonly Color FallColor = new Color(0.65f, 0.3f, 1f, 0.45f);

        // これより小さい高さの差は「段差なし」とみなす（地面のでこぼこ）
        private const float FlatTolerance = 0.05f;

        // ---------------------------------------------------------------------
        // 調べた結果
        // ---------------------------------------------------------------------

        private struct Cell
        {
            public bool Valid;      // 地面が見つかったか
            public Vector3 Point;   // 地面の位置
            public Vector3 Normal;  // 地面の向き
            public Color Color;     // 表示する色
        }

        private static Cell[,] _cells;
        private static Vector3 _center;
        private static bool _dirty = true;

        /// <summary>前回調べたときの、色ごとのマスの数（ステージチェッカーに表示する）。</summary>
        public static int DrivableCount { get; private set; }
        public static int SmallStepCount { get; private set; }
        public static int BlockedCount { get; private set; }
        public static int FallCount { get; private set; }

        // ---------------------------------------------------------------------
        // ON / OFF
        // ---------------------------------------------------------------------

        /// <summary>表示を ON / OFF する。</summary>
        public static void SetEnabled(bool enabled)
        {
            if (enabled == Enabled) return;
            Enabled = enabled;

            if (enabled)
            {
                // Scene ビューを描くたびに OnSceneGUI を呼んでもらう
                SceneView.duringSceneGui += OnSceneGUI;
                EditorApplication.hierarchyChanged += MarkDirty;
                Undo.undoRedoPerformed += MarkDirty;
                // オブジェクトを動かしたり大きさを変えたりしたときにも調べ直す
                ObjectChangeEvents.changesPublished += OnObjectChanged;
                MarkDirty();
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                EditorApplication.hierarchyChanged -= MarkDirty;
                Undo.undoRedoPerformed -= MarkDirty;
                ObjectChangeEvents.changesPublished -= OnObjectChanged;
                _cells = null;
            }
            SceneView.RepaintAll();
        }

        private static void OnObjectChanged(ref ObjectChangeEventStream stream) => MarkDirty();

        /// <summary>調べ直す（オブジェクトを動かしたときなど）。</summary>
        public static void MarkDirty()
        {
            _dirty = true;
            SceneView.RepaintAll();
        }

        // ---------------------------------------------------------------------
        // Scene ビューに描く
        // ---------------------------------------------------------------------

        private static void OnSceneGUI(SceneView view)
        {
            // 見ている場所が大きく変わったら、そのまわりを調べ直す
            Vector3 pivot = view.pivot;
            Vector2 moved = new Vector2(pivot.x - _center.x, pivot.z - _center.z);
            if (_dirty || _cells == null || moved.magnitude > Radius * 0.3f) Rebuild(pivot);

            if (Event.current.type != EventType.Repaint) return;

            // 物のうしろに隠れたマスは描かない
            Handles.zTest = CompareFunction.LessEqual;
            float h = CellSize * 0.46f;
            var corners = new Vector3[4];
            foreach (Cell cell in _cells)
            {
                if (!cell.Valid) continue;

                // マスの 4 つの角を、地面の傾きに合わせて少し浮かせて描く
                Vector3 n = cell.Normal;
                for (int i = 0; i < 4; i++)
                {
                    float dx = (i == 0 || i == 3) ? -h : h;
                    float dz = (i < 2) ? -h : h;
                    float dy = n.y > 0.01f ? -(n.x * dx + n.z * dz) / n.y : 0f;
                    corners[i] = cell.Point + new Vector3(dx, dy + 0.06f, dz);
                }
                Handles.color = cell.Color;
                Handles.DrawAAConvexPolygon(corners);
            }
            Handles.zTest = CompareFunction.Always;

            DrawLegend();
        }

        /// <summary>Scene ビューの左下に、色の意味を表示する。</summary>
        private static void DrawLegend()
        {
            Handles.BeginGUI();
            var rect = new Rect(10, Screen.height / EditorGUIUtility.pixelsPerPoint - 150, 250, 112);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12));
            GUILayout.Label("走れる場所の表示（ステージチェッカー）", EditorStyles.miniBoldLabel);
            LegendRow(DrivableColor, "走れる");
            LegendRow(SmallStepColor, "段差（" + MaxStep.ToString("0.##") + "m まで乗り越えられる）");
            LegendRow(BlockedColor, "乗り越えられない段差・急な坂");
            LegendRow(FallColor, "落ちたら戻される場所");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void LegendRow(Color color, string text)
        {
            GUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14));
            EditorGUI.DrawRect(swatch, new Color(color.r, color.g, color.b, 1f));
            GUILayout.Label(text, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------------
        // 地面を調べる
        // ---------------------------------------------------------------------

        /// <summary>center のまわりを 1 マスずつ調べて、色を決める。</summary>
        public static void Rebuild(Vector3 center)
        {
            _dirty = false;
            _center = center;
            DrivableCount = SmallStepCount = BlockedCount = FallCount = 0;

            // 乗り越えられる段差の高さは、シーンにある車の設定を使う
            RCCar car = Object.FindFirstObjectByType<RCCar>();
            MaxStep = car != null ? car.MaxStepHeight : 0.3f;

            int count = Mathf.CeilToInt(Radius * 2f / CellSize) + 1;
            _cells = new Cell[count, count];
            float start = -Radius;
            float minSlopeY = Mathf.Cos(MaxSlope * Mathf.Deg2Rad);

            // 1 回目：各マスの地面の高さと傾きを調べる
            for (int z = 0; z < count; z++)
            {
                for (int x = 0; x < count; x++)
                {
                    var origin = new Vector3(center.x + start + x * CellSize, center.y + 200f, center.z + start + z * CellSize);
                    if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f, ~0, QueryTriggerInteraction.Ignore)) continue;

                    // 車自身は地面として数えない
                    if (hit.collider.GetComponentInParent<RCCar>() != null) continue;

                    _cells[x, z] = new Cell { Valid = true, Point = hit.point, Normal = hit.normal };
                }
            }

            // 2 回目：となりのマスと比べて、段差・坂・落下を判断する
            for (int z = 0; z < count; z++)
            {
                for (int x = 0; x < count; x++)
                {
                    if (!_cells[x, z].Valid) continue;
                    Cell cell = _cells[x, z];

                    Color color;
                    if (FallsIntoKillZone(cell.Point))
                    {
                        color = FallColor;
                    }
                    else if (cell.Normal.y < minSlopeY)
                    {
                        color = BlockedColor;   // 急すぎる坂
                    }
                    else
                    {
                        float step = MaxStepToNeighbors(x, z, count);
                        color = step > MaxStep ? BlockedColor : step > FlatTolerance ? SmallStepColor : DrivableColor;
                    }
                    _cells[x, z].Color = color;

                    // 色ごとに数える
                    if (color == FallColor) FallCount++;
                    else if (color == BlockedColor) BlockedCount++;
                    else if (color == SmallStepColor) SmallStepCount++;
                    else DrivableCount++;
                }
            }
        }

        /// <summary>
        /// となり（前後左右）のマスとの「段差の高さ」のうち、いちばん大きいものを返す。
        /// 坂の場合は、傾きから予想した高さとの差だけを数える（坂そのものは段差ではない）。
        /// </summary>
        private static float MaxStepToNeighbors(int x, int z, int count)
        {
            Cell cell = _cells[x, z];
            float maxStep = 0f;
            int[] dxs = { 1, -1, 0, 0 };
            int[] dzs = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dxs[i], nz = z + dzs[i];
                if (nx < 0 || nz < 0 || nx >= count || nz >= count || !_cells[nx, nz].Valid) continue;

                Cell other = _cells[nx, nz];
                Vector3 offset = other.Point - cell.Point;
                // このマスの傾きのまま進んだら、となりはこの高さのはず
                float expected = cell.Normal.y > 0.01f ? -(cell.Normal.x * offset.x + cell.Normal.z * offset.z) / cell.Normal.y : 0f;
                maxStep = Mathf.Max(maxStep, Mathf.Abs(offset.y - expected));
            }
            return maxStep;
        }

        /// <summary>
        /// その場所に車が落ちていくと、地面に着く前に KillZone（落ちたら戻される場所）に入るか。
        /// （川や湖は、水面の少し下に KillZone を敷いているので、川底より先に KillZone に入る）
        /// </summary>
        private static bool FallsIntoKillZone(Vector3 ground)
        {
            Vector3 origin = new Vector3(ground.x, _center.y + 200f, ground.z);
            // RaycastAll … 光線が通り抜けたものを全部調べる（トリガーも含める）
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 400f, ~0, QueryTriggerInteraction.Collide))
            {
                if (hit.point.y >= ground.y - 0.01f && hit.collider.GetComponent<KillZone>() != null) return true;
            }
            return false;
        }
    }
}
