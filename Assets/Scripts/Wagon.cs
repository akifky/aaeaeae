using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WagonController : MonoBehaviour
{
    [HideInInspector] public float speed = 2.5f;

    private SpriteRenderer _sr;

    private List<Vector3> _renderPoints;
    private List<float> _segLengths;
    private List<float> _cumLengths;
    private float _totalLength;

    private float _t = 0f;
    private int _direction = 1;

    private float _waitTimer = 0f;
    private float _waitDuration = 0.4f;
    private bool _waiting = false;

    private ItemType _cargo = null;
    private int _cargoAmount = 0;
    private float elapsedTime = 0f;

    private Factory _from;
    private Factory _to;
    public bool inFactory = false;

    private float _normalSpeed;

    public void Init(List<Vector3> renderPoints, List<float> segLengths, List<float> cumLengths, float totalLength, Factory from, Factory to, float speed)
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        _renderPoints = renderPoints;
        _segLengths = segLengths;
        _cumLengths = cumLengths;
        _totalLength = totalLength;
        _from = from;
        _to = to;
        this.speed = speed;
        _normalSpeed = speed;

        TryLoadCargo(_from, _to);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Factory")
            inFactory = true;

        if (collision.gameObject.tag == "Wagon" && !inFactory)
        {
            Debug.Log("Wagon collision detected! Restarting scene...");
            SceneManager.LoadScene(0);
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Factory")
            inFactory = false;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
    }

    public void UpdateMove()
    {
        if (_waiting)
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= _waitDuration)
            {
                _waiting = false;
                _waitTimer = 0f;
            }
            return;
        }

        _t += speed * _direction * Time.deltaTime;

        if (_t >= _totalLength)
        {
            _t = _totalLength;
            _direction = -1;
            HandleArrival(_to, _from);
        }
        else if (_t <= 0f)
        {
            _t = 0f;
            _direction = 1;
            HandleArrival(_from, _to);
        }

        transform.position = GetPositionOnLine();
        Vector3 dir = GetDirectionOnLine();
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, dir));
    }

    void HandleArrival(Factory arrived, Factory other)
    {
        if (_cargo != null)
        {
            arrived.ReceiveResource(_cargo, _cargoAmount);
            _cargo = null;
            _cargoAmount = 0;
        }

        if (arrived.production != null && other.NeedsResource(arrived.production) && arrived.Stock > 0)
        {
            if (arrived.TakeResource(arrived.production, out int amount))
            {
                _cargo = arrived.production;
                _cargoAmount = amount;
            }
        }
        else if (other.production != null && arrived.NeedsResource(other.production) && other.Stock > 0)
        {
            // Boş git, karşı tarafta yüklenecek
        }

        _sr.color = _cargo != null ? _cargo.color : Color.white;
        _waitDuration = 0.4f;
        _waiting = true;
    }

    void TryLoadCargo(Factory from, Factory to)
    {
        if (from.production == null) return;
        if (to.NeedsResource(from.production) && from.Stock > 0)
        {
            if (from.TakeResource(from.production, out int amount))
            {
                _cargo = from.production;
                _cargoAmount = amount;
                _sr.color = _cargo.color;
            }
        }
    }

    Vector3 GetPositionOnLine()
    {
        int seg = GetSegment();
        float localT = _segLengths[seg] > 0f
            ? (_t - _cumLengths[seg]) / _segLengths[seg]
            : 0f;
        return Vector3.Lerp(_renderPoints[seg], _renderPoints[seg + 1], localT);
    }

    Vector3 GetDirectionOnLine()
    {
        int seg = GetSegment();
        Vector3 dir = (_renderPoints[seg + 1] - _renderPoints[seg]).normalized;
        return _direction == -1 ? -dir : dir;
    }

    int GetSegment()
    {
        int seg = 0;
        for (int i = 1; i < _cumLengths.Count; i++)
        {
            if (_t <= _cumLengths[i]) { seg = i - 1; break; }
            seg = i - 1;
        }
        return seg;
    }

    public void SetSpeed(float newSpeed)
    {
        _normalSpeed = newSpeed;
        speed = newSpeed;
    }

    public void ToggleSpeed(float normalSpeed)
    {
        _normalSpeed = normalSpeed;
        if (speed > 0f)
            StartCoroutine(SlowDown());
        else
            speed = _normalSpeed;
    }

    private IEnumerator SlowDown()
    {
        float startSpeed = speed;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            speed = Mathf.Lerp(startSpeed, 0f, elapsed / duration);
            yield return null;
        }

        speed = 0f;
    }
}