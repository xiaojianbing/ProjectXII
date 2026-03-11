using UnityEngine;
using ProjectXII.Core.Character;

namespace ProjectXII.Core.Character.States
{
    /// <summary>
    /// 状态注册表。由 Controller 创建所有状态实例并注册到此处。
    /// 各状态通过 ctx.States.XXX 访问其他状态，解决循环依赖。
    /// </summary>
    public class CharacterStateRegistry
    {
        public CharacterIdleState Idle;
        public CharacterRunState Run;
        public CharacterJumpState Jump;
        public CharacterFallState Fall;
        public CharacterDashState Dash;
        public CharacterWallSlideState WallSlide;
        public CharacterWallJumpState WallJump;
        public CharacterEdgeGrabState EdgeGrab;
        
        // 蹲下系统
        public CharacterCrouchIdleState CrouchIdle;
        public CharacterCrouchWalkState CrouchWalk;
        public CharacterCrouchDashState CrouchDash;

        // 🔒 解锁后注册
        // public CharacterDoubleJumpState DoubleJump;
        // public CharacterSwimState Swim;
        // public CharacterGrappleState Grapple;
    }
}
