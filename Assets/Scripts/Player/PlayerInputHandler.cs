using UnityEngine;
using UnityEngine.InputSystem;
using ProjectXII.Core.Character;

namespace ProjectXII.Player
{
    /// <summary>
    /// 玩家输入处理器。读取 Unity New Input System 的原始信号并转换为 ICharacterInput 标准信号。
    /// 给按键操作增加缓冲窗口（Buffer Window），提升动作游戏操作手感。
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour, ICharacterInput
    {
        // ========== ICharacterInput 底层实现 ==========
        public Vector2 MoveInput { get; private set; }
        public int NormalizedInputX { get; private set; }
        public int NormalizedInputY { get; private set; }
        public bool JumpRequested { get; private set; }
        public bool JumpReleased { get; private set; }
        public bool DashRequested { get; private set; }
        public bool GrabRequested { get; private set; }

        // ========== Input Buffer 参数 ==========
        [Header("Input Buffering")]
        [Tooltip("按键有效缓存时长，比如跳跃前 0.15 秒按下依然有效")]
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float dashBufferTime = 0.15f;
        
        private float _jumpRequestTime = -1f;
        private float _dashRequestTime = -1f;

        // ========== Unity Input Actions ==========
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction dashAction;

        private void Awake()
        {
            // 在代码中直接构建，避免在 Inspector 中反复拖拽，遵循 .cursorrules 铁律
            SetupInputActions();
        }

        private void SetupInputActions()
        {
            moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
            moveAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

            jumpAction = new InputAction("Jump", binding: "<Gamepad>/buttonSouth");
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.started += _ => OnJumpInput();
            jumpAction.canceled += _ => OnJumpRelease();

            dashAction = new InputAction("Dash", binding: "<Gamepad>/rightTrigger");
            dashAction.AddBinding("<Keyboard>/leftShift");
            dashAction.started += _ => OnDashInput();
        }

        private void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
            dashAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
            dashAction.Disable();
        }

        private void Update()
        {
            // 实时读取方向键输入
            MoveInput = moveAction.ReadValue<Vector2>();
            NormalizedInputX = Mathf.RoundToInt(MoveInput.x);
            NormalizedInputY = Mathf.RoundToInt(MoveInput.y);

            // 每帧检查缓存是否过期
            CheckBufferExpiry();
        }

        // ========== 按键事件 ==========

        private void OnJumpInput()
        {
            JumpRequested = true;
            JumpReleased = false;
            _jumpRequestTime = Time.time;
        }

        private void OnJumpRelease()
        {
            JumpReleased = true;
        }

        private void OnDashInput()
        {
            DashRequested = true;
            _dashRequestTime = Time.time;
        }

        // ========== 核心：输入缓存处理 ==========

        private void CheckBufferExpiry()
        {
            if (JumpRequested && Time.time >= _jumpRequestTime + jumpBufferTime)
                JumpRequested = false;

            if (DashRequested && Time.time >= _dashRequestTime + dashBufferTime)
                DashRequested = false;
        }

        public void ConsumeJump() => JumpRequested = false;
        public void ConsumeDash() => DashRequested = false;
        public void ConsumeGrab() => GrabRequested = false;
    }
}
