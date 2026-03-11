using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterCrouchDashState : CharacterStateBase
    {
        private float _dashStartTime;
        private Vector2 _dashDirection;

        public CharacterCrouchDashState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("CrouchDash");
            Input.ConsumeDash();
            ctx.SetCrouchCollider(true);
            
            _dashStartTime = Time.time;
            ctx.IsInvincible = true;
            Physics.SetGravityActive(false);
            
            // 确定方向
            float x = Input.NormalizedInputX != 0 ? Input.NormalizedInputX : ctx.FacingDirection;
            _dashDirection = new Vector2(x, 0f).normalized;
            ctx.CheckFlip((int)_dashDirection.x);
            ctx.FireFeedback("crouch_dash_start");
        }

        public override void PhysicsUpdate()
        {
            Physics.SetVelocity(_dashDirection * Data.crouchDashSpeed);
        }

        public override void Exit()
        {
            ctx.IsInvincible = false;
            Physics.SetGravityActive(true);
            
            Physics.SetVelocityX(Physics.Velocity.x * 0.5f);
            if (Physics.Velocity.y == 0f) Physics.SetVelocityY(0f);
        }

        public override void CheckTransitions()
        {
            // 时间结束
            if (Time.time >= _dashStartTime + Data.crouchDashDuration)
            {
                if (ctx.IsGrounded)
                {
                    // 检查是否能站起，如果不能，强制转入CrouchIdle
                    if (!ctx.CanStandUp())
                    {
                        if (Input.NormalizedInputX != 0)
                            ctx.SM.ChangeState(ctx.States.CrouchWalk);
                        else
                            ctx.SM.ChangeState(ctx.States.CrouchIdle);
                        return;
                    }
                    
                    // 可以站起时，看玩家是否还在按"下"
                    if (Input.NormalizedInputY < 0)
                    {
                        if (Input.NormalizedInputX != 0)
                            ctx.SM.ChangeState(ctx.States.CrouchWalk);
                        else
                            ctx.SM.ChangeState(ctx.States.CrouchIdle);
                    }
                    else
                    {
                        ctx.SetCrouchCollider(false);
                        if (Input.NormalizedInputX != 0)
                            ctx.SM.ChangeState(ctx.States.Run);
                        else
                            ctx.SM.ChangeState(ctx.States.Idle);
                    }
                }
                else
                {
                    if (ctx.CanStandUp()) ctx.SetCrouchCollider(false);
                    ctx.SM.ChangeState(ctx.States.Fall);
                }
                return;
            }

            // 撞墙打断
            if (Physics.IsHittingWall)
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
