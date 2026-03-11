using UnityEngine;
using ProjectXII.Core.Character.States;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 角色行为层枢纽。持有所有核心组件引用和运行时状态标志。
    /// 状态机节点通过 Context 访问物理、属性、输入等子系统。
    /// Player/Enemy/NPC 均挂载此组件，通过 SetInput() 注入不同的输入源。
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class CharacterContext : MonoBehaviour
    {
        // ========== 输入抽象（由各 Controller 注入） ==========
        public ICharacterInput Input { get; private set; }

        // ========== 状态注册表（由各 Controller 注入） ==========
        public CharacterStateRegistry States { get; private set; }
        public void SetStates(CharacterStateRegistry states) => States = states;

        // ========== 组件引用（Awake 自动获取） ==========
        public Rigidbody2D Rb { get; private set; }
        public BoxCollider2D Collider { get; private set; }
        public Animator Anim { get; private set; }
        public CharacterStats Stats { get; private set; }
        public PhysicsController Physics { get; private set; }
        public StateMachine SM { get; private set; }

        private Transform _visualTransform;

        // ========== 运行时状态标志 ==========
        public bool IsGrounded { get; set; }
        public bool IsInvincible { get; set; }
        public bool HasSuperArmor { get; set; }
        public bool CanAct { get; set; } = true;
        public int FacingDirection { get; private set; } = 1;  // 1=右, -1=左
        public int AirJumpsLeft { get; set; } // 剩余空中跳跃次数

        // ========== 移动参数配置 ==========
        [Header("Movement Data")]
        [SerializeField] private CharacterMovementData _moveData;
        public CharacterMovementData MoveData => _moveData;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            Collider = GetComponent<BoxCollider2D>();
            Anim = GetComponentInChildren<Animator>();
            Stats = GetComponent<CharacterStats>();
            Physics = GetComponent<PhysicsController>();
            SM = new StateMachine();
            
            _visualTransform = Anim != null ? Anim.transform : transform.Find("Visuals");
        }

        /// <summary>
        /// 由各 Controller 在 Awake 中调用，注入对应的输入源。
        /// PlayerController 注入 PlayerInputHandler，
        /// EnemyController 注入 EnemyAIHandler。
        /// </summary>
        public void SetInput(ICharacterInput input) => Input = input;

        // ========== 反馈事件总线（P0-A 预埋，P0-B 接入 MMFeedbacks） ==========
        
        /// <summary>
        /// 状态在关键帧触发此事件，P0-B 的 FeedbackRouter 监听并转发给 MMF_Player。
        /// </summary>
        public event System.Action<string> OnFeedbackEvent;
        public void FireFeedback(string feedbackId) => OnFeedbackEvent?.Invoke(feedbackId);

        // ========== 通用行为方法 ==========

        /// <summary>翻转角色朝向</summary>
        public void Flip()
        {
            FacingDirection *= -1;
            // 使用旋转而非缩放，防止子对象变形
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>检查是否需要翻转（当输入方向与朝向不一致时）</summary>
        public void CheckFlip(int inputX)
        {
            if (inputX != 0 && inputX != FacingDirection)
                Flip();
        }

        /// <summary>播放动画（状态切换时由各状态调用）</summary>
        public void PlayAnimation(string animName)
        {
            if (Anim != null)
                Anim.Play(animName);
        }

        /// <summary>设置 Animator Bool 参数</summary>
        public void SetAnimBool(string param, bool value)
        {
            if (Anim != null)
                Anim.SetBool(param, value);
        }

        /// <summary>设置 Animator Float 参数</summary>
        public void SetAnimFloat(string param, float value)
        {
            if (Anim != null)
                Anim.SetFloat(param, value);
        }

        // ========== 蹲下系统附加功能 ==========

        private Vector2 _originalColliderSize;
        private Vector2 _originalColliderOffset;
        private bool _colliderInitialized = false;

        private void InitColliderData()
        {
            if (!_colliderInitialized && Collider != null)
            {
                _originalColliderSize = Collider.size;
                _originalColliderOffset = Collider.offset;
                _colliderInitialized = true;
            }
        }

        /// <summary>缩小或恢复碰撞体（保持底部不动），并同步缩放视觉表现</summary>
        public void SetCrouchCollider(bool isCrouching)
        {
            InitColliderData();
            if (Collider == null) return;

            if (isCrouching)
            {
                float newHeight = _originalColliderSize.y * MoveData.crouchColliderScale;
                Collider.size = new Vector2(_originalColliderSize.x, newHeight);
                // 保持底部位置不变，中心需要下移
                float heightDiff = _originalColliderSize.y - newHeight;
                Collider.offset = new Vector2(_originalColliderOffset.x, _originalColliderOffset.y - heightDiff * 0.5f);
                
                // 仅为了灰盒测试视觉反馈：将挂载 Animator 的子物体 (通常是 Art 节点) Y轴缩放
                if (_visualTransform != null)
                {
                    _visualTransform.localScale = new Vector3(1f, MoveData.crouchColliderScale, 1f);
                    // 补偿缩放带来的中心位置偏移（Art节点原点通常在中心）
                    _visualTransform.localPosition = new Vector3(0f, -heightDiff * 0.5f, 0f);
                }
            }
            else
            {
                Collider.size = _originalColliderSize;
                Collider.offset = _originalColliderOffset;
                
                if (_visualTransform != null)
                {
                    _visualTransform.localScale = Vector3.one;
                    _visualTransform.localPosition = Vector3.zero;
                }
            }
        }

        /// <summary>通过 OverlapBox 检测头顶是否有障碍物阻挡站立</summary>
        public bool CanStandUp()
        {
            InitColliderData();
            if (Collider == null || Physics == null) return true;

            // 站立时需要占用的空间顶部位置
            // 当前碰撞体顶部在 crouch collider 的上方，我们需要检测从当前顶部到站立顶部的空间
            // 简单的做法是：还原站立的碰撞体参数，使用 Physics2D.OverlapBox 检测，然后再恢复
            
            // 为了不干扰刚体，直接做空间体积检测：
            Vector2 standCenter = (Vector2)transform.position + _originalColliderOffset;
            
            // 只检测固体层
            LayerMask solidMask = Physics.GroundLayer | Physics.WallLayer;
            
            // 为了避免底面与地面完美贴合导致判定为碰撞（Unity 中刚好接触也会算作 Overlap），
            // 把检测框底部向上提 0.1f 忽略地面，同时顶部下降 0.02f 防止擦边天花板死锁。
            float shrinkBottom = 0.1f;
            float shrinkTop = 0.02f;
            float newHeight = _originalColliderSize.y - shrinkBottom - shrinkTop;
            
            Vector2 checkSize = new Vector2(_originalColliderSize.x - Physics.SkinWidth * 2f, newHeight);
            Vector2 checkCenter = standCenter + new Vector2(0f, (shrinkBottom - shrinkTop) * 0.5f);
            
            // 发射重叠检测，由于需要忽略自身，临时禁用 Collider
            Collider.enabled = false;
            Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, solidMask);
            Collider.enabled = true;

            return hit == null;
        }
    }
}
