using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterCrouchIdleState : CharacterStateBase
    {
        public CharacterCrouchIdleState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("CrouchIdle");
            ctx.SetCrouchCollider(true);
        }

        public override void PhysicsUpdate()
        {
            Physics.Move(0f, Data.deceleration);
            Physics.ApplyGravity(Data.fallGravityMultiplier);
        }

        public override void Exit()
        {
            // 在状态退出前，因为可能是进入站立状态，由子类或外部进行恢复
            // 但为了安全起见，如果不进入 Crouch 相关的其他状态，我们要恢复碰撞体
            // 这里我们依赖下一个状态如果是站立状态，需要我们在离开 crouch 时检查，也可以在此统一设为 false
            // 但是如果是 CrouchIdle -> CrouchWalk, 其实不需要设回。
            // 更好的做法是在可以站立的状态(如 Idle/Run)中确保恢复。
            // 为了安全，我们暂且在这里不做特殊处理，而是在 CheckTransitions 决定去哪里时，如果去站立状态，就恢复。
        }

        public override void CheckTransitions()
        {
            // 跳跃请求 → Jump (跳跃时强制站起)
            if (Input.JumpRequested && ctx.IsGrounded)
            {
                if (ctx.CanStandUp())
                {
                    ctx.SetCrouchCollider(false);
                    ctx.SM.ChangeState(ctx.States.Jump);
                    return;
                }
            }

            // 冲刺请求 → CrouchDash
            if (Input.DashRequested && ctx.States.CrouchDash != null)
            {
                ctx.SM.ChangeState(ctx.States.CrouchDash);
                return;
            }

            // 松开“下”键且头顶没有障碍物 → Idle
            if (Input.NormalizedInputY >= 0)
            {
                if (ctx.CanStandUp())
                {
                    ctx.SetCrouchCollider(false);
                    ctx.SM.ChangeState(ctx.States.Idle);
                    return;
                }
            }

            // 有水平输入
            if (Input.NormalizedInputX != 0)
            {
                // 如果按住了“下”，或者头顶有障碍物被迫保持蹲姿，则都可以进入 CrouchWalk
                if (Input.NormalizedInputY < 0 || !ctx.CanStandUp())
                {
                    if (ctx.States.CrouchWalk != null)
                    {
                        ctx.SM.ChangeState(ctx.States.CrouchWalk);
                        return;
                    }
                }
            }

            // 离开地面 → Fall (可能是走到边缘掉下)
            if (!ctx.IsGrounded)
            {
                if (ctx.CanStandUp())
                {
                    ctx.SetCrouchCollider(false);
                }
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }
        }
    }
}
