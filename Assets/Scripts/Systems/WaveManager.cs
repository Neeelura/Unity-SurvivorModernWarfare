using System.Collections;
using UnityEngine;

/// <summary>
/// 波次管理器
/// 负责波次状态流转、敌人数量计算、波次切换事件广播
///
/// 流程：
///   1. StartGame() → 开始波次 1
///   2. 生成所有敌人 → 等待玩家击杀
///   3. 全部击杀 → 广播 WaveEnd → 延迟 N 秒 → 下一波
///   4. 重复直到 25 波通关 或 玩家死亡
///
/// 敌人数量公式
///   每波敌人总数 = 20 + wave × 15 + wave² × 2
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("波次配置")]
    public int totalWaves = 25;          // 总波次数
    public float waveDelay = 5f;         // 波次间准备时间（秒）
    public float startDelay = 3f;        // 游戏开始到第一波的延迟（秒）

    [Header("敌人数量额外倍率（可选调整难度）")]
    [Range(0.5f, 3f)]
    public float enemyCountMultiplier = 1f;

    // 当前状态
    public int CurrentWave { get; private set; } = 0;   // 当前波次（0=未开始，1~25）
    public int EnemiesAlive { get; private set; } = 0;  // 当前存活敌人数量
    public int EnemiesSpawned { get; private set; } = 0; // 已生成敌人数量
    public int EnemiesToSpawn { get; private set; } = 0; // 本波需要生成的敌人总数
    public int TotalKills { get; private set; } = 0;       // 累计击杀数
    public bool IsWaveActive { get; private set; } = false; // 波次进行中
    public bool IsGameOver { get; private set; } = false;   // 游戏是否结束

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 监听敌人死亡事件，用于追踪存活数量
        EventCenter.AddListener("EnemyDied", OnEnemyDied);

        // 初始化 UI
        UIManager.Instance.ShowPanel<PlayerHUDPanel>();

        // 检查是否为"继续游戏"模式
        if (SaveSystem.IsContinueMode)
        {
            SaveSystem.IsContinueMode = false;
            ContinueGame();
        }
        else
        {
            StartGame();
        }
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener("EnemyDied", OnEnemyDied);

        Instance = null;
    }


    /// <summary>
    /// 开始新游戏，启动第一波
    /// </summary>
    public void StartGame()
    {
        if (IsWaveActive) return;

        IsGameOver = false;
        CurrentWave = 0;
        TotalKills = 0;
        StartCoroutine(WaveLoop(1));
    }

    /// <summary>
    /// 从存档恢复并继续游戏
    /// 从上次保存的波次的下一波开始
    /// </summary>
    public void ContinueGame()
    {
        if (IsWaveActive) return;

        SaveData data = SaveSystem.Load();
        if (!data.CanContinue) return;

        // 恢复玩家等级与经验
        PlayerLevel.Instance.level = data.playerLevel;
        PlayerLevel.Instance.currentExp = data.playerExp;
        PlayerLevel.Instance.expToNextLevel = PlayerLevel.Instance.CalculateExpRequired(data.playerLevel);

        // 恢复武器槽位
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.RestoreSlots(data.weaponSlots);
        }

        // 重算属性
        PlayerController player = FindObjectOfType<PlayerController>();
        PlayerStats.Instance.Init(player);
        PlayerStats.Instance.Recalculate();

        IsGameOver = false;
        CurrentWave = data.currentWave;
        TotalKills = data.totalKills;

        Debug.Log($"[WaveManager] 继续游戏：从波次 {data.currentWave} 开始，等级 Lv.{data.playerLevel}，武器 {data.weaponSlots.Count} 把");

        StartCoroutine(WaveLoop(data.currentWave));
    }

    /// <summary>
    /// 波次循环协程
    /// 循环：等待 → 开始波次 → 等待敌人全部死亡 → 下一波
    /// </summary>
    private IEnumerator WaveLoop(int startWave)
    {
        // 新游戏第一波之前有准备时间
        if (startWave == 1)
            yield return new WaitForSeconds(startDelay);

        for (int wave = startWave; wave <= totalWaves; wave++)
        {
            if (IsGameOver) yield break;

            CurrentWave = wave;
            IsWaveActive = true;

            // 计算本波敌人数量
            EnemiesToSpawn = GetEnemyCount(wave);
            EnemiesSpawned = 0;
            EnemiesAlive = 0;

            Debug.Log($"[WaveManager] 波次 {wave}/{totalWaves} 开始，敌人总数: {EnemiesToSpawn}");

            // 广播波次开始事件
            EventCenter.Broadcast<int>("WaveStart", wave);

            // 等待本波所有敌人生成并被击杀
            // EnemySpawner 负责生成敌人，EnemyController.DoDie 负责减少计数
            while (EnemiesAlive > 0 || EnemiesSpawned < EnemiesToSpawn)
            {
                yield return null;
            }

            // 波次结束
            IsWaveActive = false;

            Debug.Log($"[WaveManager] 波次 {wave}/{totalWaves} 结束");

            // 广播波次结束事件
            EventCenter.Broadcast<int>("WaveEnd", wave);

            // 每波结束自动存档
            SaveSystem.SaveRuntimeState();

            // 最后一波结束后不再等待
            if (wave >= totalWaves)
            {
                Debug.Log("[WaveManager] 全部波次通关！");
                SaveSystem.ClearRuntimeState();
                EventCenter.Broadcast("GameWin");
                UIManager.Instance.ShowPanel<ResultPanel>();
                yield break;
            }

            // 下一波之前的等待时间
            yield return new WaitForSeconds(waveDelay);
        }
    }

    /// <summary>
    /// 通知 WaveManager：一个敌人生成完毕
    /// 由 EnemySpawner 在每次生成成功后调用
    /// </summary>
    public void OnEnemySpawned()
    {
        EnemiesSpawned++;
        EnemiesAlive++;
    }

    /// <summary>
    /// 敌人死亡回调（监听 "EnemyDied" 事件）
    /// </summary>
    private void OnEnemyDied()
    {
        EnemiesAlive--;
        TotalKills++;
        if (EnemiesAlive < 0) EnemiesAlive = 0;
    }

    /// <summary>
    /// 停止游戏（玩家死亡）
    /// Phase 6 中 PlayerController.DoDie() 调用
    /// </summary>
    public void StopGame()
    {
        IsGameOver = true;
        IsWaveActive = false;

        SaveSystem.ClearRuntimeState();

        EventCenter.Broadcast("GameLose");
        UIManager.Instance.ShowPanel<ResultPanel>();
    }

    /// <summary>
    /// 计算指定波次的敌人总数
    /// 公式：20 + wave × 15 + wave² × 2
    /// 结果乘以 enemyCountMultiplier 并取整
    /// </summary>
    public int GetEnemyCount(int wave)
    {
        int count = 20 + wave * 15 + wave * wave * 2;
        count = Mathf.RoundToInt(count * enemyCountMultiplier);
        return Mathf.Max(count, 1);  // 至少 1 个敌人
    }
}
