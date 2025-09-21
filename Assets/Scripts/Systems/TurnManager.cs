using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("Refs")]
    public HandManager hand;          // 拖 HandManager
    public TableManager table;        // 拖 TableManager（读台面顶牌）
    public DeckManager deck;          // 拖 DeckManager（清台丢弃）

    [Header("HUD")]
    public TMP_Text tubeText;         // 拖 HUD/TubeText
    public TMP_Text boxText;          // 拖 HUD/BoxText
    public TMP_Text coinsText;        // 拖 HUD/CoinsText
    public Button endTurnBtn;         // 拖 HUD/EndTurnBtn
    public Button endRoundBtn;        // 拖 HUD/EndRoundBtn
    public Button passBtn;            // 拖 HUD/PassBtn

    [Header("Mode")]
    [SerializeField] private bool singlePlayer = true; // 单人自测：有牌即视为你最后出

    // Tube/Box 先占位为“你持有”
    private bool tubeHeldByYou = true;
    private bool boxHeldByYou  = true;

    // 状态
    private int  coins = 0;
    private bool lastPlayedByYou = false; // 本局最后一次合法出牌是否你出的
    private bool isYourTurn = true;       // 自测版：始终轮到你
    private bool youPassed = false;       // 本局是否已选择 Pass

    void Start()
    {
        UpdateMarkers();
        UpdateCoinsUI();

        endTurnBtn.onClick.AddListener(EndTurn);
        endRoundBtn.onClick.AddListener(EndRound);
        passBtn.onClick.AddListener(OnPass);

        RefreshTurnButtons();
        StartTurn();
    }

    // 回合开始：自动摸1，若手牌空再补1；清除本回合的“已过牌”
    void StartTurn()
    {
        youPassed = false;
        hand.DrawOneToHand();
        if (hand.Count == 0) hand.DrawOneToHand();
        RefreshTurnButtons();
    }

    // （自测）结束回合：切换 Tube 显示并立刻进入下一回合
    void EndTurn()
    {
        tubeHeldByYou = !tubeHeldByYou;
        UpdateMarkers();
        StartTurn();
    }

    void UpdateMarkers()
    {
        tubeText.text = "Tube: " + (tubeHeldByYou ? "You" : "Other");
        boxText.text  = "Box: "  + (boxHeldByYou  ? "You" : "Other");
    }

    void UpdateCoinsUI() => coinsText.text = $"Coins: {coins}/10";

    public void NotifyYouPlayed()   // 由 HandManager 在合法出牌后调用
    {
        lastPlayedByYou = true;
    }

    // 自测版兜底：允许 HandManager 判断当前是否能“打空后自动摸1”
    public bool CanAutoDrawNow() => isYourTurn;

    void EndRound()
    {
        if (table == null || table.top == null)
        {
            Debug.Log("EndRound: 台面为空，无法结算");
            return;
        }

        // 兜底：单人自测时，只要台面有牌就算是你最后出的
        bool youAreLast = lastPlayedByYou || (singlePlayer && table.top != null);
        if (youAreLast)
        {
            int gain = table.top.data.value; // 黑=4，白=5，T2=2，T1=1
            coins += gain;
            UpdateCoinsUI();
            Debug.Log($"本局你得分 +{gain}，当前 {coins}/10");
        }
        else
        {
            Debug.Log("本局最后一张不是你打的");
        }

        // 清台：把顶牌丢到弃牌堆并清空
        deck.Discard(table.top);
        table.ClearTable();
        lastPlayedByYou = false;

        // 最小 Tube 轮转占位
        tubeHeldByYou = !tubeHeldByYou;
        UpdateMarkers();

        // 下一回合
        StartTurn();

        // 胜利检测
        if (coins >= 10)
        {
            Debug.Log("🎉 你达到 10 币，赢了！（自测版）");
            // TODO: 弹出结算面板/重开
        }
    }

    // —— 行为按钮 —— //
    void OnPass()
    {
        if (!isYourTurn || youPassed) return;
        youPassed = true;
        Debug.Log("你选择了 Pass（本局不再出牌）");
        RefreshTurnButtons();
    }

    void RefreshTurnButtons()
    {
        passBtn.interactable = isYourTurn && !youPassed;
        // 没有 Draw 按钮；EndTurn/EndRound 保持可用（自测）
    }

}
