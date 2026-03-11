namespace ProjectXII.Core
{
    /// <summary>
    /// 状态接口。所有 HFSM 状态节点必须实现此接口。
    /// Player/Enemy/NPC 共享同一套状态实现。
    /// </summary>
    public interface IState
    {
        /// <summary>进入状态时调用</summary>
        void Enter();

        /// <summary>每帧逻辑更新 (Update)</summary>
        void LogicUpdate();

        /// <summary>物理帧更新 (FixedUpdate)</summary>
        void PhysicsUpdate();

        /// <summary>退出状态时调用</summary>
        void Exit();

        /// <summary>在 LogicUpdate 末尾检查状态切换条件</summary>
        void CheckTransitions();
    }
}
