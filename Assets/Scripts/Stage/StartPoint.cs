// =============================================================================
//  StartPoint.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】スタート地点
//    ゲーム開始時、車はこのオブジェクトの位置・向きに置かれます。
//    青い矢印の向き（Z 軸 = 前）が車の向きになります。
//
//    見た目は持たず、Scene ビューにだけ目印(Gizmo)が表示されます。
//    シーンに 1 つだけ置いてください。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車がスタートする位置と向き。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/スタート地点 (StartPoint)")]
    [ComponentSummary(
        "車がスタートする場所と向きを決める目印です。ゲーム画面には映りません。",
        "シーンに 1 つだけ置きます。青い矢印の向きが、車の向きになります。",
        "GameManager")]
    [DisallowMultipleComponent]
    public class StartPoint : MonoBehaviour
    {
        [Header("目印の見た目（エディタのみ）")]
        [Tooltip("Scene ビューに表示する目印の色。ゲーム画面には映りません。")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 0.6f, 1f, 0.8f);

        /// <summary>
        /// OnDrawGizmos … Scene ビューに常に図形を描く（選択していなくても表示される）。
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            // 車くらいの大きさの箱を描く
            // Gizmos.matrix を設定すると、オブジェクトの回転に合わせて描ける
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 0.25f, 0f), new Vector3(1f, 0.5f, 1.6f));

            // 前向きの矢印
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 2f);
            Gizmos.DrawLine(Vector3.forward * 2f, new Vector3(-0.3f, 0f, 1.6f));
            Gizmos.DrawLine(Vector3.forward * 2f, new Vector3(0.3f, 0f, 1.6f));
        }
    }
}
