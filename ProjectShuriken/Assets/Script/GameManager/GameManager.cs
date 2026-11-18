using System.Collections;
using UnityEngine;


/// <summary>
/// ゲーム全体の進行役
/// ・ラウンドを順番に進める(準備 → 合図 → 投げる → 結果 → 次のラウンド)
/// ・ラウンドごとの点数と合計点を管理する
/// ・ラウンドが終わるたびに、刺さった手裏剣を片付ける
/// ・合図の前に投げても手裏剣は飛ぶが、点数は入らない
/// </summary>
public class GameManager : MonoBehaviour
{
    // ゲームの状態
    public enum State
    {
        BeforeStart,    // ゲーム開始前
        WaitSignal,     // 合図待ち
        Signal,         // 合図が出た
        RoundResult,    // ラウンドの結果を見せている
        Finished,       // 全ラウンド終了
    }


    public enum ResultType
    {
        Hit,    // 命中
        Miss,   // 外れ
        Foul,   // フライング(合図の前に投げた)
        TimeUp, // 時間切れ(合図の後、時間内に投げなかった)
    }


    // 1ラウンド分の結果
    public struct RoundResult
    {
        public int round;           // 何ラウンド目か(1から)
        public ResultType type;     // 結果の種類
        public int score;           // このラウンドの点数
    }


    public event System.Action<int> OnRoundStart;           // ラウンド開始(何ラウンド目か)
    public event System.Action OnSignal;                    // 合図が出た
    public event System.Action<RoundResult> OnRoundEnd;     // ラウンド結果
    public event System.Action<int> OnGameEnd;              // ゲーム終了


    [Header("参照")]
    [Tooltip("手裏剣を投げる担当")]
    [SerializeField] ShurikenThrower thrower;

    [Header("ラウンド設定")]
    [Tooltip("ラウンド数")]
    [SerializeField] int maxRounds = 3;

    [Tooltip("ゲーム開始から1ラウンド目が始まるまでの秒数")]
    [SerializeField] float startDelay = 1f;

    [Tooltip("合図が出るまでの時間(最短)。毎ラウンドこの範囲でランダムに決まる")]
    [SerializeField] float minSignalDelay = 1.5f;

    [Tooltip("合図が出るまでの時間(最長)")]
    [SerializeField] float maxSignalDelay = 4f;

    [Tooltip("合図の後、この秒数たっても投げなければ時間切れ(0点)")]
    [SerializeField] float throwTimeLimit = 3f;

    [Tooltip("ラウンドの結果を見せる秒数。この後に手裏剣を片付けて次のラウンドへ")]
    [SerializeField] float resultDuration = 2f;


    // 外から読み取れる情報(UI用)
    public State CurrentState { get; private set; } = State.BeforeStart;
    public int CurrentRound { get; private set; }    // 今何ラウンド目か
    public int TotalScore { get; private set; }     // 合計点


    // 1ラウンドの中で使う記録
    bool hasThrown;     // このラウンドで投げたか
    bool isFoul;        // 合図の前に投げたか
    bool hasResult;     // 命中/はずれの結果が届いたか
    bool lastWasHit;    // 届いた結果が命中か
    int lastHitScore;   // 命中した時の的の点数


    // 有効になった時に、各イベントへ登録する
    void OnEnable()
    {
        if (thrower != null) thrower.OnThrow += HandleThrown;
        Shuriken.OnHit += HandleHit;
        Shuriken.OnMiss += HandleMiss;
    }


    void OnDisable()
    {
        if (thrower != null) thrower.OnThrow -= HandleThrown;
        Shuriken.OnHit -= HandleHit;
        Shuriken.OnMiss -= HandleMiss;
    }


    void Start()
    {
        StartCoroutine(GameLoop());
    }


    IEnumerator GameLoop()
    {
        // 開始前は投げられないようにしておく
        thrower.Lock();
        TotalScore = 0;

        yield return new WaitForSeconds(startDelay);


        for(CurrentRound = 1; CurrentRound <= maxRounds; CurrentRound++)
        {
            yield return PlayRound();
        }


        CurrentState = State.Finished;
        thrower.Lock();
        Debug.Log($"ゲーム終了。合計点: {TotalScore}");
        OnGameEnd?.Invoke(TotalScore);
    }


