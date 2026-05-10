using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DayClock : MonoBehaviour
{
    public Image timerFill;
    public float dayDuration = 10f;
    private float _timer = 0f;
    private bool _running = true;

    void Update()
    {
        if (!_running) return;

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / dayDuration);

        timerFill.fillAmount = 1f - progress;
        

        if (progress >= 1f)
        {
            _running = false;
            OnDayEnd();
        }
    }

    public void StartClock()
    {
        _timer = 0f;
        _running = true;
        timerFill.fillAmount = 1f;
    }

    void OnDayEnd()
    {
        DayManager dayManager = FindFirstObjectByType<DayManager>();
        if (dayManager != null)
            dayManager._currentDay = 0;
        Debug.Log("Day ended! Restarting scene...");
        SceneManager.LoadScene(0);
    }
}