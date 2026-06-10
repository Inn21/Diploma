using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

public class ModelEvaluator : MonoBehaviour
{
    [Serializable]
    public class ModelEntry
    {
        public string name;
        public ModelAsset model;
    }

    [Header("Scene refs")]
    [SerializeField] private Agent agent;
    [SerializeField] private GameManager gameManager;

    [Header("Evaluation")]
    [SerializeField] private List<ModelEntry> models = new();
    [SerializeField] private string behaviorName = "FlappyBird";
    [SerializeField] private InferenceDevice inferenceDevice = InferenceDevice.Default;
    [SerializeField] private int episodesPerModel = 100;
    [SerializeField] private int baseSeed = 1234;
    [SerializeField] private int maxStepsPerEpisode = 5000;

    [Header("Runtime")]
    [SerializeField, Range(1f, 100f)] private float timeScale = 20f;
    [SerializeField] private bool quitOnFinish = false;

    [Header("Output")]
    [SerializeField] private string outputDirAbsolute = "";

    private BehaviorParameters m_BehaviorParams;
    private int m_ModelIdx = -1;
    private int m_EpisodeIdx;
    private int m_CurrentSeed;
    private StringBuilder m_Csv;
    private string m_CsvPath;
    private bool m_Finished;

    private void Awake()
    {
        if (agent == null || gameManager == null)
        {
            Debug.LogError("ModelEvaluator: missing agent or gameManager.", this);
            enabled = false;
            return;
        }

        m_BehaviorParams = agent.GetComponent<BehaviorParameters>();
        if (m_BehaviorParams == null)
        {
            Debug.LogError("ModelEvaluator: agent has no BehaviorParameters.", this);
            enabled = false;
            return;
        }

        m_BehaviorParams.DeterministicInference = true;

        Time.timeScale = timeScale;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        if (string.IsNullOrEmpty(outputDirAbsolute))
        {
            outputDirAbsolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "analysis", "out", "eval"));
        }
        Directory.CreateDirectory(outputDirAbsolute);
    }

    private void Start()
    {
        gameManager.onGameOver.AddListener(OnEpisodeEnded);
        AdvanceToNextModel();
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.onGameOver.RemoveListener(OnEpisodeEnded);
    }

    private void Update()
    {
        if (m_Finished) return;
        if (agent.StepCount >= maxStepsPerEpisode)
            gameManager.GameOver();
    }

    private void OnEpisodeEnded()
    {
        if (m_Finished || m_ModelIdx < 0) return;

        var modelName = models[m_ModelIdx].name;
        var stats = agent as IEpisodeStats;
        var ret = stats != null ? stats.LastReturn : agent.GetCumulativeReward();
        var score = gameManager.LastScore;
        var steps = stats != null ? stats.LastSteps : agent.StepCount;

        m_Csv.Append(modelName).Append(',')
             .Append(m_EpisodeIdx).Append(',')
             .Append(m_CurrentSeed).Append(',')
             .Append(ret.ToString("R", CultureInfo.InvariantCulture)).Append(',')
             .Append(score).Append(',')
             .Append(steps).Append('\n');

        Debug.Log($"[Eval] {modelName} ep {m_EpisodeIdx}/{episodesPerModel}  return={ret:F3}  score={score}  steps={steps}");

        m_EpisodeIdx++;
        if (m_EpisodeIdx >= episodesPerModel)
        {
            FlushCsv();
            AdvanceToNextModel();
        }
        else
        {
            m_CurrentSeed = baseSeed + m_ModelIdx * episodesPerModel + m_EpisodeIdx;
            UnityEngine.Random.InitState(m_CurrentSeed);
        }
    }

    private void AdvanceToNextModel()
    {
        m_ModelIdx++;
        if (m_ModelIdx >= models.Count)
        {
            m_Finished = true;
            Debug.Log($"[Eval] done. Output: {outputDirAbsolute}");
            Time.timeScale = 1f;
            if (quitOnFinish)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            return;
        }

        var entry = models[m_ModelIdx];
        if (entry.model == null || string.IsNullOrEmpty(entry.name))
        {
            Debug.LogError($"[Eval] entry {m_ModelIdx} invalid; skipping.");
            AdvanceToNextModel();
            return;
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        m_CsvPath = Path.Combine(outputDirAbsolute, $"eval_{entry.name}_{stamp}.csv");
        m_Csv = new StringBuilder(64 * 1024);
        m_Csv.Append("model,episode,seed,return,score,steps\n");

        m_BehaviorParams.DeterministicInference = true;
        agent.SetModel(behaviorName, entry.model, inferenceDevice);
        m_BehaviorParams.BehaviorType = BehaviorType.InferenceOnly;

        m_EpisodeIdx = 0;
        m_CurrentSeed = baseSeed + m_ModelIdx * episodesPerModel;
        UnityEngine.Random.InitState(m_CurrentSeed);

        Debug.Log($"[Eval] start model '{entry.name}' ({m_ModelIdx + 1}/{models.Count})");

        agent.EndEpisode();
    }

    private void FlushCsv()
    {
        if (m_Csv == null || string.IsNullOrEmpty(m_CsvPath)) return;
        File.WriteAllText(m_CsvPath, m_Csv.ToString());
        Debug.Log($"[Eval] wrote {m_CsvPath}");
    }
}
