using UnityEngine;

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

    private Camera _cam;
    private float _targetZoom;
    private Vector3 _targetPos;

    // Pan için
    private Vector3 _dragOrigin;
    private bool _isDragging = false;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _targetZoom = _cam.orthographicSize;
        _targetPos = transform.position;
    }

    void Update()
    {
        HandleZoom();
        HandlePan();

        // Smooth zoom
        _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime * zoomSmoothing);

        // Smooth pan
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * panSmoothing);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _targetZoom -= scroll * zoomSpeed * _targetZoom; // zoom hýzý mevcut zoom'a orantýlý
        _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
    }

    void HandlePan()
    {
        // Sað týk yerine sol týk — factory týklamasýyla çakýþmamasý için
        // factory raycast'i 2D physics kullanýyor, biz sadece boþluk kontrolü yapýyoruz
        if (Input.GetMouseButtonDown(0))
        {
            // Fabrikaya týklandýysa pan baþlatma
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

        // Harita sýnýrlarý
        newPos.x = Mathf.Clamp(newPos.x, mapMin.x, mapMax.x);
        newPos.y = Mathf.Clamp(newPos.y, mapMin.y, mapMax.y);
        newPos.z = transform.position.z;

        _targetPos = newPos;
    }
}