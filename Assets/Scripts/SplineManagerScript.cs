using System.Collections.Generic;
using UnityEngine;

public class SplineDrawManager : MonoBehaviour
{
    private class SplineLine
    {
        public Factory from;
        public Factory to;
        public List<Vector3> points = new List<Vector3>();
        public List<Vector3> renderPoints = new List<Vector3>();
        public LineRenderer lr;
        public List<WagonController> wagons = new List<WagonController>();
    }

    private List<SplineLine> _lines = new List<SplineLine>();
    private SplineLine _active;
    private Factory _startFactory;
    private Camera _cam;

    public GameObject wagonPrefab;
    public float wagonSpeed = 2.5f;
    public float wagonSize = 0.35f;
    public float cornerRadius = 0.3f;

    private static readonly Color[] LineColors = {
        Color.red, Color.blue, Color.green, Color.yellow,
        new Color(1f,0.5f,0f), Color.cyan, Color.magenta
    };
    private int _colorIndex = 0;

    void Start() => _cam = Camera.main;

    void Update()
    {
        HandleInput();
        UpdateWagons();
    }

    void HandleInput()
    {
        if (_active != null && _startFactory != null)
        {
            Vector3 mouse = GetMouseWorld();
            Vector3 last = _startFactory.transform.position; last.z = 0f;
            SetPreview(SnapWithBend(last, mouse));
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Wagona tıklandı mı?
            Vector2 mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null)
            {
                var wagon = hit.GetComponent<WagonController>();
                if (wagon != null)
                {
                    wagon.ToggleSpeed(wagonSpeed);
                    return;
                }
            }

            Factory clicked = GetClickedFactory();
            if (clicked == null) return;

            if (_startFactory == null)
            {
                _startFactory = clicked;
                StartLine(clicked);
            }
            else
            {
                if (clicked == _startFactory) { CancelLine(); return; }
                if (LineExists(_startFactory, clicked))
                {
                    Debug.Log("Bu iki factory arasında zaten hat var.");
                    CancelLine(); return;
                }
                FinishLine(clicked);
            }
        }

