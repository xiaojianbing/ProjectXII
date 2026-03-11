using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterCrouchWalkState : CharacterStateBase
    {
        public CharacterCrouchWalkState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("CrouchWalk");
            ctx.SetCrouchCollider(true);
        }

        public override void LogicUpdate()
        {
            ctx.CheckFlip(Input.NormalizedInputX);
        }

        public override void PhysicsUpdate()
        {
            float targetSpeed = Input.NormalizedInputX * Data.crouchWalkSpeed;
            Physics.Move(targetSpeed, Data.acceleration);
            Physics.ApplyGravity(Data.fallGravityMultiplier);
        }

        public override void CheckTransitions()
        {
            // 跳跃请求 → Jump
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

            // 没有水平输入 → CrouchIdle
            if (Input.NormalizedInputX == 0)
            {
                ctx.SM.ChangeState(ctx.States.CrouchIdle);
                return;
            }

            // 松开“下”键且头顶没有障碍物 → Run
            if (Input.NormalizedInputY >= 0)
            {
                if (ctx.CanStandUp())
                {
                    ctx.SetCrouchCollider(false);
                    ctx.SM.ChangeState(ctx.States.Run);
                    return;
                }
            }

            // 离开地面 → Fall
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
