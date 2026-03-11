using UnityEngine;
using ProjectXII.Core.Character;
using ProjectXII.Core.Character.States;

namespace ProjectXII.Player
{
    /// <summary>
    /// Player Controller 顶层控制类。
    /// 极其轻量！只负责实例化所有状态并将其组合进状态机启动运行，自身不承载任何业务逻辑。
    /// </summary>
    [RequireComponent(typeof(CharacterContext))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        private CharacterContext _ctx;
        private PlayerInputHandler _input;

        private void Start()
        {
            _ctx = GetComponent<CharacterContext>();
            _input = GetComponent<PlayerInputHandler>();

            // 1. 注入玩家专属的输入源
            _ctx.SetInput(_input);

            // 2. 初始化物理参数 (基于 MoveData ScriptableObject)
            if (_ctx.MoveData != null)
            {
                _ctx.Physics.CalculateJumpPhysics(
                    _ctx.MoveData.jumpHeight, 
                    _ctx.MoveData.timeToJumpApex);
            }
            else
            {
                PanCake.Metroidvania.Utils.DebugLogger.LogError(this, "CharacterMovementData is missing on CharacterContext!");
                return;
            }

            // 3. 实例化所有共享状态节点
            var registry = new CharacterStateRegistry
            {
                Idle = new CharacterIdleState(_ctx),
                Run = new CharacterRunState(_ctx),
                Jump = new CharacterJumpState(_ctx),
                Fall = new CharacterFallState(_ctx),
                Dash = new CharacterDashState(_ctx),
                WallSlide = new CharacterWallSlideState(_ctx),
                WallJump = new CharacterWallJumpState(_ctx),
                EdgeGrab = new CharacterEdgeGrabState(_ctx),
                CrouchIdle = new CharacterCrouchIdleState(_ctx),
                CrouchWalk = new CharacterCrouchWalkState(_ctx),
                CrouchDash = new CharacterCrouchDashState(_ctx)
            };

            // 4. 将注册表注入 Context 供相互访问
            _ctx.SetStates(registry);

            // 5. 以 Idle 状态启动机器
            _ctx.SM.Initialize(registry.Idle);
        }

        private void Update()
        {
            // 在逻辑帧前置更新环境碰撞信息
            bool wasGrounded = _ctx.IsGrounded;
            _ctx.IsGrounded = _ctx.Physics.CheckGround();
            _ctx.Physics.CheckWall(_ctx.FacingDirection);
            _ctx.Physics.CheckEdge(_ctx.FacingDirection);

            // 当从空中落地时或者一直在地面上，重置空中连跳次数
            if (_ctx.IsGrounded)
            {
                _ctx.AirJumpsLeft = _ctx.MoveData.maxAirJumps;
            }

            // 驱动状态机 Update (执行状态逻辑和跳转检查)
            _ctx.SM.Update();
        }

        private void FixedUpdate()
        {
            // 物理帧：计算状态的速度/受力
            _ctx.SM.FixedUpdate();

            // 物理帧末尾：真正执行碰撞解算和 MovePosition 移动
            _ctx.Physics.ExecuteMovement();
        }
    }
}
