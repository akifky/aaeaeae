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
        if (newspaperUI != null && days[_currentDay].newspaper != null)
            newspaperUI.Show(days[_currentDay].newspaper);
        Debug.Log($"newspaperUI: {newspaperUI}, day: {days[_currentDay].newspaper}");
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
        if (_currentDay < days.Length)
        {
            StartCoroutine(LoadAfterDelay(days[_currentDay].sceneName, 0.4f));
        }
    }

    IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}