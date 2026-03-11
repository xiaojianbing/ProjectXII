namespace ProjectXII.Core
{
    /// <summary>
    /// 通用有限状态机。同一时刻只有一个活跃状态。
    /// 由各 Controller（PlayerController / EnemyController）持有并驱动。
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        /// <summary>初始化状态机，设置起始状态并调用 Enter</summary>
        public void Initialize(IState startState)
        {
            CurrentState = startState;
            PreviousState = null;
            CurrentState.Enter();
        }

        /// <summary>切换到新状态。先 Exit 旧状态，再 Enter 新状态</summary>
        public void ChangeState(IState newState)
        {
            CurrentState.Exit();
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.Enter();
        }

        /// <summary>每帧调用（来自 MonoBehaviour.Update）</summary>
        public void Update()
        {
            CurrentState.LogicUpdate();
            CurrentState.CheckTransitions();
        }

        /// <summary>物理帧调用（来自 MonoBehaviour.FixedUpdate）</summary>
        public void FixedUpdate()
        {
            CurrentState.PhysicsUpdate();
        }
    }
}
