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
        public List<Wagon> wagons = new List<Wagon>();
    }

    private class Wagon
    {
        public GameObject go;
        public SpriteRenderer sr;
        public SplineLine line;

        public float t = 0f;
        public float speed = 2f;
        public int direction = 1;
        public float totalLength;
        public List<float> segLengths = new List<float>();
        public List<float> cumLengths = new List<float>();

        public ItemType cargo = null;
        public int cargoAmount = 0;

        public float waitTimer = 0f;
        public float waitDuration = 0.4f;
        public bool waiting = false;
    }

    private List<SplineLine> _lines = new List<SplineLine>();
    private SplineLine _active;
    private Factory _startFactory;
    private Camera _cam;

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

        var go = new GameObject($"Wagon_{line.lr.name}");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWagonSprite();
        sr.color = Color.white;
        sr.sortingOrder = -1;
        go.transform.localScale = Vector3.one * wagonSize;

        var wagon = new Wagon
        {
            go = go,
            sr = sr,
            line = line,
            speed = wagonSpeed,
            totalLength = total,
            segLengths = segLengths,
            cumLengths = cumLengths
        };

        line.wagons.Add(wagon);

        // İlk kalkışta kargo al — karşı taraf talep ediyorsa
        if (line.to.NeedsResource(line.from.production) && line.from.Stock > 0)
        {
            if (line.from.TakeResource(line.from.production, out int amount))
            {
                wagon.cargo = line.from.production;
                wagon.cargoAmount = amount;
                wagon.sr.color = wagon.cargo.color;
            }
        }
    }

    void UpdateWagons()
    {
        foreach (var line in _lines)
        {
            foreach (var wagon in line.wagons)
            {
                if (wagon.waiting)
                {
                    wagon.waitTimer += Time.deltaTime;
                    if (wagon.waitTimer >= wagon.waitDuration)
                    {
                        wagon.waiting = false;
                        wagon.waitTimer = 0f;
                    }
                    continue;
                }

                wagon.t += wagon.speed * wagon.direction * Time.deltaTime;

                if (wagon.t >= wagon.totalLength)
                {
                    wagon.t = wagon.totalLength;
                    wagon.direction = -1;
                    HandleArrival(wagon, line.to, line.from);
                }
                else if (wagon.t <= 0f)
                {
                    wagon.t = 0f;
                    wagon.direction = 1;
                    HandleArrival(wagon, line.from, line.to);
                }

                wagon.go.transform.position = GetPositionOnLine(wagon);
                Vector3 dir = GetDirectionOnLine(wagon);
                if (dir != Vector3.zero)
                    wagon.go.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, dir));
            }
        }
    }

    void HandleArrival(Wagon wagon, Factory arrived, Factory other)
    {
        // 1. Kargo varsa bırak
        if (wagon.cargo != null)
        {
            arrived.ReceiveResource(wagon.cargo, wagon.cargoAmount);
            wagon.cargo = null;
            wagon.cargoAmount = 0;
        }

        // 2. Karşı tarafa verebileceğim bir şey var mı? (arrived üretiyor, other istiyor)
        if (other.NeedsResource(arrived.production) && arrived.Stock > 0)
        {
            if (arrived.TakeResource(arrived.production, out int amount))
            {
                wagon.cargo = arrived.production;
                wagon.cargoAmount = amount;
            }
        }
        // 3. Verecek bir şey yoksa, karşı taraftan alabilecek bir şey var mı? (other üretiyor, arrived istiyor)
        else if (arrived.NeedsResource(other.production) && other.Stock > 0)
        {
            // Boş git, karşı tarafta yüklenecek — yön değiştirme, sadece git
        }

        wagon.sr.color = wagon.cargo != null ? wagon.cargo.color : Color.white;
        wagon.waitDuration = 0.4f;
        wagon.waiting = true;
    }

    Vector3 GetPositionOnLine(Wagon wagon)
    {
        var pts = wagon.line.renderPoints;
        var cum = wagon.cumLengths;
        int seg = 0;

        for (int i = 1; i < cum.Count; i++)
        {
            if (wagon.t <= cum[i]) { seg = i - 1; break; }
            seg = i - 1;
        }

        float localT = wagon.segLengths[seg] > 0f
            ? (wagon.t - cum[seg]) / wagon.segLengths[seg]
            : 0f;

        return Vector3.Lerp(pts[seg], pts[seg + 1], localT);
    }

    Vector3 GetDirectionOnLine(Wagon wagon)
    {
        var pts = wagon.line.renderPoints;
        var cum = wagon.cumLengths;
        int seg = 0;

        for (int i = 1; i < cum.Count; i++)
        {
            if (wagon.t <= cum[i]) { seg = i - 1; break; }
            seg = i - 1;
        }

        Vector3 dir = (pts[seg + 1] - pts[seg]).normalized;
        return wagon.direction == -1 ? -dir : dir;
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

    Sprite CreateWagonSprite()
    {
        int w = 32, h = 10;
        var tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.white);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
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