using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterDashState : CharacterStateBase
    {
        private float _dashStartTime;
        private Vector2 _dashDirection;

        public CharacterDashState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Dash");
            Input.ConsumeDash();
            _dashStartTime = Time.time;
            
            ctx.IsInvincible = true;
            Physics.SetGravityActive(false);
            
            // 确定冲刺方向（优先取输入，无输入取朝向）
            float x = Input.NormalizedInputX != 0 ? Input.NormalizedInputX : ctx.FacingDirection;
            _dashDirection = new Vector2(x, 0f).normalized;
            
            ctx.CheckFlip((int)_dashDirection.x);
            ctx.FireFeedback("dash_start");
        }

        public override void PhysicsUpdate()
        {
            Physics.SetVelocity(_dashDirection * Data.dashSpeed);
        }

        public override void Exit()
        {
            ctx.IsInvincible = false;
            Physics.SetGravityActive(true);
            
            // 冲刺结束保留一半惯性
            Physics.SetVelocityX(Physics.Velocity.x * 0.5f);
            if (Physics.Velocity.y == 0f) Physics.SetVelocityY(0f);
        }

        public override void CheckTransitions()
        {
            // 时间结束退出状态
            if (Time.time >= _dashStartTime + Data.dashDuration)
            {
                if (ctx.IsGrounded)
                {
                    if (Input.NormalizedInputX != 0)
                        ctx.SM.ChangeState(ctx.States.Run);
                    else
                        ctx.SM.ChangeState(ctx.States.Idle);
                }
                else
                {
                    ctx.SM.ChangeState(ctx.States.Fall);
                }
                return;
            }

            // 撞墙打断
            if (Physics.IsHittingWall)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }
        }
    }
}
