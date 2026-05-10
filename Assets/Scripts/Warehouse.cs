using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Warehouse : MonoBehaviour
{
    [Header("Günlük Hedef")]
    public DemandEntry[] demands;

    public Dictionary<ItemType, int> Stock { get; private set; }
        = new Dictionary<ItemType, int>();

    [Header("UI")]
    public Transform inputPanel;
    public GameObject iconPrefab;

    private Dictionary<ItemType, List<Image>> _icons
        = new Dictionary<ItemType, List<Image>>();

    void Start()
    {
        foreach (var d in demands)
            Stock[d.item] = 0;

        if (iconPrefab != null)
        {
            foreach (var d in demands)
            {
                _icons[d.item] = new List<Image>();
                for (int i = 0; i < d.amount; i++)
                {
                    var go = Instantiate(iconPrefab, inputPanel);
                    var img = go.GetComponent<Image>();
                    img.sprite = d.item.icon;
                    img.color = new Color(1f, 1f, 1f, 0.2f);
                    _icons[d.item].Add(img);
                }
            }
        }
    }

    public bool NeedsResource(ItemType type)
    {
        if (!Stock.ContainsKey(type)) return false;
        var demand = System.Array.Find(demands, d => d.item == type);
        return Stock[type] < demand.amount;
    }

    public void ReceiveResource(ItemType type, int amount)
    {
        if (!Stock.ContainsKey(type)) return;
        var demand = System.Array.Find(demands, d => d.item == type);
        Stock[type] = Mathf.Min(Stock[type] + amount, demand.amount);
        UpdateIcons(type);
        CheckCompletion();
    }

    void UpdateIcons(ItemType type)
    {
        if (!_icons.ContainsKey(type)) return;
        int have = Stock[type];
        for (int i = 0; i < _icons[type].Count; i++)
            _icons[type][i].color = i < have
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.2f);
    }

    void CheckCompletion()
    {
        foreach (var d in demands)
        {
            if (Stock[d.item] < d.amount) return;
        }

        // Tüm hedefler tamamlandý
        StartCoroutine(CompleteDay());
    }

    System.Collections.IEnumerator CompleteDay()
    {
        // Fade out
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var images = GetComponentsInChildren<Image>();
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            foreach (var r in renderers) r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
            foreach (var img in images) img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            yield return null;
        }

        FindObjectOfType<DayManager>()?.NextDay();
    }
}