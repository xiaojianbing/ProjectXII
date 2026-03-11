using UnityEngine;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 统一 Kinematic 物理引擎。所有物理操作的唯一入口。
    /// 🔴 高风险模块：碰撞检测精度直接决定移动手感，需大量实机调试。
    /// 
    /// 架构约束（.cursorrules §1.1）：
    /// - 使用 Rigidbody2D (Kinematic) + 自定义碰撞检测
    /// - 移动逻辑在 FixedUpdate 中通过 MovePosition 执行
    /// - 重力参数通过公式推导，禁止魔法数字
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PhysicsController : MonoBehaviour
    {
        private float _lastEdgeLogTime;
        [Header("Collision Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float skinWidth = 0.02f;
        
        public LayerMask GroundLayer => groundLayer;
        public LayerMask WallLayer => wallLayer;
        public float SkinWidth => skinWidth;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); // y改为更大的值或是考虑在后续检测时增加skinWidth配合

        [Header("Wall Check")]
        [SerializeField] private Transform wallCheckPoint;
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 0.5f);

        [Header("Edge Check")]
        [SerializeField] private Transform ledgeCheckPoint;

        // ========== 速度状态 ==========
        public Vector2 Velocity;

        // ========== 推导出的物理常量 ==========
        public float Gravity { get; private set; }
        public float JumpVelocity { get; private set; }

        // ========== 碰撞信息（每帧更新） ==========
        public bool IsGrounded { get; private set; }
        public bool IsHittingWall { get; private set; }
        public bool IsHittingCeiling { get; private set; }
        public bool IsOnEdge { get; private set; }

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private bool _gravityActive = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            // 架构约束：Kinematic 模式
            _rb.isKinematic = true;
            _rb.useFullKinematicContacts = true;
        }

        // ========== 物理参数初始化 ==========

        /// <summary>
        /// 根据期望跳跃高度和到达顶点时间，推导重力和跳跃初速度。
        /// gravity = -2h / t²
        /// jumpVelocity = |gravity| * t
        /// </summary>
        public void CalculateJumpPhysics(float height, float timeToApex)
        {
            Gravity = -(2f * height) / Mathf.Pow(timeToApex, 2f);
            JumpVelocity = Mathf.Abs(Gravity) * timeToApex;
        }

        // ========== 移动 API ==========

        /// <summary>水平移动：平滑加速/减速至目标速度</summary>
        public void Move(float targetSpeed, float accelRate)
        {
            Velocity.x = Mathf.MoveTowards(
                Velocity.x, targetSpeed, accelRate * Time.deltaTime);
        }

        /// <summary>跳跃：设置垂直速度</summary>
        public void Jump(float? overrideForce = null)
        {
            Velocity.y = overrideForce ?? JumpVelocity;
        }

        /// <summary>可变跳高：松开跳跃键时切割垂直速度</summary>
        public void CutJump(float multiplier)
        {
            if (Velocity.y > 0f) Velocity.y *= multiplier;
        }

        public void SetVelocity(Vector2 vel) => Velocity = vel;
        public void SetVelocityX(float x) => Velocity.x = x;
        public void SetVelocityY(float y) => Velocity.y = y;

        // ========== 重力控制 ==========

        /// <summary>重力开关（Dash/EdgeGrab 时关闭）</summary>
        public void SetGravityActive(bool active) => _gravityActive = active;

        /// <summary>应用重力。地面时重力清零避免持续累加下落速度</summary>
        public void ApplyGravity(float fallMultiplier = 1f)
        {
            if (!_gravityActive) return;

            // 如果在地面上并且没有主动向上跳，清空Y轴速度防止穿模累积
            if (IsGrounded && Velocity.y <= 0f)
            {
                Velocity.y = 0f;
            }
            else
            {
                float g = Gravity;
                // 下落时加速，标准平台跳跃手感
                if (Velocity.y < 0f) g *= fallMultiplier;
                Velocity.y += g * Time.fixedDeltaTime;
            }
        }

        /// <summary>
        /// 执行 Kinematic 移动。
        /// 采用分轴 BoxCast 碰撞解算：水平和垂直分别投射碰撞体，
        /// 遇到碰撞时将移动距离截断到碰撞点 - skinWidth，彻底消除穿模。
        /// </summary>
        public void ExecuteMovement()
        {
            Vector2 step = Velocity * Time.fixedDeltaTime;
            Vector2 origin = _rb.position;
            
            // 缩小投射体积，避免紧邻表面的误判
            Vector2 castSize = _collider.size - Vector2.one * skinWidth * 2f;
            castSize = Vector2.Max(castSize, Vector2.one * 0.01f); // 防止尺寸为负
            Vector2 offset = _collider.offset;

            // 合并碰撞层 (地面 + 墙壁都是固体)
            LayerMask solidMask = groundLayer | wallLayer;
            
            // 暂时禁用自身碰撞体，防止 BoxCast 检测到自己
            _collider.enabled = false;

            // ---- 水平轴解算 ----
            if (Mathf.Abs(step.x) > 0.0001f)
            {
                Vector2 dir = step.x > 0 ? Vector2.right : Vector2.left;
                float dist = Mathf.Abs(step.x);

                RaycastHit2D hit = Physics2D.BoxCast(
                    origin + offset, castSize, 0f, dir, dist + skinWidth, solidMask);

                if (hit.collider != null)
                {
                    // 截断到碰撞面前方 skinWidth 处
                    float safeDist = Mathf.Max(hit.distance - skinWidth, 0f);
                    step.x = dir.x * safeDist;
                    Velocity.x = 0f; // 撞墙后清零水平速度
                }
            }

            // ---- 垂直轴解算 ----
            if (Mathf.Abs(step.y) > 0.0001f)
            {
                Vector2 dir = step.y > 0 ? Vector2.up : Vector2.down;
                float dist = Mathf.Abs(step.y);

                // 水平轴已经修正过了，用修正后的位置继续垂直投射
                Vector2 afterHorizontal = origin + new Vector2(step.x, 0f);

                RaycastHit2D hit = Physics2D.BoxCast(
                    afterHorizontal + offset, castSize, 0f, dir, dist + skinWidth, solidMask);

                if (hit.collider != null)
                {
                    float safeDist = Mathf.Max(hit.distance - skinWidth, 0f);
                    step.y = dir.y * safeDist;
                    Velocity.y = 0f; // 撞地/天花板后清零垂直速度
                }
            }

            // 恢复碰撞体
            _collider.enabled = true;

            _rb.MovePosition(origin + step);
        }

        // ========== 碰撞检测 API ==========

        /// <summary>地面检测</summary>
        public bool CheckGround()
        {
            if (groundCheckPoint == null) return false;
            
            // 因为 BoxCast 会让角色悬浮 skinWidth 的距离，所以检测盒子需要往下偏移 skinWidth 才能真正碰到地面
            Vector2 checkPos = (Vector2)groundCheckPoint.position - new Vector2(0, skinWidth);
            IsGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
            return IsGrounded;
        }

        /// <summary>墙壁检测（朝当前朝向检测）</summary>
        public bool CheckWall(int facingDirection)
        {
            if (wallCheckPoint == null) return false;
            
            // 同样需要抵消 skinWidth 的悬浮距离
            Vector2 dir = Vector2.right * facingDirection;
            Vector2 checkPos = (Vector2)wallCheckPoint.position + dir * skinWidth;
            
            IsHittingWall = Physics2D.OverlapBox(checkPos, wallCheckSize, 0f, wallLayer);
            return IsHittingWall;
        }

        /// <summary>
        /// 边缘检测：墙壁高度有碰撞 + 头顶高度无碰撞 = 边缘
        /// 同时检测 wallLayer 和 groundLayer，确保平台边缘也能触发抓取
        /// </summary>
        public bool CheckEdge(int facingDirection)
        {
            if (wallCheckPoint == null || ledgeCheckPoint == null) return false;

            Vector2 dir = Vector2.right * facingDirection;
            
            // 射线也需要加上 skinWidth 的探测距离
            float rayDist = 0.5f + skinWidth;
            
            // 合并检测层：墙壁 + 地面（平台）都是可抓取的固体表面
            LayerMask edgeMask = wallLayer | groundLayer;
            
            bool wallHit = Physics2D.Raycast(wallCheckPoint.position, dir, rayDist, edgeMask);
            bool ledgeFree = !Physics2D.Raycast(ledgeCheckPoint.position, dir, rayDist, edgeMask);

            IsOnEdge = wallHit && ledgeFree;
            
            // 🔍 调试日志：每秒最多输出一次，避免刷屏
            if ((IsHittingWall || IsOnEdge) && Time.time - _lastEdgeLogTime > 1f)
            {
                _lastEdgeLogTime = Time.time;
                PanCake.Metroidvania.Utils.DebugLogger.Log(this,
                    $"[EdgeCheck] wallHit={wallHit} ledgeFree={ledgeFree} IsOnEdge={IsOnEdge} " +
                    $"wallCheckPos={wallCheckPoint.position} ledgeCheckPos={ledgeCheckPoint.position} " +
                    $"dir={dir} rayDist={rayDist} facing={facingDirection}", true);
            }
            
            return IsOnEdge;
        }

        // ========== Gizmos ==========

        private void OnDrawGizmos()
        {
            if (groundCheckPoint != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
            }
            if (wallCheckPoint != null)
            {
                Gizmos.color = IsHittingWall ? Color.blue : Color.cyan;
                Gizmos.DrawWireCube(wallCheckPoint.position, wallCheckSize);
            }
        }
    }
}
