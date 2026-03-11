using UnityEngine;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 攻击数据包。由攻击方构造，传递给 IDamageable.TakeDamage()。
    /// P0-A 阶段仅使用 Damage 字段（陷阱扣血），其余字段 P0-B 启用。
    /// </summary>
    public struct HitData
    {
        /// <summary>伤害值</summary>
        public float Damage;

        /// <summary>击退方向（归一化向量）</summary>
        public Vector2 KnockbackDirection;

        /// <summary>击退力度</summary>
        public float KnockbackForce;

        /// <summary>顿帧时长（秒），P0-B 启用</summary>
        public float HitStopDuration;
    }
}
