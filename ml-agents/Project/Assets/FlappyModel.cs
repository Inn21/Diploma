using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class FlappyModel : Agent, IEpisodeStats
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float deathPenalty = -1f;
    [SerializeField] private float scoreReward = 0.5f;
    [SerializeField] private float noTapPenalty = -0.005f;
    [SerializeField] private float survivalReward = 0.0025f;
    [SerializeField] private float flapPenalty = -0.001f;
    [SerializeField] private bool allowKeyboardHeuristic = false;

    public float LastReturn { get; private set; }
    public int LastSteps { get; private set; }

    public override void Initialize()
    {
        if (gameManager == null)
        {
            Debug.LogError("FlappyModel: GameManager reference is missing.", this);
            return;
        }

        gameManager.onGameOver.AddListener(Lose);
        gameManager.onAddScore.AddListener(AddScore);
    }

    public override void OnEpisodeBegin()
    {
        gameManager.RestartGame();
    }

    public void Lose()
    {
        AddReward(deathPenalty);
        LastReturn = GetCumulativeReward();
        LastSteps = StepCount;
        EndEpisode();
    }
    public void AddScore()
    {
        AddReward(scoreReward);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (actions.DiscreteActions[0] == 1)
        {
            gameManager.Tap();
            AddReward(flapPenalty);
        }

        AddReward(gameManager.isFirstTap ? noTapPenalty : survivalReward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = allowKeyboardHeuristic && (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0))
            ? 1
            : 0;
    }

    private void OnDestroy()
    {
        if (gameManager == null) return;
        gameManager.onGameOver.RemoveListener(Lose);
        gameManager.onAddScore.RemoveListener(AddScore);

    }
}
