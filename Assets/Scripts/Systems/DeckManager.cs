using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable] public class CardInstance {
    public string uid;         // 唯一ID（便于日志/网络）
    public CardData data;      // 指向模板（颜色/等级/价值）
    public CardInstance(CardData d) { data = d; uid = Guid.NewGuid().ToString("N"); }
}

public class DeckManager : MonoBehaviour
{
    [Header("Configs & Templates")]
    public GameConfig config;     // 拖进来
    public CardData t1Red, t1Blue, t1Yellow;
    public CardData t2Purple, t2Orange, t2Green;
    public CardData t3Black, t4White;

    [Header("Runtime Piles")]
    public List<CardInstance> deck = new();     // 牌库（面朝下）
    public List<CardInstance> discard = new();  // 弃牌堆（面朝上）

    void Awake() { BuildFreshDeck(); Shuffle(deck); }

    public void BuildFreshDeck()
    {
        deck.Clear(); discard.Clear();

        // 按 GameConfig 数量，用模板批量生成 50 张
        AddCopies(t4White, config.countWhite);
        AddCopies(t3Black, config.countBlack);
        AddCopies(t1Red,   config.countRed);
        AddCopies(t1Blue,  config.countBlue);
        AddCopies(t1Yellow,config.countYellow);
        AddCopies(t2Purple,config.countPurple);
        AddCopies(t2Orange,config.countOrange);
        AddCopies(t2Green, config.countGreen);
    }

    void AddCopies(CardData template, int n)
    {
        for (int i = 0; i < n; i++) deck.Add(new CardInstance(template));
    }

    public void Shuffle(List<CardInstance> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 抽一张：若库空→把弃牌洗回库再抽
    public CardInstance DrawOne()
    {
        if (deck.Count == 0)
        {
            if (discard.Count == 0) return null; // 没牌了
            deck.AddRange(discard);
            discard.Clear();
            Shuffle(deck);
        }
        var top = deck[^1];
        deck.RemoveAt(deck.Count - 1);
        return top;
    }

    public void Discard(CardInstance c) { if (c != null) discard.Add(c); }

    // 简易测试（右键菜单）
    [ContextMenu("TEST: Draw 5")]
    void TestDraw5()
    {
        for (int i = 0; i < 5; i++)
        {
            var c = DrawOne();
            Debug.Log($"Draw: {c?.data.displayName} ({c?.data.tier}/{c?.data.color})");
        }
        Debug.Log($"Deck={deck.Count}, Discard={discard.Count}");
    }
}

