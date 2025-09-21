using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TableManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform pileRoot;         // 拖 Canvas/TablePile
    public GameObject tableCardPrefab;     // 拖 CardButton.prefab
    public int maxShown = 6;               // 最多显示几张叠在台面
    public float stepOffset = 10f;         // 每张相对位移
    public float stepRotate = 4f;          // 每张相对旋转

    [Header("Runtime")]
    public CardInstance top;               // 当前台面顶牌
    private readonly List<CardInstance> stack = new();
    private readonly List<GameObject> visuals = new();

    public void PlaceOnTable(CardInstance card, bool byPlayer)
    {
        if (card == null) return;
        top = card;
        stack.Add(card);

        // ---- 实例一个小卡片到中央堆叠区 ----
        var go = Instantiate(tableCardPrefab, pileRoot);
        visuals.Add(go);

        // 文本
        var txt = go.GetComponentInChildren<TMP_Text>();
        if (txt) txt.text = $"{card.data.displayName} [{card.data.value}]";

        // 关交互，只做展示
        var btn = go.GetComponent<Button>();
        if (btn) btn.interactable = false;

        // 轻微位移+旋转，形成堆叠
        int i = stack.Count - 1;
        var rt = go.transform as RectTransform;
        if (rt)
        {
            rt.anchoredPosition = new Vector2((i % maxShown) * stepOffset * 0.6f,
                -(i % maxShown) * stepOffset);
            rt.localRotation = Quaternion.Euler(0, 0, (i % maxShown - 2) * stepRotate);
        }

        // 控制最多显示张数（老的从UI移除，但数据保留）
        if (visuals.Count > maxShown)
        {
            Destroy(visuals[0]);
            visuals.RemoveAt(0);
        }
    }

    public void ClearTable()
    {
        top = null;
        stack.Clear();
        for (int i = 0; i < visuals.Count; i++) Destroy(visuals[i]);
        visuals.Clear();
    }
}