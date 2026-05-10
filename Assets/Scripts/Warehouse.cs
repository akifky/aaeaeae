using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Warehouse : MonoBehaviour
{
    private Factory _factory;
    private bool _completed = false;

    void Start()
    {
        
    }

    void Update()
    {
        if (_completed) return;

        _factory = GetComponent<Factory>();

        foreach (var d in _factory.demands)
        {
            int have = _factory.DemandStock.ContainsKey(d.item) ? _factory.DemandStock[d.item] : 0;
            if (have < d.amount) return;
        }

        _completed = true;
        StartCoroutine(CompleteDay());
    }

    IEnumerator CompleteDay()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var images = GetComponentsInChildren<Image>();
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            foreach (var r in renderers)
                r.color = new Color(r.color.r, r.color.g, r.color.b, alpha);
            foreach (var img in images)
                img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            yield return null;
        }

        FindFirstObjectByType<DayManager>()?.NextDay();
    }
}