using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterRunState : CharacterStateBase
    {
        public CharacterRunState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            ctx.PlayAnimation("Run");
        }

        public override void LogicUpdate()
        {
            ctx.CheckFlip(Input.NormalizedInputX);
        }

        public override void PhysicsUpdate()
        {
            float targetSpeed = Input.NormalizedInputX * Data.moveSpeed;
            Physics.Move(targetSpeed, Data.acceleration);
            Physics.ApplyGravity(Data.fallGravityMultiplier);
        }

        public override void CheckTransitions()
        {
            // 按下“下”键 → CrouchWalk
            if (Input.NormalizedInputY < 0 && ctx.States.CrouchWalk != null)
            {
                ctx.SM.ChangeState(ctx.States.CrouchWalk);
                return;
            }

            if (Input.NormalizedInputX == 0)
            {
                ctx.SM.ChangeState(ctx.States.Idle);
                return;
            }

            if (Input.JumpRequested && ctx.IsGrounded)
            {
                ctx.SM.ChangeState(ctx.States.Jump);
                return;
            }

            if (Input.DashRequested && ctx.States.Dash != null)
            {
                ctx.SM.ChangeState(ctx.States.Dash);
                return;
            }

            if (!ctx.IsGrounded)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }
        }
    }
}
