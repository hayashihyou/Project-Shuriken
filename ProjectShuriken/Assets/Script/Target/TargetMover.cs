using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// 的を左右に往復させる
/// </summary>
public class TargetMover : MonoBehaviour
{
    public float amplitude = 3f; // 振幅。大きいほど大きく振れる
    public float period = 2.4f; // 1往復にかかる時間。小さいほど早く動く

    Vector3 center; // 中心位置
    float elapsedTime; // 経過時間

    // 動き始めてからの秒数(読み取り専用)
    public float Elapsed
    {
        get { return elapsedTime; }
    }

    // 再生開始時に1回だけ呼ばれる
    private void Start()
    {
        // 中心位置を記録
        center = transform.position; 
    }

    // 毎フレーム呼ばれる
    private void Update()
    {
        // 経過時間を更新
        elapsedTime += Time.deltaTime;
        // サイン波で左右に往復させる
        transform.position = center + Vector3.right * OffsetAt(elapsedTime);
    }

    public float OffsetAt(float time)
    {
        // periodが0以下の場合は0を返す
        if (period <= 0f) return 0f;

        // サイン波は-1～1を行き来する
        float wave = Mathf.Sin(2 * Mathf.PI * time / period);

        return amplitude * wave;
    }
}
