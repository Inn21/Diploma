using UnityEngine;

public class Food : MonoBehaviour
{
    private SnakeGameManager _gameManager;

    public Vector2Int Cell { get; private set; }

    public void SetGameManager(SnakeGameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void SetCell(Vector2Int cell, Vector3 localPosition)
    {
        Cell = cell;
        transform.localPosition = localPosition;
    }
}
