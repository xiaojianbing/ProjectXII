using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterFallState : CharacterStateBase
    {
        private float _timeSinceLeftGround;

        public CharacterFallState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Fall");
            
            // 如果是从地面直接掉落（未跳跃），赋予土狼时间。如果是跳跃后下落，时间直接失效。
            if (ctx.SM.PreviousState == ctx.States.Idle || ctx.SM.PreviousState == ctx.States.Run)
                _timeSinceLeftGround = 0f;
            else
                _timeSinceLeftGround = Data.coyoteTime + 1f; // 直接让土狼时间失效
        }

        public override void LogicUpdate()
        {
            ctx.CheckFlip(Input.NormalizedInputX);
            _timeSinceLeftGround += Time.deltaTime;
        }

        public override void PhysicsUpdate()
        {
            float targetSpeed = Input.NormalizedInputX * Data.moveSpeed;
            Physics.Move(targetSpeed, Data.acceleration);
            Physics.ApplyGravity(Data.fallGravityMultiplier);
        }

        public override void CheckTransitions()
        {
            if (ctx.IsGrounded)
            {
                ctx.FireFeedback("land_impact");
                if (Input.NormalizedInputX != 0)
                    ctx.SM.ChangeState(ctx.States.Run);
                else
                    ctx.SM.ChangeState(ctx.States.Idle);
                return;
            }

            // Coyote Time 土狼时间：或者消耗空中跳跃次数
            if (Input.JumpRequested)
            {
                if (_timeSinceLeftGround <= Data.coyoteTime)
                {
                    ctx.SM.ChangeState(ctx.States.Jump);
                    return;
                }
                else if (ctx.AirJumpsLeft > 0)
                {
                    ctx.SM.ChangeState(ctx.States.Jump);
                    return;
                }
            }

            if (Input.DashRequested && ctx.States.Dash != null)
            {
                ctx.SM.ChangeState(ctx.States.Dash);
                return;
            }

            if (ctx.States.EdgeGrab != null && Physics.IsOnEdge && Input.NormalizedInputX == ctx.FacingDirection)
            {
                PanCake.Metroidvania.Utils.DebugLogger.Log("FallState", "🧗 Edge grab triggered! Transitioning to EdgeGrab.", true);
                ctx.SM.ChangeState(ctx.States.EdgeGrab);
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
