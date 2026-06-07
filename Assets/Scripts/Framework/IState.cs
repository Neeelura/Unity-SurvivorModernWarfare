/// <summary>
/// 状态接口，提供状态的基本生命周期方法和优先级属性
/// </summary>
public interface IState
{
    void OnEnter();
    void OnUpdate();
    void OnExit();
    // 状态机优先级，数值越大优先级越高
    int Priority { get; }
}
