using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// 手裏剣を投げる担当
/// ・スペースキー / 左クリック / 画面タップで手裏剣を投げる
/// ・1ラウンドにつき一回しか投げられない
/// </summary>
public class ShurikenThrower : MonoBehaviour
{
    [Tooltip("生成する手裏剣のプレハブ")]
    [SerializeField] Shuriken shurikenPrefab;

    [Tooltip("手裏剣が出てくる位置と向き")]
    [SerializeField] Transform spawnPoint;


    bool hasThrown = false;     // 投げたかどうか(trueなら投げられない)


    // 更新処理
    void Update()
    {
        // 既に投げていたら、入力を受け付けずに終了
        if (hasThrown) return;

        // 「押した瞬間」だけtrueになる
        // Keyboard.currentがnullになるのは、キーボードが接続されていない場合
        // Touchscreen.currentはスマホ・タブレットの画面タッチ(どこをタップしても投げる)
        bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                       (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                       (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

        if (pressed) Throw();
    }


    // 手裏剣を投げる
    void Throw()
    {
        hasThrown = true;   // 投げたので、以降は入力を無視する

        // プレハブのコピーをSpawnPointの位置・向きで生成する
        // 生成された手裏剣はShuriken.csによって自動で前に飛んでいく
        Instantiate(shurikenPrefab, spawnPoint.position, spawnPoint.rotation);
    }


    // もう一度投げられる状態に戻す。次のラウンドを始めるときにGameManagerから呼ぶ予定
    public void ResetThrow()
    {
        hasThrown = false;
    }
}
