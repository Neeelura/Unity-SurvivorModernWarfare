using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 敌人生成器
/// 监听 "WaveStart" 事件，按波次规则持续生成敌人
/// 生成位置：玩家周围 spawnRadius 距离的随机 NavMesh 点
/// 使用对象池管理敌人实例
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("敌人生成配置")]
    [Tooltip("敌人生成位置距离玩家的最小距离")]
    public float spawnRadius = 15f;

    [Tooltip("每个敌人之间的生成间隔（秒）")]
    public float spawnInterval = 0.3f;

    [Tooltip("NavMesh 位置采样最大尝试次数")]
    public int maxSpawnAttempts = 10;

    [Tooltip("NavMesh 采样最大距离")]
    public float navMeshSampleDist = 5f;

    // 敌人类型数据
    private List<EnemyData> enemyTypes;

    // 内部状态
    private Transform player;

    private void Awake()
    {
        // 加载敌人配置
        enemyTypes = new List<EnemyData>();
        EnemyData[] loaded = Resources.LoadAll<EnemyData>("Data/Enemies");
        enemyTypes.AddRange(loaded);
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        EventCenter.AddListener<int>("WaveStart", OnWaveStart);
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener<int>("WaveStart", OnWaveStart);
    }

    /// <summary>
    /// 波次开始回调
    /// 启动生成协程，按 EnemySpawner.enemyCount 逐个生成敌人
    /// </summary>
    private void OnWaveStart(int wave)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (WaveManager.Instance != null)
        {
            int count = WaveManager.Instance.EnemiesToSpawn;
            StartCoroutine(SpawnWave(wave, count));
        }
    }

    /// <summary>
    /// 波次生成协程
    /// 逐个生成敌人，每个之间间隔 spawnInterval 秒
    /// </summary>
    private IEnumerator SpawnWave(int wave, int totalCount)
    {
        for (int i = 0; i < totalCount; i++)
        {
            // 选择敌人类型
            EnemyData selectedEnemy = SelectEnemyType(wave);
            if (selectedEnemy == null)
            {
                Debug.LogWarning($"[EnemySpawner] 波次 {wave} 无法选择敌人类型，跳过");
                continue;
            }

            // 获取生成位置
            Vector3 spawnPos = GetSpawnPosition();

            // 从对象池生成敌人
            SpawnEnemy(selectedEnemy, spawnPos, wave);

            // 通知 WaveManager
            WaveManager.Instance?.OnEnemySpawned();

            // 间隔等待
            yield return new WaitForSeconds(spawnInterval);
        }
        Debug.Log($"[EnemySpawner] 波次 {wave} 敌人生成完毕，共 {totalCount} 个");
    }

    /// <summary>
    /// 根据波次选择敌人类型
    /// </summary>
    private EnemyData SelectEnemyType(int wave)
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return null;

        List<EnemyData> available = new List<EnemyData>();
        List<float> weights = new List<float>();

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemyData data = enemyTypes[i];
            if (data == null) continue;

            if (data.IsAvailableAtWave(wave))
            {
                available.Add(data);
                weights.Add(data.spawnWeight);
            }
        }

        return WeightedRandom(available, weights);
    }

    /// <summary>
    /// 加权随机选择
    /// 从候选列表中按权重随机选取一个元素
    /// </summary>
    private EnemyData WeightedRandom(List<EnemyData> candidates, List<float> weights)
    {
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // 计算总权重
        float totalWeight = 0f;
        for (int i = 0; i < weights.Count; i++)
            totalWeight += weights[i];

        // 随机落在 [0, totalWeight) 区间
        float random = Random.Range(0f, totalWeight);

        // 累加权重找到对应项
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (random <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// 获取敌人生成位置
    /// 在玩家周围 spawnRadius 距离处，随机找一个 NavMesh 上的有效点
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // 随机方向 × 生成半径
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 candidate = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // 在 NavMesh 上采样，确保敌人出生在可行走区域
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDist, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return player.position;
    }

    /// <summary>
    /// 从对象池生成一个敌人并初始化
    /// </summary>
    private void SpawnEnemy(EnemyData data, Vector3 position, int wave)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[EnemySpawner] {data.enemyName} 的 prefab 未配置！");
            return;
        }

        // 从对象池取出敌人实例
        GameObject enemyObj = PoolManager.Instance.Spawn(data.prefab, position, Quaternion.identity);
        if (enemyObj == null)
        {
            Debug.LogError($"[EnemySpawner] PoolManager 生成 {data.enemyName} 失败！");
            return;
        }

        // 初始化敌人属性（根据类型和波次配置）
        EnemyController enemy = enemyObj.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Initialize(data, wave);
        }
    }
}
