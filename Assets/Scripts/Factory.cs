using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DemandEntry
{
    public ItemType item;
    public int amount = 3;
}

public class Factory : MonoBehaviour
{
    [Header("Kimlik")]
    public string factoryName = "Fabrika";

    [Header("Üretim")]
    public ItemType production;
    public float productionInterval = 3f;
    public int maxStock = 10;

    [Header("Talep")]
    public DemandEntry[] demands;
    public Dictionary<ItemType, int> DemandStock { get; private set; }
        = new Dictionary<ItemType, int>();

    [Header("UI")]
    public Transform InputPanel;
    public SpriteRenderer OutputSprite;
    public GameObject IconPrefab;

    // Stok: üretilen kaynak
    public int Stock { get; private set; } = 0;

    private float _timer = 0f;

    private Dictionary<ItemType, List<Image>> _demandIcons = new Dictionary<ItemType, List<Image>>();

    void Start()
    {
        foreach (var d in demands)
            if (!DemandStock.ContainsKey(d.item))
                DemandStock[d.item] = 0;

        _timer = Random.Range(0f, productionInterval);

        // Output Image
        if (OutputSprite != null)
        {
            OutputSprite.sprite = production.icon;
        }

        // Input Images
        if (IconPrefab != null)
        {
            foreach (var d in demands)
            {
                _demandIcons[d.item] = new List<Image>();
                for (int i = 0; i < d.amount; i++)
                {
                    GameObject newIcon = Instantiate(IconPrefab, InputPanel);
                    Image img = newIcon.GetComponent<Image>();
                    img.sprite = d.item.icon;
                    img.color = new Color(1f, 1f, 1f, 0.2f); // başta hepsi saydam
                    _demandIcons[d.item].Add(img);
                }
            }
        }
    }

    void Update()
    {
        foreach (var d in demands)
        {
            int have = DemandStock.ContainsKey(d.item) ? DemandStock[d.item] : 0;
            if (have < d.amount) return;
        }

        _timer += Time.deltaTime;
        if (_timer >= productionInterval)
        {
            _timer = 0f;
            if (Stock == 0)
            {
                Stock++;
                foreach (var d in demands)
                {
                    DemandStock[d.item] = 0;
                    UpdateDemandIcons(d.item);
                }
            }
        }
    }

    // Vagon bu fabrikadan kaynak alır
    public bool TakeResource(ItemType type, out int amount)
    {
        if (type == production && Stock > 0)
        {
            amount = 1;
            Stock--;
            return true;
        }
        amount = 0;
        return false;
    }

    // Vagon bu fabrikaya kaynak bırakır
    public void ReceiveResource(ItemType type, int amount)
    {
        if (DemandStock.ContainsKey(type))
        {
            var demand = System.Array.Find(demands, d => d.item == type);
            DemandStock[type] = Mathf.Min(DemandStock[type] + amount, demand.amount);
            UpdateDemandIcons(type);
        }
    }

    public bool NeedsResource(ItemType type)
    {
        if (!DemandStock.ContainsKey(type)) return false;
        var demand = System.Array.Find(demands, d => d.item == type);
        return DemandStock[type] < demand.amount;
    }

    void UpdateDemandIcons(ItemType type)
    {
        if (!_demandIcons.ContainsKey(type)) return;

        int have = DemandStock[type];
        var icons = _demandIcons[type];

        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].color = i < have
                ? new Color(1f, 1f, 1f, 1f)   // dolu: tam opak
                : new Color(1f, 1f, 1f, 0.2f); // boş: saydam
        }
    }
}