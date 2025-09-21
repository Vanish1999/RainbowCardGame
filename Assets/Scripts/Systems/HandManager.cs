using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HandManager : MonoBehaviour
{
    [Header("Refs")]
    public DeckManager deck;               // 把 Systems 上的 DeckManager 拖进来
    public RectTransform handPanel;        // 拖 Canvas 下的 HandPanel
    public GameObject cardButtonPrefab;    // 拖刚做好的 CardButton.prefab

    [Header("Rules")]
    public int startHandSize = 4;

    private readonly List<CardInstance> hand = new();

    void Start()
    {
        // 起手摸4
        DrawToHand(startHandSize);
        RefreshUI();
    }

    public void DrawToHand(int n)
    {
        for (int i = 0; i < n; i++)
        {
            var c = deck.DrawOne();
            if (c != null) hand.Add(c);
        }
    }

    void RefreshUI()
    {
        // 清空旧的
        for (int i = handPanel.childCount - 1; i >= 0; i--)
            Destroy(handPanel.GetChild(i).gameObject);

        // 生成按钮
        foreach (var c in hand)
        {
            var go = Instantiate(cardButtonPrefab, handPanel);
            var txt = go.GetComponentInChildren<TMP_Text>();
            txt.text = $"{c.data.displayName} [{c.data.value}]";

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (combineToggle != null && combineToggle.isOn)
                {
                    OnCardClickedForCombine(c);
                }
                else
                {
                    // 正常出牌（规则校验）
                    if (table == null) { Debug.LogError("TableManager 未赋值"); return; }
                    var prev = table.top?.data;
                    if (RuleValidator.CanPlayAfter(prev, c.data))
                    {
                        table.top = c;
                        hand.Remove(c);
                        RefreshUI();
                        Debug.Log($"Play: {c.data.displayName}");
                        turn?.NotifyYouPlayed();
                        // 打出后若手牌空，且现在是你的回合 → 自动摸1

                        bool allowAutoDraw = (turn == null) || turn.CanAutoDrawNow();
                        if (hand.Count == 0 && allowAutoDraw)
                        {
                            DrawOneToHand();
                        }
                        
                    }
                    else
                    {
                        Debug.Log($"非法出牌：{c.data.displayName}");
                    }
                }
            });


        }
    }
    public int Count => hand.Count;
    public void DrawOneToHand()
    {
        var c = deck.DrawOne();
        if (c != null) { hand.Add(c); RefreshUI(); }
    }
    public TableManager table;  // 在 Inspector 里把 Systems 上的 TableManager 拖进来

    public Toggle combineToggle;   // 拖 HUD/CombineToggle
    private CardInstance pending;  // 第一次点选用于合成的牌

    void OnCardClickedForCombine(CardInstance clicked)
    {
        // 第一次点：记录候选
        if (pending == null)
        {
            pending = clicked;
            Debug.Log($"Combine: 选中 {pending.data.displayName}，再点一张同阶牌尝试合成");
            return;
        }

        // 再次点同一张 = 取消
        if (pending == clicked)
        {
            pending = null;
            Debug.Log("Combine: 取消选中");
            return;
        }

        // 第二张：尝试合成
        var result = TryCombine(pending, clicked);
        if (result != null)
        {
            hand.Remove(pending);
            hand.Remove(clicked);
            hand.Add(result);
            pending = null;
            RefreshUI();
            Debug.Log($"Combine 成功：→ {result.data.displayName}");
        }
        else
        {
            pending = null;
            Debug.Log("Combine 失败：需同阶且符合配色/规则");
        }
    }

    CardInstance TryCombine(CardInstance a, CardInstance b)
    {
        // 必须同阶
        if (a.data.tier != b.data.tier) return null;

        // 2×T1 → T2（按颜色配对）
        if (a.data.tier == CardTier.T1)
        {
            var c1 = a.data.color; var c2 = b.data.color;

            bool rb = (c1 == CardColor.Red   && c2 == CardColor.Blue ) || (c1 == CardColor.Blue  && c2 == CardColor.Red);
            bool ry = (c1 == CardColor.Red   && c2 == CardColor.Yellow) || (c1 == CardColor.Yellow&& c2 == CardColor.Red);
            bool by = (c1 == CardColor.Blue  && c2 == CardColor.Yellow) || (c1 == CardColor.Yellow&& c2 == CardColor.Blue);

            if (rb) return new CardInstance(deck.t2Purple); // 红+蓝=紫
            if (ry) return new CardInstance(deck.t2Orange); // 红+黄=橙
            if (by) return new CardInstance(deck.t2Green ); // 蓝+黄=绿
            return null; // 其他组合不合法（例如 红+红）
        }

        // 2×T2 → 黑：必须是两张不同的 T2（橙/紫/绿中任意两种不同）
        if (a.data.tier == CardTier.T2 && b.data.tier == CardTier.T2)
        {
            // 只接受二级色三种：Orange / Purple / Green
            bool aIsT2Color = a.data.color == CardColor.Orange || a.data.color == CardColor.Purple || a.data.color == CardColor.Green;
            bool bIsT2Color = b.data.color == CardColor.Orange || b.data.color == CardColor.Purple || b.data.color == CardColor.Green;

            if (aIsT2Color && bIsT2Color && a.data.color != b.data.color)
                return new CardInstance(deck.t3Black);   // 不同二级色 → 黑
            else
                return null; // 相同颜色或不是二级色 → 不能合成
        }


        // 其他阶不支持合成（例如 黑/白）
        return null;
    }
    public TurnManager turn; // 把 TurnManager 拖进来

}