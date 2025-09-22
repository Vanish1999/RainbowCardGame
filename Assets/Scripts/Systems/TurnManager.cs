using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    [Header("Refs")]
    public HandManager hand;      // 玩家
    public AIHand ai;             // AI
    public TableManager table;    // 台面
    public DeckManager deck;      // 丢弃等（本版本仅在需要时使用）

    [Header("HUD")]
    public TMP_Text coinsText;    // 玩家分
    public Button   endTurnBtn;   // “我这回合出不了了/不想出了”

    // —— 状态 —— //
    private bool isPlayerTurn = true;  // 当前行动者
    private bool playerActive = true;  // 本 turn 是否还在场（未退出）
    private bool aiActive     = true;
    private int  coins        = 0;     // 先只记玩家分
    private bool firstTurn    = true;  // 首局额外发4

    void Start()
    {
        endTurnBtn.onClick.RemoveAllListeners();
        endTurnBtn.onClick.AddListener(OnPlayerEndTurnClicked);

        UpdateCoinsUI();

        // 开第一局：默认玩家先手（你可根据 Tube/Box 决定传 true/false）
        StartNewTurn(starterIsPlayer: true);
    }

    // ========= Turn 流程 =========

    // 开一个“新 turn”：清台 →（首局各发4）→ 各摸1 → 进入首手
    public void StartNewTurn(bool starterIsPlayer)
    {
        table.ClearTable();
        playerActive = aiActive = true;
        isPlayerTurn = starterIsPlayer;

        if (firstTurn)
        {
            hand.DrawToHand(4);
            ai.DrawToHand(4);     // AIHand 需有 DrawToHand(n)
            ai.DrawStartOfTurn();
            UpdateAIInfoUI();
            firstTurn = false;
        }

        // 每个turn开始都各摸1（包含第一局）
        hand.DrawOneToHand();
        ai.DrawStartOfTurn();     // 或 ai.DrawOne();

        NextActor();
    }

    // 玩家点“End Turn”= 退出本 turn
    private void OnPlayerEndTurnClicked()
    {
        if (!isPlayerTurn || !playerActive) return;
        playerActive = false;
        CheckTurnWinnerOrContinue();
    }

    // 由 HandManager 在玩家“出牌成功”后调用（旧名兼容）
    public void NotifyYouPlayed()      => NotifyPlayerPlayed();
    public void NotifyPlayerPlayed()
    {
        isPlayerTurn = false;     // 切给 AI
        NextActor();
    }

    // 轮到当前行动者；玩家手动出，AI 自动出或退出
    private void NextActor()
    {
        if (isPlayerTurn && !playerActive) isPlayerTurn = false;
        if (!isPlayerTurn && !aiActive)    isPlayerTurn = true;

        bool playerTurnNow = isPlayerTurn && playerActive;
        endTurnBtn.interactable = playerTurnNow;  // AI 行动时禁用按钮

        if (playerTurnNow)
        {
            // 玩家现在可以出牌（HandManager 按钮），
            // 出牌成功后会回调 NotifyPlayerPlayed() 自动切到 AI
            Debug.Log("== Player plays ==");
        }
        else
        {
            if (!aiActive && !playerActive) { Debug.LogError("两边都退出了？"); return; }
            if (aiActive) StartCoroutine(AITakeAction());
        }
    }

    private IEnumerator AITakeAction()
    {
        if (ai == null || ai.deck == null || ai.table == null)
        { Debug.LogError("AI 或依赖未赋值"); yield break; }

        // 思考时间（需要“立刻出”就设为 0）
        yield return new WaitForSeconds(0.6f);

        var play = ai.DecidePlay();
        UpdateAIInfoUI();
        if (play != null)
        {
            table.PlaceOnTable(play, byPlayer: false);
            Debug.Log($"AI 出牌：{play.data.displayName}");

            // 出完切回玩家
            isPlayerTurn = true;
            NextActor();
        }
        else
        {
            // AI 退出本 turn → 若只剩玩家活着则玩家立刻赢
            Debug.Log("AI Pass（退出本 turn）");
            aiActive = false;
            CheckTurnWinnerOrContinue();
        }
    }

    // 判定是否只剩一人“活着”；若是→该人赢得本 turn，按顶牌价值加分→开启下一 turn（输家先手）
    private void CheckTurnWinnerOrContinue()
    {
        if (playerActive ^ aiActive)   // 恰好一方为真
        {
            bool winnerIsPlayer = playerActive;   // 还活着的就是胜者
            int gain = (table.top != null) ? table.top.data.value : 0;

            if (gain > 0)
            {
                if (winnerIsPlayer) { coins += gain; UpdateCoinsUI(); }
                else { /* TODO: AI 的分，有需要再加 */ }
                Debug.Log($"Turn Winner = {(winnerIsPlayer ? "Player" : "AI")}，+{gain}");
            }

            // （可选）把顶牌丢弃后清台
            if (table.top != null) deck.Discard(table.top);
            table.ClearTable();

            // 下一 turn 由胜者的右手边先手——两人=输家先手
            bool nextStarterIsPlayer = !winnerIsPlayer;
            StartNewTurn(nextStarterIsPlayer);
            return;
        }

        // 否则：本 turn 继续，轮到仍在场的那一方
        isPlayerTurn = playerActive ? false : true;   // 玩家刚退出→到AI；AI刚退出→到玩家
        NextActor();
    }

    // ========= 工具 =========
    private void UpdateCoinsUI() => coinsText.text = $"Coins: {coins}/10";

    // 给 HandManager 用：只有在玩家回合仍“活着”时才允许自动补摸
    public bool CanAutoDrawNow() => isPlayerTurn && playerActive;
    // HUD 引用
    public TMP_Text aiText;   // 拖 HUD/AIText

    void UpdateAIInfoUI() => aiText.text = $"AI: {ai?.Count ?? 0} cards";

}
