using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏内 HUD 面板
/// 数据直接从各组件/单例读取，运行时通过事件更新
/// </summary>
public class PlayerHUDPanel : BasePanel
{
    [Header("血条")]
    public Image imgHp;

    [Header("经验条")]
    public Image imgExp;
    public Text txtLevel;

    [Header("波次与击杀")]
    public Text txtWave;
    public Text txtKillCount;

    [Header("武器槽位")]
    public Image[] weaponIcons = new Image[4];
    public Text[] weaponLevelTexts = new Text[4];

    [Header("属性面板")]
    public GameObject attributePanel;
    public Text txtArmor;
    public Text txtSpeed;
    public Text txtDamageBonus;
    public Text txtAttackRange;
    public Text txtPickupRange;

    public override void Init()
    {
        // 注册运行时事件
        EventCenter.AddListener<int>("PlayerHpChanged", OnHpChanged);
        EventCenter.AddListener<int, int>("PlayerExpChanged", OnExpChanged);
        EventCenter.AddListener<int>("PlayerLevelUp", OnLevelUp);
        EventCenter.AddListener<int>("WaveStart", OnWaveStart);
        EventCenter.AddListener("EnemyDied", OnEnemyDied);
    }

    protected override void OnShow()
    {
        txtLevel.text = $"Lv.1";
        txtWave.text = $"波次 1 / 25";
        txtKillCount.text = $"击杀 0";

        RefreshWeaponSlots();
        RefreshAttributes();
    }

    protected override void OnHide()
    {
        EventCenter.RemoveListener<int>("PlayerHpChanged", OnHpChanged);
        EventCenter.RemoveListener<int, int>("PlayerExpChanged", OnExpChanged);
        EventCenter.RemoveListener<int>("PlayerLevelUp", OnLevelUp);
        EventCenter.RemoveListener<int>("WaveStart", OnWaveStart);
        EventCenter.RemoveListener("EnemyDied", OnEnemyDied);
    }

    private void OnHpChanged(int currentHp)
    {
        imgHp.rectTransform.sizeDelta = new Vector2((float)currentHp / 100 * 800f, imgHp.rectTransform.sizeDelta.y);
    }

    private void OnExpChanged(int currentExp, int expToNext)
    {
        imgExp.rectTransform.sizeDelta = new Vector2(expToNext > 0 ? (float)currentExp / expToNext * 1500f : 1500f, imgExp.rectTransform.sizeDelta.y);
    }

    private void OnLevelUp(int newLevel)
    {
        txtLevel.text = $"Lv.{newLevel}";
        RefreshAttributes();
    }

    private void OnWaveStart(int wave)
    {
        txtWave.text = $"波次 {wave} / 25";
    }

    private void OnEnemyDied()
    {
        txtKillCount.text = $"击杀 {WaveManager.Instance.TotalKills}";
    }

    public void RefreshWeaponSlots()
    {
        WeaponManager wm = WeaponManager.Instance;

        for (int i = 0; i < 4; i++)
        {
            bool hasWeapon = wm != null
                          && i < wm.slots.Count
                          && wm.slots[i] != null
                          && wm.slots[i].weaponData != null;

            if (weaponIcons[i] != null)
            {
                weaponIcons[i].enabled = hasWeapon;
                if (hasWeapon)
                    weaponIcons[i].sprite = wm.slots[i].weaponData.icon;
            }

            if (weaponLevelTexts[i] != null)
            {
                weaponLevelTexts[i].text = hasWeapon
                    ? $"Lv.{wm.slots[i].level}"
                    : "";
            }
        }
    }

    public void RefreshAttributes()
    {
        txtArmor.text = $"护甲: {PlayerStats.Instance.TotalArmor}";
        txtSpeed.text = $"移速: {PlayerStats.Instance.TotalSpeed:F1}";
        txtDamageBonus.text = $"伤害加成: +{PlayerStats.Instance.TotalDamageBonus * 100f:F0}%";
        txtAttackRange.text = $"攻击范围: {PlayerStats.Instance.TotalAttackRange:F1}";
        txtPickupRange.text = $"拾取范围: {PlayerStats.Instance.TotalPickupRange:F1}";
    }

    public void ToggleAttributePanel()
    {
        if (attributePanel != null)
        {
            attributePanel.SetActive(!attributePanel.activeSelf);
            if (attributePanel.activeSelf)
                RefreshAttributes();
        }
    }

    private void OnDestroy()
    {
        OnHide();
    }
}
