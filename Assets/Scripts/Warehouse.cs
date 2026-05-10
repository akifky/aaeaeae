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
        if(_factory != null)
        {
            foreach (var d in _factory.demands)
            {
                int have = _factory.DemandStock.ContainsKey(d.item) ? _factory.DemandStock[d.item] : 0;
                if (have < d.amount) return;
            }
        }

        _completed = true;
        StartCoroutine(CompleteDay());
    }

    IEnumerator CompleteDay()
    {
        yield return new WaitForSeconds(0.5f);
        FindFirstObjectByType<DayManager>()?.NextDay();
    }
}