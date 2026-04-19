using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class FlappyModel : Agent
{
    [SerializeField] GameManager gameManager;
    public override void Initialize()
    {
        gameManager.onGameOver.AddListener(Lose);
        gameManager.onAddScore.AddListener(AddScore);
    }

    public override void OnEpisodeBegin()
    {
        gameManager.RestartGame();
    }

    public void Lose()
    {
        AddReward(-5);
        EndEpisode();
    }
    public void AddScore()
    {
        AddReward(5);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (actions.DiscreteActions[0] == 1)
        {
            gameManager.Tap();
        }

        AddReward((gameManager.isFirstTap? -0.01f: 0.0005f));

    }
}