        if (Input.GetMouseButtonDown(1)) CancelLine();
    }

    void StartLine(Factory from)
    {
        var go = new GameObject($"Line_{_lines.Count}");
        var lr = go.AddComponent<LineRenderer>();
        Color c = LineColors[_colorIndex % LineColors.Length]; _colorIndex++;

        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = c; lr.endColor = c;
        lr.startWidth = 0.15f; lr.endWidth = 0.15f;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.sortingOrder = -2;

        _active = new SplineLine { from = from, lr = lr };
        Vector3 start = from.transform.position; start.z = 0f;
        _active.points.Add(start);
        RefreshLine(_active);
    }

    void FinishLine(Factory to)
    {
        Vector3 last = _active.points[_active.points.Count - 1];
        Vector3 end = to.transform.position; end.z = 0f;

        foreach (var p in SnapWithBend(last, end))
            _active.points.Add(p);
        _active.points[_active.points.Count - 1] = end;

        _active.to = to;
        RefreshLine(_active);
        _lines.Add(_active);

        SpawnWagon(_active);

        _active = null;
        _startFactory = null;
    }

    void CancelLine()
    {
        if (_active != null) { Destroy(_active.lr.gameObject); _active = null; }
        _startFactory = null;
    }

    void SpawnWagon(SplineLine line)
    {
        var pts = line.renderPoints;
        var segLengths = new List<float>();
        var cumLengths = new List<float>();
        float total = 0f;
        cumLengths.Add(0f);

        for (int i = 0; i < pts.Count - 1; i++)
        {
            float len = Vector3.Distance(pts[i], pts[i + 1]);
            segLengths.Add(len);
            total += len;
            cumLengths.Add(total);
        }

        var go = Instantiate(wagonPrefab);
        go.name = $"Wagon_{line.lr.name}";

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) { sr.color = Color.white; sr.sortingOrder = -1; }

        var controller = go.GetComponent<WagonController>();
        if (controller == null) controller = go.AddComponent<WagonController>();

        controller.Init(pts, segLengths, cumLengths, total, line.from, line.to, wagonSpeed);
        line.wagons.Add(controller);
    }

    void UpdateWagons()
    {
        foreach (var line in _lines)
            foreach (var wagon in line.wagons)
                wagon.UpdateMove();
    }

    public void SetAllWagonSpeed(float newSpeed)
    {
        wagonSpeed = newSpeed;
        foreach (var line in _lines)
            foreach (var wagon in line.wagons)
                wagon.SetSpeed(newSpeed);
    }

    Vector3[] SnapWithBend(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x, dy = to.y - from.y;
        float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
        float sx = Mathf.Sign(dx), sy = Mathf.Sign(dy);

        if (ay < 0.001f) return new[] { new Vector3(to.x, from.y, 0f) };
        if (ax < 0.001f) return new[] { new Vector3(from.x, to.y, 0f) };

        float diag = Mathf.Min(ax, ay);
        Vector3 bend = new Vector3(from.x + sx * diag, from.y + sy * diag, 0f);

        return Vector3.Distance(bend, to) < 0.001f
            ? new[] { to }
            : new[] { bend, to };
    }

    List<Vector3> BuildRenderPoints(List<Vector3> pts)
    {
        var result = new List<Vector3>();
        if (pts.Count < 2) { result.AddRange(pts); return result; }

        result.Add(pts[0]);

        for (int i = 1; i < pts.Count - 1; i++)
        {
            Vector3 prev = pts[i - 1];
            Vector3 curr = pts[i];
            Vector3 next = pts[i + 1];

            Vector3 dirIn = (curr - prev).normalized;
            Vector3 dirOut = (next - curr).normalized;

            float distIn = Vector3.Distance(prev, curr);
            float distOut = Vector3.Distance(curr, next);
            float r = Mathf.Min(cornerRadius, distIn * 0.5f, distOut * 0.5f);

            Vector3 arcStart = curr - dirIn * r;
            Vector3 arcEnd = curr + dirOut * r;

            result.Add(arcStart);
            for (int s = 1; s < 8; s++)
            {
                float t = (float)s / 8;
                Vector3 p = Mathf.Pow(1 - t, 2) * arcStart
                          + 2 * (1 - t) * t * curr
                          + Mathf.Pow(t, 2) * arcEnd;
                result.Add(p);
            }
            result.Add(arcEnd);
        }

        result.Add(pts[pts.Count - 1]);
        return result;
    }

    void RefreshLine(SplineLine line)
    {
        line.renderPoints = BuildRenderPoints(line.points);
        line.lr.positionCount = line.renderPoints.Count;
        line.lr.SetPositions(line.renderPoints.ToArray());
    }

    void SetPreview(Vector3[] extra)
    {
        if (_active == null) return;
        var all = new List<Vector3>(_active.points);
        foreach (var p in extra) all.Add(p);
        var render = BuildRenderPoints(all);
        _active.lr.positionCount = render.Count;
        _active.lr.SetPositions(render.ToArray());
    }

    bool LineExists(Factory a, Factory b)
    {
        foreach (var l in _lines)
            if ((l.from == a && l.to == b) || (l.from == b && l.to == a))
                return true;
        return false;
    }

    Factory GetClickedFactory()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
        return hit.collider != null ? hit.collider.GetComponent<Factory>() : null;
    }

    Vector3 GetMouseWorld()
    {
        Vector3 s = Input.mousePosition;
        s.z = Mathf.Abs(_cam.transform.position.z);
        Vector3 w = _cam.ScreenToWorldPoint(s); w.z = 0f;
        return w;
    }

    void OnGUI()
    {
        string status = _startFactory != null
            ? $"Başlangıç: {_startFactory.name} — bitiş factory'sine tıkla"
            : "Bir factory'ye tıkla";
        GUI.Label(new Rect(10, 10, 500, 22), status);
        GUI.Label(new Rect(10, 30, 500, 22), "SAĞ TIK: iptal");
    }
}