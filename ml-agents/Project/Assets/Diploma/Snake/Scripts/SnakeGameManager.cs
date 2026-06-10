using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SnakeGameManager : MonoBehaviour
{
    public Snake snake;
    public Food food;

    public int score;

    public UnityEvent onAddScore;
    public UnityEvent onGameOver;

    public bool IsGameOver = false;
    [SerializeField] private bool autoRestartOnGameOver = false;

    [Header("Grid")]
    public int gridWidth = 11;
    public int gridHeight = 11;
    public float cellSize = 1f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool drawGridCells = true;
    [SerializeField] private Color borderColor = new Color(0f, 1f, 0f, 0.9f);
    [SerializeField] private Color gridColor = new Color(0f, 1f, 0f, 0.25f);

    public int MaxCells => gridWidth * gridHeight;

    private void Awake()
    {
        // Wire references in Awake so the snake has its manager before the first
        // Academy step (which can run in FixedUpdate before Start), otherwise
        // ResetState() during OnEpisodeBegin would hit a null manager.
        snake.SetManager(this);
        if (food != null) food.SetGameManager(this);
    }

    private void Start()
    {
        RestartGame();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) RestartGame();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || gridWidth < 1 || gridHeight < 1 || cellSize <= 0f) return;

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Color prevColor = Gizmos.color;

        Gizmos.matrix = transform.localToWorldMatrix;

        float fullWidth = gridWidth * cellSize;
        float fullHeight = gridHeight * cellSize;
        float halfWidth = fullWidth * 0.5f;
        float halfHeight = fullHeight * 0.5f;

        if (drawGridCells)
        {
            Gizmos.color = gridColor;
            for (int x = 0; x <= gridWidth; x++)
            {
                float px = -halfWidth + x * cellSize;
                Gizmos.DrawLine(new Vector3(px, -halfHeight, 0f), new Vector3(px, halfHeight, 0f));
            }
            for (int y = 0; y <= gridHeight; y++)
            {
                float py = -halfHeight + y * cellSize;
                Gizmos.DrawLine(new Vector3(-halfWidth, py, 0f), new Vector3(halfWidth, py, 0f));
            }
        }

        Gizmos.color = borderColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(fullWidth, fullHeight, 0f));

        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }

    public Vector3 CellToLocal(Vector2Int cell)
    {
        float x = (cell.x - (gridWidth - 1) * 0.5f) * cellSize;
        float y = (cell.y - (gridHeight - 1) * 0.5f) * cellSize;
        return new Vector3(x, y, 0f);
    }

    public bool InBounds(Vector2Int cell)
        => cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;

    public bool IsCellBlocked(Vector2Int cell)
        => !InBounds(cell) || (snake != null && snake.ContainsCell(cell));

    public Vector2Int FoodCell => food != null ? food.Cell : Vector2Int.zero;

    public bool IsFoodAt(Vector2Int cell) => food != null && food.Cell == cell;

    public int FoodDistance(Vector2Int from)
    {
        Vector2Int f = FoodCell;
        return Mathf.Abs(f.x - from.x) + Mathf.Abs(f.y - from.y);
    }

    public Transform FindFood() => food != null ? food.transform : null;

    public void PlaceFood()
    {
        if (food == null) return;

        List<Vector2Int> free = new List<Vector2Int>(MaxCells);
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!snake.ContainsCell(cell)) free.Add(cell);
            }
        }

        if (free.Count == 0)
        {
            GameOver();
            return;
        }

        var chosen = free[Random.Range(0, free.Count)];
        food.SetCell(chosen, CellToLocal(chosen));
    }

    public void OnFoodEaten()
    {
        IncreaseScore();
        PlaceFood();
    }

    public void IncreaseScore()
    {
        if (IsGameOver) return;
        onAddScore?.Invoke();
        score++;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        onGameOver?.Invoke();

        if (autoRestartOnGameOver)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        score = 0;
        IsGameOver = false;

        snake.ResetState();
        PlaceFood();
    }
}
