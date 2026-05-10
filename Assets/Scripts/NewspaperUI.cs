using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
public class NewspaperUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public bool IsDragging => _isDragging;
    public Image newspaperImage;
    public float slideDuration = 0.5f;
    public float stayDuration = 3f;
    private RectTransform _rect;
    private Vector2 _shownPos;
    private Vector2 _hiddenPos;
    private Vector2 _dragOffset;
    private bool _isDragging = false;
    private Coroutine _currentAnim;
    void Awake()
    {
        _rect = newspaperImage.GetComponent<RectTransform>();
        _shownPos = _rect.anchoredPosition;
        float offscreen = Screen.width + _rect.rect.width;
        _hiddenPos = new Vector2(_shownPos.x - offscreen, _shownPos.y - offscreen);
        _rect.anchoredPosition = _hiddenPos;
        newspaperImage.gameObject.SetActive(false);
    }
    public void Show(Sprite sprite)
    {
        newspaperImage.sprite = sprite;
        newspaperImage.gameObject.SetActive(true);
        if (_currentAnim != null) StopCoroutine(_currentAnim);
        _currentAnim = StartCoroutine(SlideIn());
    }
    IEnumerator SlideIn()
    {
        yield return StartCoroutine(Slide(_hiddenPos, _shownPos, 30f, 0f));
        // Oyuncu sürükleyene kadar bekle — otomatik geri dönme yok
    }
    // Oyuncu gazetenin üstüne týklayýnca
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isDragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        _dragOffset = _rect.anchoredPosition - localPoint;
        if (_currentAnim != null) StopCoroutine(_currentAnim);
    }
    public void OnDrag(PointerEventData eventData)
    {
        _isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);
        _rect.anchoredPosition = localPoint + _dragOffset;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
        // Ekranýn dýþýna sürüklendiyse yok et
        Vector2 pos = _rect.anchoredPosition;
        float threshold = Screen.width * 0.15f;
        if (Mathf.Abs(pos.x - _shownPos.x) > threshold || Mathf.Abs(pos.y - _shownPos.y) > threshold)
        {
            if (_currentAnim != null) StopCoroutine(_currentAnim);
            _currentAnim = StartCoroutine(ThrowOut());
        }
        else
        {
            // Geri yerine koy
            if (_currentAnim != null) StopCoroutine(_currentAnim);
            _currentAnim = StartCoroutine(Slide(_rect.anchoredPosition, _shownPos, _rect.localEulerAngles.z, 0f));
        }
    }
    IEnumerator ThrowOut()
    {
        float offscreen = Screen.width + _rect.rect.width;
        Vector2 throwTarget = _rect.anchoredPosition + (_rect.anchoredPosition - _shownPos).normalized * offscreen;
        yield return StartCoroutine(Slide(_rect.anchoredPosition, throwTarget, _rect.localEulerAngles.z, 30f));
        newspaperImage.gameObject.SetActive(false);
    }
    IEnumerator Slide(Vector2 from, Vector2 to, float fromAngle, float toAngle)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            _rect.anchoredPosition = Vector2.Lerp(from, to, t);
            _rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(fromAngle, toAngle, t));
            yield return null;
        }
        _rect.anchoredPosition = to;
        _rect.localEulerAngles = new Vector3(0f, 0f, toAngle);
    }
}