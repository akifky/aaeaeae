using UnityEngine;
using UnityEngine.UI;

public class DayClock : MonoBehaviour
{
    [Header("Referanslar")]
    public RectTransform clockHand; // akrep objesi
    public NewspaperUI newspaper;

    [Header("Ayarlar")]
    public float dayDuration = 120f;

    private float _timer = 0f;
    private bool _running = false;

    void Update()
    {
        if (!_running) return;

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / dayDuration);

        clockHand.localEulerAngles = new Vector3(0f, 0f, -360f * progress);

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
    }

    void OnDayEnd()
    {
        DayManager dayManager = FindFirstObjectByType<DayManager>();
        if (dayManager != null)
            dayManager.NextDay();
    }
}