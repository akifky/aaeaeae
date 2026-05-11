using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 3f;
    public float zoomSmoothing = 8f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    [Header("Pan")]
    public float panSmoothing = 12f;

    [Header("Harita Sýnýrlarý")]
    public Vector2 mapMin = new Vector2(-20f, -20f);
    public Vector2 mapMax = new Vector2(20f, 20f);

    [Header("Çarpýþma Zoom")]
    public float crashZoom = 3f;
    public float crashDuration = 2f;

    private Camera _cam;
    private float _targetZoom;
    private Vector3 _targetPos;
    private Vector3 _dragOrigin;
    private bool _isDragging = false;
    private bool _crashed = false;

    // Fade için
    private CanvasGroup _fadeGroup;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _targetZoom = _cam.orthographicSize;
        _targetPos = transform.position;
        // Fade canvas kodlarý silindi
    }

    void Update()
    {
        // Crashed olsa bile kamera smooth harekete devam etsin
        HandleZoom();
        HandlePan();

        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.unscaledDeltaTime * zoomSmoothing);
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.unscaledDeltaTime * panSmoothing);
    }

    void HandleZoom()
    {
        if (_crashed) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;
        _targetZoom -= scroll * zoomSpeed * _targetZoom;
        _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
    }

    void HandlePan()
    {
        if (_crashed) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            if (hit.collider != null) return;

            _isDragging = true;
            _dragOrigin = _cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (!_isDragging) return;

        Vector3 currentMouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 delta = _dragOrigin - currentMouseWorld;

        Vector3 newPos = _targetPos + delta;
        newPos.x = Mathf.Clamp(newPos.x, mapMin.x, mapMax.x);
        newPos.y = Mathf.Clamp(newPos.y, mapMin.y, mapMax.y);
        newPos.z = transform.position.z;

        _targetPos = newPos;
    }

    public void OnWagonCrash(Vector3 crashPos)
    {
        if (_crashed) return;
        _crashed = true;
        StartCoroutine(CrashSequence(crashPos));
    }

    IEnumerator CrashSequence(Vector3 crashPos)
    {
        // timeScale'i yavaþça 0.2'ye lerple
        float elapsed = 0f;
        float slowDuration = 0.5f;
        float startScale = Time.timeScale;
        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(startScale, 0.2f, elapsed / slowDuration);
            yield return null;
        }
        Time.timeScale = 0.2f;

        _targetPos = new Vector3(crashPos.x, crashPos.y, transform.position.z);
        _targetZoom = crashZoom;

        elapsed = 0f;
        while (elapsed < crashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return StartCoroutine(FadeManager.Instance.DoFade(0f, 1f, 1f));

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}