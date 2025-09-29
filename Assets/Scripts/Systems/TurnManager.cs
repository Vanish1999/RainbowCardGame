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
    public DeckManager deck;      // 弃牌（可选）

    [Header("HUD")]
    public TMP_Text coinsText;    // 玩家 Coins 文本
    public TMP_Text aiInfoText;   // AI 信息文本（Coins | Cards）
    public TMP_Text resultText;   // 大字结果（你赢了/你输了）
    public Button   endTurnBtn;   // 我退出本 turn 按钮

    // —— 状态 —— //
    private bool isPlayerTurn = true;
    private bool playerActive = true;
    private bool aiActive     = true;
    private bool firstTurn    = true;
    private bool gameOver     = false;

    private int  coins    = 0;    // 玩家金币
    private int  aiCoins  = 0;    // AI 金币

    void Start()
    {
        endTurnBtn.onClick.RemoveAllListeners();
        endTurnBtn.onClick.AddListener(OnPlayerEndTurnClicked);

        if (resultText != null) resultText.gameObject.SetActive(false);

        UpdateCoinsUI();
        UpdateAIUI();

        // 开第一局：默认玩家先手
        StartNewTurn(starterIsPlayer: true);
    }

    // ========= Turn 流程 =========

    // 开一个“新 turn”：清台 →（首局各发4）→ 各摸1 → 进入首手
    public void StartNewTurn(bool starterIsPlayer)
    {
        if (gameOver) return;

        table.ClearTable();
        playerActive = aiActive = true;
        isPlayerTurn = starterIsPlayer;

        if (firstTurn)
        {
            hand.DrawToHand(4);
            ai.DrawToHand(4);
            firstTurn = false;
        }

        // 每个 turn 开始都各摸 1（包含首局）
        hand.DrawOneToHand();
        ai.DrawStartOfTurn();

        UpdateAIUI();
        NextActor();
    }

    // 玩家点“End Turn”= 我退出本 turn
    private void OnPlayerEndTurnClicked()
    {
        if (gameOver) return;
        if (!isPlayerTurn || !playerActive) return;
        playerActive = false;
        CheckTurnWinnerOrContinue();
    }

    // 由 HandManager 在“玩家成功出牌”后调用（兼容旧名）
    public void NotifyYouPlayed()      => NotifyPlayerPlayed();
    public void NotifyPlayerPlayed()
    {
        if (gameOver) return;
        isPlayerTurn = false;     // 切给 AI
        NextActor();
    }

    // 轮到当前行动者；玩家手动出，AI 自动出或退出
    private void NextActor()
    {
        if (gameOver) return;

        if (isPlayerTurn && !playerActive) isPlayerTurn = false;
        if (!isPlayerTurn && !aiActive)    isPlayerTurn = true;

        bool playerTurnNow = isPlayerTurn && playerActive;
        endTurnBtn.interactable = playerTurnNow;

        if (playerTurnNow)
        {
            Debug.Log("== Player plays ==");
            // 玩家通过 HandManager 点击手牌出牌；
            // 出牌成功后会回调 NotifyPlayerPlayed() 自动切到 AI
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

        yield return new WaitForSeconds(0.6f); // 思考时间

        var play = ai.DecidePlay();
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
            // AI 退出本 turn
            Debug.Log("AI Pass（退出本 turn）");
            aiActive = false;
            CheckTurnWinnerOrContinue();
        }
    }

    // 判定是否只剩一人“活着”；若是→该人赢得本 turn，按顶牌价值加分→开下一 turn（输家先手）
    private void CheckTurnWinnerOrContinue()
    {
        if (gameOver) return;

        if (playerActive ^ aiActive)   // 恰好一方为真
        {
            bool winnerIsPlayer = playerActive;   // 还活着的就是胜者
            int gain = (table.top != null) ? table.top.data.value : 0;

            if (gain > 0)
            {
                if (winnerIsPlayer) { coins += gain; UpdateCoinsUI(); }
                else                { aiCoins += gain; UpdateAIUI(); }
                Debug.Log($"Turn Winner = {(winnerIsPlayer ? "Player" : "AI")}，+{gain}");
            }

            // 丢弃顶牌并清台（可选）
            if (table.top != null) deck.Discard(table.top);
            table.ClearTable();

            // 胜负检查
            if (CheckWin()) return;

            // 下一 turn：胜者的右手边先手（两人=输家先手）
            bool nextStarterIsPlayer = !winnerIsPlayer;
            StartNewTurn(nextStarterIsPlayer);
            return;
        }

        // 否则：本 turn 继续，轮到仍在场的那一方
        isPlayerTurn = playerActive ? false : true;   // 玩家刚退出→到AI；AI刚退出→到玩家
        NextActor();
    }

    // ========= UI & 胜负 =========
    private void UpdateCoinsUI() => coinsText.text = $"Coins: {coins}/10";

    private void UpdateAIUI()
    {
        if (aiInfoText != null)
        {
            int cards = ai != null ? ai.Count : 0;
            aiInfoText.text = $"AI: {aiCoins}/10 | {cards} cards";
        }
    }

    // 返回是否已经出结果
    private bool CheckWin()
    {
        if (coins >= 10) { ShowResult("You Win！"); return true; }
        if (aiCoins >= 10) { ShowResult("You Lose！"); return true; }
        return false;
    }

    private void ShowResult(string msg)
    {
        gameOver = true;
        endTurnBtn.interactable = false;

        if (resultText != null)
        {
            resultText.text = msg;
            resultText.gameObject.SetActive(true);
        }
        Debug.Log(msg);
        // TODO: 这里可以启用“再来一局”按钮或回主菜单
    }

    // HandManager 自动补摸时的判定
    public bool CanAutoDrawNow() => !gameOver && isPlayerTurn && playerActive;
    
}
