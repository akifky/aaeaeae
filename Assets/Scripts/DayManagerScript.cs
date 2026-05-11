using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DayManager : MonoBehaviour
{
    public DayData[] days;
    public NewspaperUI newspaperUI;
    public int _currentDay = 0;

    void Start()
    {
        ApplyDay(_currentDay);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
            NextDay();
    }

    public void NextDay()
    {
        _currentDay++;
        if (_currentDay >= days.Length) return;
        ApplyDay(_currentDay);
    }

    void ApplyDay(int index)
    {
        // Warehouse'larý sýfýrla
        foreach (var w in FindObjectsOfType<Warehouse>(true))
            w.Reset();

        // Sadece ilk günde hepsini kapat
        if (index == 0)
            foreach (var f in FindObjectsOfType<Factory>(true))
                f.gameObject.SetActive(false);

        // O güne ait olanlarý aç
        foreach (var f in FindObjectsOfType<Factory>(true))
            if (System.Array.IndexOf(days[index].unlockedFactoryIDs, f.factoryID) >= 0)
                f.gameObject.SetActive(true);

        // Gazete göster
        if (newspaperUI != null && days[index].newspaper != null)
            newspaperUI.Show(days[index].newspaper);

        // Fade in
        FadeManager.Instance?.FadeIn(1f);
    }
}