using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SnakeModelVector : Agent
{
    [SerializeField] private Snake snake;
    [SerializeField] private SnakeGameManager gameManager;

    [Header("Rewards")]
    [SerializeField] private float deathPenalty = -1f;
    [SerializeField] private float foodReward = 1f;
    [SerializeField] private float approachReward = 0.03f;
    [SerializeField] private float survivalReward = 0f;

    [Header("Episode")]
    [SerializeField] private int maxStepsWithoutFood = 200;
    [SerializeField] private bool allowKeyboardHeuristic = false;

    private int _stepsSinceFood;

    public override void Initialize()
    {
        if (gameManager == null || snake == null)
        {
            Debug.LogError("SnakeModelVector: snake or gameManager reference is missing.", this);
            return;
        }

        gameManager.onGameOver.AddListener(Lose);
        gameManager.onAddScore.AddListener(AddScore);
        snake.SetAgentControlled(true);
    }

    public override void OnEpisodeBegin()
    {
        gameManager.RestartGame();
        _stepsSinceFood = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Fail safe: if references are gone (scene tearing down on disconnect, or
        // misconfigured) write a blank, correctly-sized observation instead of
        // throwing a NullReferenceException every frame. A thrown exception here is
        // logged with a full stack trace each step and can bloat the Unity Player
        // log to tens of GB. Keep this count in sync with the 10 values below.
        if (snake == null || gameManager == null)
        {
            for (int i = 0; i < 10; i++) sensor.AddObservation(0f);
            return;
        }

        Vector2Int head = snake.HeadCell;
        Vector2Int dir = snake.Direction;
        Vector2Int foodCell = gameManager.FoodCell;
        int w = gameManager.gridWidth;
        int h = gameManager.gridHeight;

        sensor.AddObservation((float)head.x / Mathf.Max(1, w - 1));
        sensor.AddObservation((float)head.y / Mathf.Max(1, h - 1));

        sensor.AddObservation(dir.x);
        sensor.AddObservation(dir.y);

        sensor.AddObservation((float)(foodCell.x - head.x) / Mathf.Max(1, w - 1));
        sensor.AddObservation((float)(foodCell.y - head.y) / Mathf.Max(1, h - 1));

        Vector2Int right = new Vector2Int(dir.y, -dir.x);
        Vector2Int left = new Vector2Int(-dir.y, dir.x);
        sensor.AddObservation(gameManager.IsCellBlocked(head + dir) ? 1f : 0f);
        sensor.AddObservation(gameManager.IsCellBlocked(head + right) ? 1f : 0f);
        sensor.AddObservation(gameManager.IsCellBlocked(head + left) ? 1f : 0f);

        sensor.AddObservation((float)snake.Length / gameManager.MaxCells);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (snake == null || gameManager == null) return;

        int action = actions.DiscreteActions[0];
        Vector2Int dir = snake.Direction;
        Vector2Int newDir =
            action == 1 ? new Vector2Int(dir.y, -dir.x) :
            action == 2 ? new Vector2Int(-dir.y, dir.x) :
            dir;
        snake.SetDirection(newDir);

        int scoreBefore = gameManager.score;
        int prevDistance = gameManager.FoodDistance(snake.HeadCell);

        snake.Step();

        if (gameManager.IsGameOver) return;

        bool ate = gameManager.score > scoreBefore;
        if (ate)
        {
            _stepsSinceFood = 0;
        }
        else
        {
            int newDistance = gameManager.FoodDistance(snake.HeadCell);
            AddReward((prevDistance - newDistance) * approachReward);
            _stepsSinceFood++;
        }

        AddReward(survivalReward);

        if (maxStepsWithoutFood > 0 && _stepsSinceFood >= maxStepsWithoutFood)
        {
            gameManager.GameOver();
        }
    }

    public void Lose()
    {
        AddReward(deathPenalty);
        EndEpisode();
    }

    public void AddScore()
    {
        AddReward(foodReward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        if (!allowKeyboardHeuristic) return;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) discreteActions[0] = 1;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) discreteActions[0] = 2;
    }

    private void OnDestroy()
    {
        if (gameManager == null) return;
        gameManager.onGameOver.RemoveListener(Lose);
        gameManager.onAddScore.RemoveListener(AddScore);
    }
}
