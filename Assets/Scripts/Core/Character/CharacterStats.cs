using UnityEngine;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 角色数值层。管理 HP、能量槽、Buff 等属性值。
    /// Player/Enemy/Boss/可破坏道具 均可挂载 —— 不同实体启用不同属性。
    /// 实现 IDamageable，提供统一受击入口。
    /// </summary>
    public class CharacterStats : MonoBehaviour, IDamageable
    {
        [Header("HP")]
        [SerializeField] private float maxHP = 100f;
        public float MaxHP => maxHP;
        public float CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0f;

        [Header("Energy (P0-D 预埋)")]
        [SerializeField] private float maxEnergy = 100f;
        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy { get; private set; }

        // 事件驱动：UI 和其他系统订阅，避免 Update 轮询
        public event System.Action OnDeath;
        public event System.Action<float> OnDamaged;     // 参数 = 伤害量
        public event System.Action<float> OnHeal;
        public event System.Action<float> OnEnergyChanged;

        private CharacterContext _context;

        private void Awake()
        {
            _context = GetComponent<CharacterContext>();
            CurrentHP = maxHP;
            CurrentEnergy = 0f;
        }

        // ========== IDamageable 实现 ==========

        public void TakeDamage(HitData data)
        {
            if (IsDead) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - data.Damage);
            OnDamaged?.Invoke(data.Damage);

            if (CurrentHP <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        public CharacterContext GetContext() => _context;

        // ========== HP API ==========

        public void Heal(float amount)
        {
            if (IsDead) return;
            CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
            OnHeal?.Invoke(amount);
        }

        /// <summary>调试用：重置为满血</summary>
        public void ResetHP()
        {
            CurrentHP = maxHP;
        }

        // ========== Energy API（P0-D 启用） ==========

        public void AddEnergy(float amount)
        {
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, maxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy);
        }

        public bool TryConsumeEnergy(float amount)
        {
            if (CurrentEnergy < amount) return false;
            CurrentEnergy -= amount;
            OnEnergyChanged?.Invoke(CurrentEnergy);
            return true;
        }
    }
}
