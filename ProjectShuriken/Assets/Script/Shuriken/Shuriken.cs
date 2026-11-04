using UnityEngine;


/// <summary>
/// 投げられた手裏剣1つ分の動き
/// ・向いている方向(前方)へまっすぐ飛ぶ
/// ・的に当たったら刺さって止まり、点数を計算する
/// ・一定距離飛んでも当たらなければ「はずれ」として消える
/// </summary>
public class Shuriken : MonoBehaviour
{
    // ----結果の通知----
    // GameManagerやUIはこのイベントに登録しておけば、命中/はずれを受け取れる
    // 例: Shuriken.OnHit += (target, score) => { ... };

    /// <summary>的に刺さった時に呼ばれる(刺さった的, 点数)</summary>
    public static event System.Action<Target, int> OnHit;

    /// <summary>はずれて消える時に呼ばれる</summary>
    public static event System.Action OnMiss;


    [Tooltip("飛ぶ速さ")]
    [SerializeField] float speed = 20f;

    [Tooltip("この距離を超えたらはずれ扱い")]
    [SerializeField] float maxDistance = 30f;

    [Tooltip("回転させる見た目(子のModel")]
    [SerializeField] Transform model;

    [Tooltip("回転の速さ(1秒あたりの角度 1080 = 3回転)")]
    [SerializeField] float spinSpeed = 1080f;


    float traveld;  // これまでに飛んだ距離
    bool isStuck;   // 的に刺さったかどうか


    // 更新処理
    void Update()
    {
        // 刺さった後は何もしない
        if (isStuck) return;

        // このフレームで進む距離。TimedeltaTimeを掛けることで、PCの性能(フレームレート)関係なく同じ速さで飛ぶ
        float step = speed * Time.deltaTime;


        // ----当たり判定----
        // 進む前に、これから進む距離の分だけ前方へレイを飛ばしてす。線の途中にColliderがあれば当たったと判定する。
        // ※手裏剣は速いので、普通のCollider動詞の判定だと薄い的のを1フレームで通り過ぎてしまう事がある。
        //   レイなら確実に検知でき、当たった正確な位置(hit.point)も分かる
        if(Physics.Raycast(transform.position,transform.forward,out RaycastHit hit,step))
        {
            // 当たったCollider(TargetModel)から親をたどってTarget.csを探す
            Target target = hit.collider.GetComponentInParent<Target>();

            if(target != null)
            {
                StickTo(target, hit.point);
                return;
            }
        }


        // ----移動----
        // 当たっていなければ前方へ進む
        transform.position += transform.forward * step;
        traveld += step;

        // 見た目だけを回転させる(親を回すと飛ぶ方向まで変わってしまうため)
        if (model != null) model.Rotate(0f, spinSpeed * Time.deltaTime, 0f);

        // ----はずれ判定----
        // 一定距離飛んでも当たらなかったらはずれ。手裏剣を削除する
        if(traveld >= maxDistance)
        {
            Debug.Log("はずれ");
            OnMiss?.Invoke();   // はずれたことを通知
            Destroy(gameObject);
        }
    }


    // 的に刺さった時の処理
    void StickTo(Target target, Vector3 point)
    {
        isStuck = true;

        // 当たった位置にぴったり移動する
        transform.position = point;

        // 的の子オブジェクトにすることで、動く的と一緒に移動するようになる
        transform.SetParent(target.transform, true);

        int score = target.CalcScore(point);
        Debug.Log($"命中！{score}点");
        OnHit?.Invoke(target, score);   // 命中と点数を通知
    }
}
