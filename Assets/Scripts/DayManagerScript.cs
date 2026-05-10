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
        Factory[] allFactories = FindObjectsOfType<Factory>(true);
        foreach (var f in allFactories)
            f.gameObject.SetActive(System.Array.IndexOf(days[index].unlockedFactoryIDs, f.factoryID) >= 0);

        if (newspaperUI != null && days[index].newspaper != null)
            newspaperUI.Show(days[index].newspaper);
    }
}