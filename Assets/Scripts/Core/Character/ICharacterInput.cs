using UnityEngine;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 输入抽象层。Player 通过物理按键实现，Enemy 通过 AI 实现。
    /// 所有 CharacterXXXState 只读这个接口，不关心输入来源。
    /// </summary>
    public interface ICharacterInput
    {
        // --- 移动 ---
        Vector2 MoveInput { get; }
        int NormalizedInputX { get; }
        int NormalizedInputY { get; }

        // --- 动作请求（带输入缓存） ---
        bool JumpRequested { get; }
        bool JumpReleased { get; }
        bool DashRequested { get; }
        bool GrabRequested { get; }    // 🔒 钩索（剧情解锁）

        // --- 消耗输入（防止重复触发） ---
        void ConsumeJump();
        void ConsumeDash();
        void ConsumeGrab();
    }
}
