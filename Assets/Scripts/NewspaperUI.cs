using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class NewspaperUI : MonoBehaviour
{
    [Header("Referanslar")]
    public SpriteRenderer newspaperRenderer;

    [Header("Ayarlar")]
    public float slideDuration = 0.5f;
    public float slideDistance = 15f;

    private Vector3 _shownPos;
    private Vector3 _hiddenPos;
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Coroutine _currentAnim;
    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        _shownPos = newspaperRenderer.transform.position;
        _hiddenPos = new Vector3(_shownPos.x - slideDistance, _shownPos.y - slideDistance, _shownPos.z);
        newspaperRenderer.transform.position = _hiddenPos;
        newspaperRenderer.gameObject.SetActive(false);
    }

    public void Show(Sprite sprite)
    {
        newspaperRenderer.sprite = sprite;
        newspaperRenderer.gameObject.SetActive(true);
        if (_currentAnim != null) StopCoroutine(_currentAnim);
        _currentAnim = StartCoroutine(SlideIn());
    }

    IEnumerator SlideIn()
    {
        yield return StartCoroutine(Slide(_hiddenPos, _shownPos, 30f, 0f));
    }

    void Update()
    {
        if (!newspaperRenderer.gameObject.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = newspaperRenderer.transform.position.z;

            // Newspaper'a týklandý mý?
            Bounds bounds = newspaperRenderer.bounds;
            if (bounds.Contains(mouseWorld))
            {
                _isDragging = true;
                _dragOffset = newspaperRenderer.transform.position - mouseWorld;
                if (_currentAnim != null) StopCoroutine(_currentAnim);
            }
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = newspaperRenderer.transform.position.z;
            newspaperRenderer.transform.position = mouseWorld + _dragOffset;
        }

        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _isDragging = false;
            Vector3 pos = newspaperRenderer.transform.position;
            float dist = Vector3.Distance(pos, _shownPos);

            if (dist > slideDistance * 0.5f)
            {
                if (_currentAnim != null) StopCoroutine(_currentAnim);
                _currentAnim = StartCoroutine(ThrowOut());
            }
            else
            {
                if (_currentAnim != null) StopCoroutine(_currentAnim);
                _currentAnim = StartCoroutine(Slide(pos, _shownPos, newspaperRenderer.transform.localEulerAngles.z, 0f));
            }
        }
    }

    IEnumerator ThrowOut()
    {
        Vector3 dir = (newspaperRenderer.transform.position - _shownPos).normalized;
        Vector3 throwTarget = newspaperRenderer.transform.position + dir * slideDistance * 2f;
        yield return StartCoroutine(Slide(newspaperRenderer.transform.position, throwTarget,
            newspaperRenderer.transform.localEulerAngles.z, 30f));
        newspaperRenderer.gameObject.SetActive(false);

        DayClock clock = FindFirstObjectByType<DayClock>();
        if (clock != null) clock.StartClock();
    }

    IEnumerator Slide(Vector3 from, Vector3 to, float fromAngle, float toAngle)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            newspaperRenderer.transform.position = Vector3.Lerp(from, to, t);
            newspaperRenderer.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(fromAngle, toAngle, t));
            yield return null;
        }
        newspaperRenderer.transform.position = to;
        newspaperRenderer.transform.localEulerAngles = new Vector3(0f, 0f, toAngle);
    }
}