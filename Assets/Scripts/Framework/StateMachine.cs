/// <summary>
/// 状态机
/// </summary>
public class StateMachine
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        CurrentState?.OnExit();
        CurrentState = newState;
        CurrentState?.OnEnter();
    }

    /// <summary>
    /// 因部分状态需要优先级，优先级低于当前状态的状态无法切换
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    public bool TryChangeState(IState newState)
    {
        if (CurrentState != null && newState.Priority < CurrentState.Priority)
            return false;

        ChangeState(newState);
        return true;
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}
