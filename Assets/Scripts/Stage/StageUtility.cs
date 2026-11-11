// =============================================================================
//  StageUtility.cs
// -----------------------------------------------------------------------------
//  【役割】
//    ステージギミック（ゴール・加速床など）で共通して使う便利メソッド集。
//
//  【static クラスとは？】
//    GameObject に付けずに使える「道具箱」です。
//    StageUtility.FindCar(other) のように、クラス名から直接呼び出します。
// =============================================================================

using UnityEngine;

namespace VantanSchoolFestival2026
{
    /// <summary>ステージギミック用の便利メソッド。</summary>
    public static class StageUtility
    {
        /// <summary>
        /// ぶつかった Collider から RCCar を探す。車でなければ null を返す。
        /// </summary>
        /// <remarks>
        /// 車はタイヤやボディなど複数の子オブジェクトでできていることがあるので、
        /// Collider そのものではなく attachedRigidbody（その Collider を動かしている
        /// Rigidbody）から RCCar を探している。
        /// </remarks>
        public static RCCar FindCar(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (body == null) return null;
            return body.GetComponent<RCCar>();
        }

        /// <summary>
        /// Collider を「トリガー」にする。トリガーはぶつからずに通り抜け、
        /// 通り抜けたことだけを OnTriggerEnter で知らせてくれる。
        /// </summary>
        public static void MakeTrigger(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;
        }
    }
}
