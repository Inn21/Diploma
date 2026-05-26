using System;
using Unity.AppUI.Redux;
using UnityEngine;
using Action = System.Action;

public class Pipe : MonoBehaviour
{
    private GameManager _gameManager;

    public float speed = 2f;
    private float leftEdge = -1.8f;

    public Action AddScore;

    public void SetGameManager(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    private void Update()
    {
        if (_gameManager.IsGameOver) return;

        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.localPosition.x < leftEdge)
        {
            AddScore = null;
            _gameManager.RemovePipe(this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        _gameManager.IncreaseScore();
    }
}
