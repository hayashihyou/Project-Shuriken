using UnityEngine;


/// <summary>
/// 的の情報と点数計算
/// ・手裏剣が刺さった位置が中心に近いほど高得点
/// ・中心ならぴったり maxScore 点、ふちなら 0点 
/// </summary>
public class Target : MonoBehaviour
{
    [Tooltip("的の半径(TargetModel)のScale Xの半分")]
    [SerializeField] float radius = 1f;

    [Tooltip("ど真ん中に当たった時の点数")]
    [SerializeField] int maxScore = 100;


    // 当たった位置から点数を計算して返す。Shuriken.csから呼ばれる。
    public int CalcScore(Vector3 hitPoint)
    {
        // 的の中心から当たった位置までのズレ(ベクトル)
        Vector3 offset = hitPoint - transform.position;

        // 奥行き(Z)のズレは無視して、的の面の上での距離だけを見る    
        offset.z = 0f;

        // 中心からの距離を半径で割って、0～1の割合にする
        float ratio = offset.magnitude / radius;

        // 的の外なら0点
        if (ratio > 1f) return 0;

        // 中心に近いほど高得点になるよう計算する
        return Mathf.RoundToInt((1f - ratio) * maxScore);   
    }


    // Sceneビューでこのオブジェクトを選択した時、判定の範囲を黄色い円で表示する
    // 見た目の的の大きさと判定が合っているかの確認用
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
