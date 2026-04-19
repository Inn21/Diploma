using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Bird bird;

    public int score;

    public UnityEvent onAddScore;
    public UnityEvent onGameOver;

    public bool IsGameOver = false;


    [Header("Pipe spawner")]

    public GameObject pipePrefab;
    public float spawnRate = 1f;
    [Space]
    public float spawnXPosition = 1f;
    public float minHeight = -1f;
    public float maxHeight = 1f;

    private List<Pipe> pipes = new List<Pipe>();

    private float timer;

    [HideInInspector] public bool isFirstTap = true;

    private void Start()
    {
        bird.SetManager(this);
        StartGame();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) RestartGame();

        if (IsGameOver || isFirstTap) return;

        if (timer > spawnRate)
        {
            SpawnPipe();
            timer = 0;
        }

        timer += Time.deltaTime;
    }

    private void SpawnPipe()
    {
        GameObject newPipe = Instantiate(pipePrefab, transform.position+new Vector3(spawnXPosition, Random.Range(minHeight, maxHeight), 0f), Quaternion.identity, transform);

        var pipe = newPipe.GetComponent<Pipe>();

        pipe.SetGameManager(this);

        pipes.Add(pipe);
    }

    private void ResetPipeSpawner()
    {
        foreach (Pipe pipe in pipes.ToList())
        {
            if (pipe != null)
                Destroy(pipe.gameObject);
        }

        pipes.Clear();
        timer = 0;
    }

    public void Tap()
    {
        bird.Jump();
    }

    public void IncreaseScore()
    {
        if (IsGameOver) return;
        onAddScore?.Invoke();
        score++;
    }

    public void GameOver()
    {
        if(IsGameOver) return;

        onGameOver?.Invoke();

        // IsGameOver = true;
        RestartGame();
    }

    public void RestartGame()
    {
        score = 0;
        isFirstTap = true;

        ResetPipeSpawner();
        bird.ResetState();
        StartGame();
    }

    private void StartGame()
    {
        IsGameOver = false;
    }
}
