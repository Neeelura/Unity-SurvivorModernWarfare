/// <summary>
/// 玩家等级与经验系统
/// 经验曲线公式：
///   升级所需 EXP = 50 + (当前等级 × 10)
/// 升级奖励：
///   - 每级：基础攻击+10%
///   - 每5级：触发武器抽取
/// </summary>
public class PlayerLevel
{
    private static  PlayerLevel instance = new PlayerLevel();
    public static PlayerLevel Instance => instance;

    public int level = 1;               // 当前等级
    public int currentExp;              // 当前经验值
    public int expToNextLevel;          // 升到下一级所需的总经验

    private PlayerLevel()
    {
        // 根据当前等级计算升级所需经验
        expToNextLevel = CalculateExpRequired(level);
    }

    /// <summary>
    /// 计算指定等级升级所需的经验值
    /// </summary>
    /// <param name="lvl">当前等级</param>
    /// <returns>升到下一级所需经验</returns>
    public int CalculateExpRequired(int lvl)
    {
        return 50 + lvl * 10;
    }

    /// <summary>
    /// 增加经验值，达到阈值自动升级
    /// 支持一次性获得大量经验连续升级
    /// </summary>
    /// <param name="amount">获得的经验值</param>
    public void GainExp(int amount)
    {
        currentExp += amount;

        // 广播经验变化事件，通知 UI 更新经验条
        EventCenter.Broadcast<int, int>("PlayerExpChanged", currentExp, expToNextLevel);

        // 检查是否升级（可能连续升多级，用 while 处理）
        while (currentExp >= expToNextLevel)
        {
            // 扣除当前等级所需经验，溢出部分计入下一级
            currentExp -= expToNextLevel;
            level++;

            // 重新计算下一级所需经验
            expToNextLevel = CalculateExpRequired(level);

            // 广播升级事件，PlayerStats 监听此事件加攻击力
            EventCenter.Broadcast<int>("PlayerLevelUp", level);

            // 广播经验变化
            EventCenter.Broadcast<int, int>("PlayerExpChanged", currentExp, expToNextLevel);

            // 每5级触发武器抽取
            if (level % 5 == 0)
            {
                UIManager.Instance.ShowPanel<WeaponSelectPanel>();
            }
        }
    }
}
