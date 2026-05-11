using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Warehouse : MonoBehaviour
{
    private Factory _factory;
    private bool _completed = false;

    void Start() { }

    void Update()
    {
        if (_completed) return;

        _factory = GetComponentInChildren<Factory>();
        if (_factory == null) { Debug.Log("[Warehouse] Factory bulunamadý"); return; }
        if (_factory.demands == null || _factory.demands.Length == 0) { Debug.Log("[Warehouse] Demand yok"); return; }

        foreach (var d in _factory.demands)
        {
            int have = _factory.DemandStock.ContainsKey(d.item) ? _factory.DemandStock[d.item] : 0;
            Debug.Log($"[Warehouse] {d.item.name}: {have}/{d.amount}");
            if (have < d.amount) return;
        }

        Debug.Log("[Warehouse] Tüm demandlar karþýlandý, CompleteDay baþlýyor");
        _completed = true;
        StartCoroutine(CompleteDay());
    }

    public void Reset()
    {
        Debug.Log("[Warehouse] Reset çaðrýldý");
        _completed = false;
    }

    IEnumerator CompleteDay()
    {
        Debug.Log("[Warehouse] CompleteDay coroutine baþladý");
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[Warehouse] NextDay çaðrýlýyor");
        FindFirstObjectByType<DayManager>()?.NextDay();
    }
}