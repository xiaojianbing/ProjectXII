using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterIdleState : CharacterStateBase
    {
        public CharacterIdleState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Idle");
        }

        public override void PhysicsUpdate()
        {
            Physics.Move(0f, Data.deceleration);
            Physics.ApplyGravity(Data.fallGravityMultiplier);
        }

        public override void CheckTransitions()
        {
            // 按下“下”键 → CrouchIdle
            if (Input.NormalizedInputY < 0 && ctx.States.CrouchIdle != null)
            {
                ctx.SM.ChangeState(ctx.States.CrouchIdle);
                return;
            }

            // 有水平输入 → Run
            if (Input.NormalizedInputX != 0)
            {
                ctx.SM.ChangeState(ctx.States.Run);
                return;
            }

            // 跳跃请求 → Jump
            if (Input.JumpRequested && ctx.IsGrounded)
            {
                ctx.SM.ChangeState(ctx.States.Jump);
                return;
            }

            // 冲刺请求 → Dash
            if (Input.DashRequested && ctx.States.Dash != null)
            {
                ctx.SM.ChangeState(ctx.States.Dash);
                return;
            }

            // 离开地面（走出边缘）→ Fall
            if (!ctx.IsGrounded)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }
        }
    }
}
