using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public Bird bird;

    public int score;

    public int LastScore { get; private set; }

    public UnityEvent onAddScore;
    public UnityEvent onGameOver;

    public bool IsGameOver = false;
    [SerializeField] private bool autoRestartOnGameOver = false;


    [Header("Pipe spawner")]

    public GameObject pipePrefab;
    public float spawnRate = 1f;
    [Space]
    public float spawnXPosition = 1f;
    public float minHeight = -1f;
    public float maxHeight = 1f;

    private List<Pipe> pipes = new List<Pipe>();
    public List<Pipe> Pipes => pipes;

    private float timer;

    [HideInInspector] public bool isFirstTap = true;

    private void Awake()
    {
        bird.SetManager(this);
    }

    private void Start()
    {
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

    public void RemovePipe(Pipe pipe)
    {
        pipes.Remove(pipe);
    }

    public Transform FindNearestPipe(Transform actorTransform)
    {
        float minDistance = float.MaxValue;
        Transform nearestPipe = null;

        foreach (Pipe pipe in pipes)
        {
            if(pipe == null) continue;

            if (pipe.transform.localPosition.x < 0) continue;

            float distance = pipe.transform.localPosition.x;
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPipe = pipe.transform;
            }
        }

        return nearestPipe;
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
        IsGameOver = true;

        LastScore = score;

        onGameOver?.Invoke();

        if (autoRestartOnGameOver)
        {
            RestartGame();
        }
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
