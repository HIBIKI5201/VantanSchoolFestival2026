// =============================================================================
//  CourseArea.cs
// -----------------------------------------------------------------------------
//  【ステージ部品】コースエリア（走ってよい範囲）
//    車が走ってよい場所を表す「見えない箱」です。ゲーム画面には映りません。
//    コースに沿ってたくさん並べて使います。
//
//  【場外判定のしくみ】
//    車がコースエリアに入ると OutOfBoundsJudge（場外判定係）に「入ったよ」と知らせ、
//    出ると「出たよ」と知らせます。
//    車がどのコースエリアの中にもいない状態が続くと、OutOfBoundsJudge が
//    「コースアウト」と判断して、最後のチェックポイントへ戻します。
//
//        [CourseArea] [CourseArea] [CourseArea] …  ← コースに沿って並べる
//              └── 入った / 出た ──▶ OutOfBoundsJudge ──▶ 一定時間外にいたら Respawn
//
//  【なぜ「外側」ではなく「内側」に置くの？】
//    くねくね曲がったコースの外側を全部ふさぐのは大変です。
//    「ここは走ってよい」という箱を並べるほうが、置き忘れがあってもすぐ気づけます。
//    （Scene ビューで緑の半透明の箱として見えます）
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>車が走ってよい範囲を表すトリガー。</summary>
    [AddComponentMenu("VantanSchoolFestival2026/ステージ/コースエリア (CourseArea)")]
    [ComponentSummary(
        "車が走ってよい範囲を表す、見えない箱です。この箱から全部出てしまうと「コースアウト」になります。",
        "コースに沿ってすき間なく並べます。ジャンプする場所や動く床の上も、空中までおおうように置きましょう。シーンに OutOfBoundsJudge が 1 つ必要です。",
        "OutOfBoundsJudge / Checkpoint")]
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class CourseArea : MonoBehaviour
    {
        [Header("目印の見た目（エディタのみ）")]
        [Tooltip("Scene ビューに表示する範囲の色。ゲーム画面には映りません。")]
        [SerializeField] private Color _gizmoColor = new Color(0.2f, 1f, 0.4f, 0.08f);

        private void Awake()
        {
            // 通り抜けられるようにトリガーにする
            StageUtility.MakeTrigger(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            // 場外判定係がいれば「入ったよ」と知らせる
            if (OutOfBoundsJudge.Instance != null) OutOfBoundsJudge.Instance.EnterArea(this);
        }

        private void OnTriggerExit(Collider other)
        {
            RCCar car = StageUtility.FindCar(other);
            if (car == null) return;

            if (OutOfBoundsJudge.Instance != null) OutOfBoundsJudge.Instance.ExitArea(this);
        }

        /// <summary>範囲を半透明の箱で表示する（たくさん並ぶので薄い色にしている）。</summary>
        private void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = _gizmoColor;
            Gizmos.DrawCube(box.center, box.size);
        }

        /// <summary>選択しているときは枠線もはっきり表示する。</summary>
        private void OnDrawGizmosSelected()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
