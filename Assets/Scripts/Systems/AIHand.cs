using System.Collections.Generic;
using UnityEngine;

public class AIHand : MonoBehaviour
{
    public DeckManager deck;      // 拖 DeckManager
    public TableManager table;    // 拖 TableManager

    private readonly List<CardInstance> hand = new();

    public int Count => hand.Count;

    // —— 摸牌 —— //
    public void DrawToHand(int n)            // <== 新增：批量摸牌（首局发4张用）
    {
        for (int i = 0; i < n; i++) DrawOne();
    }

    public void DrawStartOfTurn()            // 每个 turn 开始各摸1（已有）
    {
        DrawOne();
        if (hand.Count == 0) DrawOne();
    }

    public void DrawOne()
    {
        var c = deck.DrawOne();
        if (c != null) hand.Add(c);
    }

    // —— 决策：返回要出的牌（null=本回合退出/Pass） —— //
    public CardInstance DecidePlay()
    {
        var prev = table.top?.data;

        // 1) 直接找一张能合法出的牌（优先级：白>黑>二级色>一级色）
        CardInstance PickPlayable(params CardTier[] pref)
        {
            foreach (var t in pref)
            {
                var found = hand.Find(ci => ci.data.tier == t && RuleValidator.CanPlayAfter(prev, ci.data));
                if (found != null) return found;
            }
            return null;
        }

        // 2) 简单合成：prev==T1 → 需要T2，尝试由两张T1合出可用T2
        if (prev != null && prev.tier == CardTier.T1)
        {
            CardInstance t1a = null, t1b = null;
            foreach (var a in hand)
            {
                if (a.data.tier != CardTier.T1) continue;
                foreach (var b in hand)
                {
                    if (ReferenceEquals(a, b) || b.data.tier != CardTier.T1) continue;
                    var ca = a.data.color; var cb = b.data.color;
                    bool rb = (ca==CardColor.Red && cb==CardColor.Blue) || (ca==CardColor.Blue && cb==CardColor.Red);
                    bool ry = (ca==CardColor.Red && cb==CardColor.Yellow) || (ca==CardColor.Yellow && cb==CardColor.Red);
                    bool by = (ca==CardColor.Blue&&cb==CardColor.Yellow) || (ca==CardColor.Yellow&&cb==CardColor.Blue);

                    if (rb && RuleValidator.CanPlayAfter(prev, new CardData{ tier=CardTier.T2, color=CardColor.Purple })) { t1a=a; t1b=b; break; }
                    if (ry && RuleValidator.CanPlayAfter(prev, new CardData{ tier=CardTier.T2, color=CardColor.Orange })) { t1a=a; t1b=b; break; }
                    if (by && RuleValidator.CanPlayAfter(prev, new CardData{ tier=CardTier.T2, color=CardColor.Green  })) { t1a=a; t1b=b; break; }
                }
                if (t1a != null) break;
            }
            if (t1a != null)
            {
                hand.Remove(t1a); hand.Remove(t1b);
                // 推断目标二级色
                CardColor target = CardColor.Purple;
                var ca = t1a.data.color; var cb = t1b.data.color;
                if ((ca==CardColor.Red && cb==CardColor.Yellow) || (ca==CardColor.Yellow&&cb==CardColor.Red)) target = CardColor.Orange;
                if ((ca==CardColor.Blue&&cb==CardColor.Yellow) || (ca==CardColor.Yellow&&cb==CardColor.Blue)) target = CardColor.Green;

                var fake = new CardData { tier=CardTier.T2, color=target, displayName=target.ToString(), value=2 };
                return new CardInstance(fake);
            }
        }

        // 3) prev==T2 → 需要黑：两张不同T2合黑
        if (prev != null && prev.tier == CardTier.T2)
        {
            CardInstance a = null, b = null;
            foreach (var x in hand) foreach (var y in hand)
            {
                if (ReferenceEquals(x,y)) continue;
                bool xt2 = x.data.tier==CardTier.T2;
                bool yt2 = y.data.tier==CardTier.T2;
                bool diff = x.data.color!=y.data.color;
                if (xt2 && yt2 && diff) { a=x; b=y; break; }
            }
            if (a != null)
            {
                hand.Remove(a); hand.Remove(b);
                var fake = new CardData { tier=CardTier.T3_Black, color=CardColor.Black, displayName="Black", value=4 };
                return new CardInstance(fake);
            }
        }

        // 4) 直接出可出的（白>黑>T2>T1；空台时随意）
        var pick = PickPlayable(CardTier.T4_White, CardTier.T3_Black, CardTier.T2, CardTier.T1);
        if (pick != null)
        {
            hand.Remove(pick);
            return pick;
        }

        // 5) 出不了就退出（Pass）
        return null;
    }
}
