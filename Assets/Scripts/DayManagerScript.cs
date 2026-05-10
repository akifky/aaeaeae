using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DayManager : MonoBehaviour
{
    public DayData[] days;
    public NewspaperUI newspaperUI;
    private static int _currentDay = 0;

    void Start()
{
    Factory[] allFactories = FindObjectsOfType<Factory>();
    foreach (var f in allFactories)
        f.gameObject.SetActive(false);

    foreach (var f in days[_currentDay].unlockedFactories)
        f.gameObject.SetActive(true);

    if (newspaperUI != null && days[_currentDay].newspaper != null)
        newspaperUI.Show(days[_currentDay].newspaper);
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

        // Tüm factory'leri kapat
        Factory[] allFactories = FindObjectsOfType<Factory>();
        foreach (var f in allFactories)
            f.gameObject.SetActive(false);

        // Sadece o güne ait olanlarý aç
        foreach (var f in days[_currentDay].unlockedFactories)
            f.gameObject.SetActive(true);

        // Gazete göster
        if (newspaperUI != null && days[_currentDay].newspaper != null)
            newspaperUI.Show(days[_currentDay].newspaper);
    }

    IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}