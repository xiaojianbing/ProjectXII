namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 受击接口。所有可受击对象（Player、Enemy、木箱、碎墙）均实现此接口。
    /// 攻击系统只需调用 TakeDamage(HitData)，不关心对面是什么。
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(HitData data);
        CharacterContext GetContext();
    }
}
