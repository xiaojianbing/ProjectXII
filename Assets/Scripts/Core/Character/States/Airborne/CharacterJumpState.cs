using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterJumpState : CharacterStateBase
    {
        public CharacterJumpState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Jump");
            Physics.Jump(); // ← 这行之前被误删了！
            // 如果起跳时不在地面（二段跳），消耗一次空中跳跃次数
            if (!ctx.IsGrounded)
            {
                ctx.AirJumpsLeft--;
            }
            Input.ConsumeJump();
            ctx.FireFeedback("jump_start");
        }

        public override void LogicUpdate()
        {
            ctx.CheckFlip(Input.NormalizedInputX);
        }

        public override void PhysicsUpdate()
        {
            float targetSpeed = Input.NormalizedInputX * Data.moveSpeed;
            Physics.Move(targetSpeed, Data.acceleration);
            
            // 向上跳时重力倍率为 1
            Physics.ApplyGravity(1f);

            // 松开跳跃键，切割速度实现可变跳跃高度
            if (Input.JumpReleased)
            {
                Physics.CutJump(Data.jumpCutMultiplier);
            }
        }

        public override void CheckTransitions()
        {
            // 在空中再次按下跳跃
            if (Input.JumpRequested && ctx.AirJumpsLeft > 0)
            {
                ctx.SM.ChangeState(ctx.States.Jump);
                return;
            }

            // 到达顶点开始下落
            if (Physics.Velocity.y < 0f)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }

            if (Input.DashRequested && ctx.States.Dash != null)
            {
                ctx.SM.ChangeState(ctx.States.Dash);
                return;
            }

            if (ctx.States.WallSlide != null && Physics.IsHittingWall && Input.NormalizedInputX == ctx.FacingDirection)
            {
                ctx.SM.ChangeState(ctx.States.WallSlide);
                return;
            }
        }
    }
}