    IEnumerator PlayRound()
    {
        ClearShurikens();
        ResetRoundRecord();
        thrower.ResetThrow();   // 合図の前から投げられる

        CurrentState = State.WaitSignal;
        Debug.Log($"ラウンド{CurrentRound} / {maxRounds}");
        OnRoundStart?.Invoke(CurrentRound);

        // ----合図待ち----
        // ランダムな時間だけ待つ。待っている間に投げたらフライング
        float signalDelay = Random.Range(minSignalDelay, maxSignalDelay);
        float timer = 0f;
        while (timer < signalDelay && !hasThrown)
        {
            timer += Time.deltaTime;
            yield return null;  // 1フレーム待つ
        }


        if (!hasThrown)
        {
            // ----合図----
            CurrentState = State.Signal;
            Debug.Log("合図！");
            OnSignal?.Invoke();

            // 投げるか、制限時間が過ぎるまで待つ
            timer = 0f;
            while (timer < throwTimeLimit && !hasThrown)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // ここから先は、もう投げられないようにする
        thrower.Lock();

        // 投げていれば、命中/はずれの結果が届くまで待つ
        // (手裏剣は最大でも maxDistance ÷ speed 秒で必ず結果が出る)
        if (hasThrown)
        {
            yield return new WaitUntil(() => hasResult);
        }


        // ----結果----
        RoundResult result = MakeRoundResult();
        TotalScore += result.score;

        CurrentState = State.RoundResult;
        Debug.Log($"ラウンド{CurrentRound}の結果: {result.type} / {result.score}点(合計 {TotalScore}点)");
        OnRoundEnd?.Invoke(result);

        // 結果を見せる時間
        yield return new WaitForSeconds(resultDuration);
    }


    // このラウンドの記録から結果を作る
    RoundResult MakeRoundResult()
    {
        RoundResult result = new RoundResult();
        result.round = CurrentRound;
        result.score = 0;

        if (isFoul)
        {
            // 合図の前に投げた → 当たっていても0点
            result.type = ResultType.Foul;
        }
        else if (!hasThrown)
        {
            // 合図の後、時間内に投げなかった
            result.type = ResultType.TimeUp;
        }
        else if (lastWasHit)
        {
            result.type = ResultType.Hit;
            result.score = lastHitScore;
        }
        else
        {
            result.type = ResultType.Miss;
        }

        return result;
    }


    // ----イベントを受け取る処理----

    // 手裏剣が投げられた時(ShurikenThrowerから)
    void HandleThrown()
    {
        // 投げてよいのは「合図待ち」と「合図」の間だけ
        if (CurrentState != State.WaitSignal && CurrentState != State.Signal) return;
        if (hasThrown) return;

        hasThrown = true;

        // 合図の前ならフライング
        if (CurrentState == State.WaitSignal)
        {
            isFoul = true;
            Debug.Log("フライング！");
        }
    }


    // 手裏剣が的に刺さった時(Shurikenから)
    void HandleHit(Target target, int score)
    {
        // 投げていない時や、すでに結果が届いている時は無視する
        if (!hasThrown || hasResult) return;

        hasResult = true;
        lastWasHit = true;
        lastHitScore = score;
    }


    // 手裏剣がはずれた時(Shurikenから)
    void HandleMiss()
    {
        if (!hasThrown || hasResult) return;

        hasResult = true;
        lastWasHit = false;
    }


    // ----ラウンドの準備----

    // シーン上にある手裏剣を全部消す(刺さっているもの・飛んでいるもの両方)
    void ClearShurikens()
    {
        Shuriken[] shurikens = FindObjectsByType<Shuriken>(FindObjectsSortMode.None);
        foreach (Shuriken s in shurikens)
        {
            Destroy(s.gameObject);
        }
    }


    // 1ラウンド分の記録をリセットする
    void ResetRoundRecord()
    {
        hasThrown = false;
        isFoul = false;
        hasResult = false;
        lastWasHit = false;
        lastHitScore = 0;
    }
}
