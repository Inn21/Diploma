using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class FlappyModelVector : Agent
{
    [SerializeField] private Bird bird;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float deathPenalty = -1f;
    [SerializeField] private float scoreReward = 0.5f;
    [SerializeField] private float noTapPenalty = -0.005f;
    [SerializeField] private float survivalReward = 0.0025f;
    [SerializeField] private float flapPenalty = -0.001f;
    [SerializeField] private bool allowKeyboardHeuristic = false;

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
        gameManager.Tap();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var normalizedPositionY = Mathf.InverseLerp(-2.8f, 2.8f, bird.transform.localPosition.y);
        sensor.AddObservation(normalizedPositionY);

        Rigidbody2D rb = bird.GetComponent<Rigidbody2D>();

        var normalizedVelocityY = Mathf.InverseLerp(-12f, 6f, rb.linearVelocity.y);
        sensor.AddObservation(normalizedVelocityY);

        Transform pipe = gameManager.FindNearestPipe(transform);

        if (pipe != null)
        {
            var pipeX = pipe.localPosition.x;
            var pipeY = pipe.localPosition.y;

            var normalizedPipeX = Mathf.InverseLerp(0, 2, pipeX);
            sensor.AddObservation(normalizedPipeX);

            var distanceToPipe = bird.transform.localPosition.y - pipeY;
            var normalizedDistanceToPipe = Mathf.InverseLerp(-4f, 4f, distanceToPipe);
            sensor.AddObservation(normalizedDistanceToPipe);
        }
        else
        {
            sensor.AddObservation(1f);
            sensor.AddObservation(0f);
        }
    }

    public void Lose()
    {
        AddReward(deathPenalty);
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
