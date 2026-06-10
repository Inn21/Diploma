using System.Collections.Generic;
using UnityEngine;

public class Snake : MonoBehaviour
{
    private SnakeGameManager _manager;

    [Header("Body")]
    [SerializeField] private GameObject bodySegmentPrefab;
    [SerializeField] private int initialLength = 3;
    [SerializeField] private Vector2Int startDirection = Vector2Int.right;
    [SerializeField] private bool randomizeStartDirection = false;

    [Header("Standalone play")]
    [SerializeField] private bool allowKeyboardInput = false;
    [SerializeField] private float moveInterval = 0.15f;

    private readonly List<Vector2Int> _cells = new List<Vector2Int>();
    private readonly List<Transform> _segments = new List<Transform>();

    private Vector2Int _direction;
    private Vector2Int _pendingDirection;
    private float _timer;
    private bool _agentControlled;

    public Vector2Int HeadCell => _cells.Count > 0 ? _cells[0] : Vector2Int.zero;
    public Vector2Int Direction => _direction;
    public int Length => _cells.Count;
    public IReadOnlyList<Vector2Int> Cells => _cells;

    public void SetManager(SnakeGameManager manager) => _manager = manager;

    public void SetAgentControlled(bool controlled) => _agentControlled = controlled;

    public bool ContainsCell(Vector2Int cell) => _cells.Contains(cell);

    private void Update()
    {
        if (allowKeyboardInput && !_agentControlled) ReadKeyboard();

        if (_agentControlled || _manager == null || _manager.IsGameOver) return;

        _timer += Time.deltaTime;
        if (_timer >= moveInterval)
        {
            _timer = 0f;
            Step();
        }
    }

    private void ReadKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) SetDirection(Vector2Int.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) SetDirection(Vector2Int.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SetDirection(Vector2Int.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SetDirection(Vector2Int.right);
    }

    public void SetDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;
        if (_cells.Count > 1 && dir == -_direction) return;
        _pendingDirection = dir;
    }

    public void Step()
    {
        if (_manager == null || _manager.IsGameOver || _cells.Count == 0) return;

        if (_pendingDirection != Vector2Int.zero) _direction = _pendingDirection;

        Vector2Int newHead = _cells[0] + _direction;

        if (!_manager.InBounds(newHead))
        {
            _manager.GameOver();
            return;
        }

        bool willEat = _manager.IsFoodAt(newHead);

        int bodyToCheck = willEat ? _cells.Count : _cells.Count - 1;
        for (int i = 0; i < bodyToCheck; i++)
        {
            if (_cells[i] == newHead)
            {
                _manager.GameOver();
                return;
            }
        }

        _cells.Insert(0, newHead);
        if (!willEat) _cells.RemoveAt(_cells.Count - 1);

        SyncSegments();

        if (willEat) _manager.OnFoodEaten();
    }

    public void ResetState()
    {
        _direction = startDirection == Vector2Int.zero ? Vector2Int.right : startDirection;
        if (randomizeStartDirection)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            _direction = dirs[Random.Range(0, dirs.Length)];
        }
        _pendingDirection = _direction;

        Vector2Int start = new Vector2Int(_manager.gridWidth / 2, _manager.gridHeight / 2);

        _cells.Clear();
        int len = Mathf.Max(1, initialLength);
        for (int i = 0; i < len; i++)
        {
            Vector2Int cell = start - _direction * i;
            cell.x = Mathf.Clamp(cell.x, 0, _manager.gridWidth - 1);
            cell.y = Mathf.Clamp(cell.y, 0, _manager.gridHeight - 1);
            _cells.Add(cell);
        }

        _timer = 0f;
        SyncSegments();
    }

    private void SyncSegments()
    {
        int bodyCount = _cells.Count - 1;

        while (_segments.Count < bodyCount)
        {
            Transform seg;
            if (bodySegmentPrefab != null)
            {
                seg = Instantiate(bodySegmentPrefab, transform.parent).transform;
            }
            else
            {
                seg = new GameObject("SnakeSegment").transform;
                seg.SetParent(transform.parent, false);
            }
            _segments.Add(seg);
        }
        while (_segments.Count > bodyCount)
        {
            int last = _segments.Count - 1;
            if (_segments[last] != null) Destroy(_segments[last].gameObject);
            _segments.RemoveAt(last);
        }

        transform.localPosition = _manager.CellToLocal(_cells[0]);
        for (int i = 1; i < _cells.Count; i++)
            _segments[i - 1].localPosition = _manager.CellToLocal(_cells[i]);
    }
}
