using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour, IDamageable
{
    public int maxHp;
    private int currentHp;
    public float chaseRange;  // 追逐范围
    public float attackRange;  // 攻击范围
    public int damage;
    public float attackCooldown;
    public float patrolRadius; // 巡逻半径

    // DOT 灼烧状态（由 M2 火焰喷射器等武器附加）
    // DOT 造成伤害时不会触发 Hit 受击硬直，避免火焰持续造成硬直锁死
    private float dotTimer;          // DOT 剩余持续时间（秒）
    private float dotTickTimer;      // DOT 每跳计时器（每秒一跳）
    private int dotDamagePerTick;    // DOT 每跳伤害值

    [HideInInspector] public float despawnTimer;    // 死亡后计时器
    [HideInInspector] public PlayerController player;
    [HideInInspector] public Vector3 spawnPoint;
    [HideInInspector] public EnemyAnimController animController;

    public NavMeshAgent agent { get; private set; }

    // 预缓存距离平方
    public float chaseRangeSqr { get; private set; }
    public float attackRangeSqr { get; private set; }

    // FSM 状态
    public StateMachine FSM { get; private set; }
    public EnemyState_Patrol patrolState { get; private set; }
    public EnemyState_Chase chaseState { get; private set; }
    public EnemyState_Attack attackState { get; private set; }
    public EnemyState_Hit hitState { get; private set; }
    public EnemyState_Dead deadState { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        animController = GetComponentInChildren<EnemyAnimController>();

        FSM = new StateMachine();
        patrolState = new EnemyState_Patrol(this, FSM);
        chaseState = new EnemyState_Chase(this, FSM);
        attackState = new EnemyState_Attack(this, FSM);
        hitState = new EnemyState_Hit(this, FSM);
        deadState = new EnemyState_Dead(this, FSM);
    }

    void OnEnable()
    {
        GetComponent<Collider>().enabled = true;
    }

    void Start()
    {
        FSM.ChangeState(patrolState);
    }

    void Update()
    {
        FSM.Update();

        // DOT 灼烧持续伤害
        // 在 FSM 更新之后执行，确保 DOT 伤害能触发死亡状态切换
        if (dotTimer > 0)
        {
            dotTimer -= Time.deltaTime;
            dotTickTimer -= Time.deltaTime;

            // 每秒触发一次 DOT 伤害
            if (dotTickTimer <= 0f)
            {
                dotTickTimer = 1f;  // 重置每跳计时器

                // DOT 直接扣血，不触发 Hit 受击硬直
                currentHp -= dotDamagePerTick;

                // 检查是否因 DOT 死亡
                if (currentHp <= 0)
                {
                    DoDie();
                }
            }
        }
    }

    /// <summary>
    /// 初始化/重置敌人属性
    /// 由 EnemySpawner 在生成时调用，根据 EnemyData 和当前波次配置属性
    /// 对象池回收再使用时也会走此方法，保证状态重置
    /// </summary>
    /// <param name="data">敌人类型配置 SO</param>
    /// <param name="wave">当前波次</param>
    public void Initialize(EnemyData data, int wave)
    {
        // 查找 Player
        player = FindObjectOfType<PlayerController>();

        // 应用波次成长后的属性
        maxHp = data.GetScaledHp(wave);
        currentHp = maxHp;
        damage = data.GetScaledDamage(wave);
        chaseRange = data.chaseRange;
        attackRange = data.attackRange;
        attackCooldown = data.attackCooldown;
        patrolRadius = data.patrolRadius;

        // 配置 NavMeshAgent
        if (agent != null)
        {
            agent.speed = data.GetScaledSpeed(wave);
            // 强制 Warp 到当前位置，确保 Agent 注册在 NavMesh 上
            agent.Warp(transform.position);
        }

        // 重新预计算距离平方（用于 FSM 状态判断）
        chaseRangeSqr = chaseRange * chaseRange;
        attackRangeSqr = attackRange * attackRange;

        // 清空 DOT 状态（防止对象池复用残留）
        dotTimer = 0f;

        // 记录生成点
        spawnPoint = transform.position;

        // 确保碰撞体开启
        GetComponent<Collider>().enabled = true;

        // 切换到巡逻状态
        if (FSM != null && patrolState != null)
        {
            FSM.ChangeState(patrolState);
        }
    }

    public void DoMove(Vector3 target)
    {
        agent.SetDestination(target);
    }

    public void DoAttack()
    {
        IDamageable target = player.GetComponent<IDamageable>();
        target?.TakeDamage(damage, player.transform.position);
    }

    public void DoDie()
    {
        // 清空 DOT 状态（防止死亡后 DOT 继续扣血产生异常）
        dotTimer = 0f;

        // 生成掉落物（经验球/医疗包）
        if (DropSystem.Instance != null)
        {
            DropSystem.Instance.SpawnDrops(transform.position);
        }

        PoolManager.Instance.Despawn(gameObject);

        // 广播敌人死亡事件，携带经验奖励值
        EventCenter.Broadcast("EnemyDied");
    }

    /// <summary>
    /// 附加 DOT 灼烧效果
    /// 每秒造成一次伤害，持续指定秒数
    /// 新的 DOT 会覆盖旧的 DOT
    /// </summary>
    /// <param name="damagePerSecond">每秒伤害值</param>
    /// <param name="duration">持续时间</param>
    public void ApplyDOT(int damagePerSecond, float duration)
    {
        bool isNewDOT = dotTimer <= 0f;
        dotDamagePerTick = damagePerSecond;
        dotTimer = duration;
        if (isNewDOT)
            dotTickTimer = 0f;  // 新 DOT 立即开始第一跳
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (currentHp <= 0) return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            FSM.TryChangeState(deadState);
        }
        else
        {
            FSM.TryChangeState(hitState);
        }
    }
}
