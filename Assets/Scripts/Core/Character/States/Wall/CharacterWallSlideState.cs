using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterWallSlideState : CharacterStateBase
    {
        public CharacterWallSlideState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("WallSlide");
            // 确保贴墙时朝向墙壁
            ctx.CheckFlip(Physics.IsHittingWall ? ctx.FacingDirection : Input.NormalizedInputX);
            
            // 接触墙壁刷新空中跳跃次数 (Dead Cells 方案)
            ctx.AirJumpsLeft = Data.maxAirJumps;
            ctx.FireFeedback("wall_slide");
        }

        public override void PhysicsUpdate()
        {
            Physics.Move(0f, Data.deceleration);
            
            // 减缓下落速度
            Physics.ApplyGravity(1f);
            if (Physics.Velocity.y < Data.wallSlideSpeed)
            {
                Physics.SetVelocityY(Data.wallSlideSpeed);
            }
        }

        public override void CheckTransitions()
        {
            if (Input.JumpRequested)
            {
                if (ctx.States.WallJump != null)
                    ctx.SM.ChangeState(ctx.States.WallJump);
                return;
            }

            // 离开墙壁或方向键背离墙壁
            if (!Physics.CheckWall(ctx.FacingDirection) || Input.NormalizedInputX != ctx.FacingDirection)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }

            // 滑到底部触地
            if (ctx.IsGrounded)
            {
                ctx.SM.ChangeState(ctx.States.Idle);
                return;
            }

            // 滑动过种中遇到边缘
            if (ctx.States.EdgeGrab != null && Physics.CheckEdge(ctx.FacingDirection))
            {
                ctx.SM.ChangeState(ctx.States.EdgeGrab);
                return;
            }
        }
    }
}
