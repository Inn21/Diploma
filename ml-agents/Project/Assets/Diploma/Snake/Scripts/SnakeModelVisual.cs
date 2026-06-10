using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;


public class SnakeModelVisual : Agent
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
            Debug.LogError("SnakeModelVisual: snake or gameManager reference is missing.", this);
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
