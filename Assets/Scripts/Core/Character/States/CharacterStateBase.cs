using UnityEngine;
using ProjectXII.Core.Character;

namespace ProjectXII.Core.Character.States
{
    /// <summary>
    /// 所有 Character 共享状态的基类。
    /// 读取 ctx.Input（ICharacterInput）做决策，Player 和 Enemy 共用。
    /// </summary>
    public abstract class CharacterStateBase : IState
    {
        protected readonly CharacterContext ctx;

        // 便捷属性：避免每个状态都写 ctx.Input / ctx.Physics / ctx.MoveData
        protected ICharacterInput Input => ctx.Input;
        protected PhysicsController Physics => ctx.Physics;
        protected CharacterMovementData Data => ctx.MoveData;

        protected CharacterStateBase(CharacterContext ctx)
        {
            this.ctx = ctx;
        }

        public virtual void Enter() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
        public abstract void CheckTransitions();
    }
}
