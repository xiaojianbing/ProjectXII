using UnityEngine;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 移动参数数据驱动配置。使用 ScriptableObject 实现。
    /// Player 和 Enemy 可引用不同的 MovementData 资产，
    /// 实现不同的移速/跳高/冲刺参数。
    /// </summary>
    [CreateAssetMenu(fileName = "MovementData", menuName = "ProjectXII/Movement Data")]
    public class CharacterMovementData : ScriptableObject
    {
        [Header("Ground Movement")]
        [Tooltip("最大水平移动速度")]
        public float moveSpeed = 10f;
        [Tooltip("加速率（越大越快到达目标速度）")]
        public float acceleration = 80f;
        [Tooltip("减速率（越大越快停下来）")]
        public float deceleration = 80f;

        [Header("Jump")]
        [Tooltip("跳跃最大高度（单位：Unity units）")]
        public float jumpHeight = 3.5f;
        [Tooltip("到达跳跃顶点的时间（秒）")]
        public float timeToJumpApex = 0.35f;
        [Tooltip("松开跳跃键后的速度切割系数（实现可变跳高）")]
        public float jumpCutMultiplier = 0.5f;
        [Tooltip("下落时重力倍率（>1 会让下落更快更有手感）")]
        public float fallGravityMultiplier = 1.8f;
        [Tooltip("离地后仍可跳跃的宽容时间（秒）")]
        public float coyoteTime = 0.1f;
        [Tooltip("落地前按跳跃的缓冲时间（秒）")]
        public float jumpBufferTime = 0.15f;
        [Tooltip("允许空中跳跃的次数（0=无二段跳，1=一次二段跳...）")]
        public int maxAirJumps = 1;

        [Header("Dash")]
        [Tooltip("冲刺速度")]
        public float dashSpeed = 20f;
        [Tooltip("冲刺持续时间（秒）")]
        public float dashDuration = 0.15f;
        [Tooltip("冲刺冷却时间（秒）")]
        public float dashCooldown = 0.5f;
        [Tooltip("空中最大冲刺次数")]
        public int maxAirDashes = 1;

        [Header("Wall")]
        [Tooltip("墙壁滑落最大速度（负值 = 向下）")]
        public float wallSlideSpeed = -2f;
        [Tooltip("蹬墙跳的力（x=水平弹射, y=垂直跳跃）")]
        public Vector2 wallJumpForce = new Vector2(12f, 16f);
        [Tooltip("蹬墙跳后锁定输入的时间（防止立即贴回墙壁）")]
        public float wallJumpInputLockTime = 0.15f;

        [Header("Edge Grab")]
        [Tooltip("边缘抓取时的位置偏移")]
        public Vector2 edgeGrabOffset = new Vector2(0.3f, -0.1f);

        [Header("Crouch")]
        [Tooltip("蹲行速度")]
        public float crouchWalkSpeed = 4f;
        [Tooltip("蹲下时碰撞体高度缩放比例")]
        public float crouchColliderScale = 0.5f;
        [Tooltip("蹲下冲刺速度")]
        public float crouchDashSpeed = 18f;
        [Tooltip("蹲下冲刺持续时间（秒）")]
        public float crouchDashDuration = 0.2f;

        [Header("Unlockable Abilities (🔒)")]
        [Tooltip("二段跳是否已解锁")]
        public bool doubleJumpUnlocked = false;
        [Tooltip("游泳是否已解锁")]
        public bool swimUnlocked = false;
        [Tooltip("钩索是否已解锁")]
        public bool grappleUnlocked = false;
    }
}
