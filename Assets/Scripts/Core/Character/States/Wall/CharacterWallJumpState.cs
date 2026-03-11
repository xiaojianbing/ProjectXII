using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterWallJumpState : CharacterStateBase
    {
        private float _timeSinceJump;

        public CharacterWallJumpState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Jump");
            Input.ConsumeJump();
            _timeSinceJump = 0f;

            // 反方向弹射
            int jumpDir = -ctx.FacingDirection;
            ctx.CheckFlip(jumpDir);
            
            Physics.SetVelocity(new Vector2(jumpDir * Data.wallJumpForce.x, Data.wallJumpForce.y));
        }

        public override void LogicUpdate()
        {
            _timeSinceJump += Time.deltaTime;
            
            // 经过锁定时间后，允许重新在空中调整方向
            if (_timeSinceJump > Data.wallJumpInputLockTime)
            {
                ctx.CheckFlip(Input.NormalizedInputX);
            }
        }

        public override void PhysicsUpdate()
        {
            // 锁定输入时间，防止立刻掉头吸回刚才的墙壁
            if (_timeSinceJump > Data.wallJumpInputLockTime)
            {
                float targetSpeed = Input.NormalizedInputX * Data.moveSpeed;
                Physics.Move(targetSpeed, Data.acceleration);
            }
            
            Physics.ApplyGravity(1f);
            
            // 同样支持可变跳高
            if (Input.JumpReleased)
            {
                Physics.CutJump(Data.jumpCutMultiplier);
            }
        }

        public override void CheckTransitions()
        {
            if (Physics.Velocity.y < 0f)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }

            if (Input.DashRequested && ctx.States.Dash != null && _timeSinceJump > Data.wallJumpInputLockTime)
            {
                ctx.SM.ChangeState(ctx.States.Dash);
                return;
            }
        }
    }
}
