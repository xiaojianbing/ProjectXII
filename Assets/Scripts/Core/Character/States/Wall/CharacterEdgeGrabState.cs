using UnityEngine;

namespace ProjectXII.Core.Character.States
{
    public class CharacterEdgeGrabState : CharacterStateBase
    {
        // 攀爬阶段
        private enum ClimbPhase { Hanging, ClimbingUp, ClimbingOver }
        
        private ClimbPhase _phase;
        private float _climbTimer;
        private Vector2 _climbStartPos;
        private Vector2 _climbMidPos;   // 先向上到脚与平台齐平
        private Vector2 _climbEndPos;   // 再向前翻上平台
        
        // 攀爬参数 (Dead Cells 风格 — 缓慢费力)
        private const float ClimbUpDuration = 0.35f;    // 向上攀爬用时 (较慢，有体力感)
        private const float ClimbOverDuration = 0.2f;   // 翻上平台用时
        
        public CharacterEdgeGrabState(CharacterContext ctx) : base(ctx) { }

        public override void Enter()
        {
            PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", "✅ EdgeGrab entered! Gravity OFF, Velocity zeroed.", true);
            ctx.PlayAnimation("EdgeGrab");
            Physics.SetGravityActive(false);
            Physics.SetVelocity(Vector2.zero);
            
            _phase = ClimbPhase.Hanging;
            _climbTimer = 0f;
            
            // ---- 吸附到固定悬挂位置 ----
            // 向下 Raycast 找平台顶面，让角色碰撞体顶部与平台顶面对齐
            SnapToEdge();
            ctx.FireFeedback("edge_grab");
        }
        
        /// <summary>
        /// 将角色精确吸附到边缘位置：
        /// 1. 水平射线找墙面 X → 确定贴墙位置
        /// 2. 从墙面内侧上方向下射线找平台顶面 Y → 确定悬挂高度
        /// 3. 角色碰撞体顶部对齐平台顶面，身体紧贴墙面
        /// </summary>
        private void SnapToEdge()
        {
            var collSize = ctx.Collider.size;
            float halfHeight = collSize.y * 0.5f;
            float halfWidth = collSize.x * 0.5f;
            
            LayerMask solidMask = Physics.GroundLayer | Physics.WallLayer;
            int facing = ctx.FacingDirection;
            
            // ---- Step 1: 找到墙面 X ----
            // 从角色中心向面朝方向打水平射线
            RaycastHit2D wallHit = Physics2D.Raycast(
                ctx.transform.position, 
                Vector2.right * facing, 
                2f, solidMask);
            
            if (!wallHit.collider)
            {
                PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", "⚠️ Snap failed: wall ray missed", true);
                return;
            }
            
            float wallSurfaceX = wallHit.point.x;
            float snapX = wallSurfaceX - facing * (halfWidth + 0.02f);
            
            // ---- Step 2: 找到平台顶面 Y ----
            // 从墙面的内侧 (平台上方) 向下打射线
            // probeX 在墙面内侧 0.1 单位处（确保落在平台上而不是空气中）
            float probeX = wallSurfaceX + facing * 0.1f;
            float probeY = ctx.transform.position.y + halfHeight + 3f; // 从很高的地方往下找
            RaycastHit2D downHit = Physics2D.Raycast(
                new Vector2(probeX, probeY), 
                Vector2.down, 
                6f, solidMask);
            
            if (!downHit.collider)
            {
                PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", "⚠️ Snap failed: down ray missed", true);
                return;
            }
            
            float platformTopY = downHit.point.y;
            // 角色中心 Y = 平台顶面 - 半身高（碰撞体顶部齐平台面）
            float snapY = platformTopY - halfHeight;
            
            ctx.transform.position = new Vector3(snapX, snapY, ctx.transform.position.z);
            
            PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", 
                $"📌 Snapped to edge: wallX={wallSurfaceX:F2} platformTop={platformTopY:F2} snapPos=({snapX:F2}, {snapY:F2})", true);
        }

        public override void LogicUpdate()
        {
            if (_phase == ClimbPhase.Hanging) return;
            
            _climbTimer += Time.deltaTime;
            
            if (_phase == ClimbPhase.ClimbingUp)
            {
                float t = Mathf.Clamp01(_climbTimer / ClimbUpDuration);
                // 使用 ease-in 曲线：开始慢→后面快，模拟从手臂撑起的吃力感
                t = t * t;
                
                Vector2 pos = Vector2.Lerp(_climbStartPos, _climbMidPos, t);
                ctx.transform.position = new Vector3(pos.x, pos.y, ctx.transform.position.z);
                
                if (_climbTimer >= ClimbUpDuration)
                {
                    _phase = ClimbPhase.ClimbingOver;
                    _climbTimer = 0f;
                }
            }
            else if (_phase == ClimbPhase.ClimbingOver)
            {
                float t = Mathf.Clamp01(_climbTimer / ClimbOverDuration);
                // 使用 ease-out 曲线：翻上去后减速停稳
                t = 1f - (1f - t) * (1f - t);
                
                Vector2 pos = Vector2.Lerp(_climbMidPos, _climbEndPos, t);
                ctx.transform.position = new Vector3(pos.x, pos.y, ctx.transform.position.z);
                
                if (_climbTimer >= ClimbOverDuration)
                {
                    PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", "🏁 Climb-up complete! Transitioning to Idle.", true);
                    ctx.SM.ChangeState(ctx.States.Idle);
                }
            }
        }

        public override void PhysicsUpdate()
        {
            // 攀爬全程由 LogicUpdate 手动位移，物理引擎不插手
            Physics.SetVelocity(Vector2.zero);
        }

        public override void Exit()
        {
            Physics.SetGravityActive(true);
        }

        public override void CheckTransitions()
        {
            // 攀爬中禁止任何操作
            if (_phase != ClimbPhase.Hanging) return;
            
            // 跳跃键 → 蹬墙跳
            if (Input.JumpRequested)
            {
                Physics.Jump(Physics.JumpVelocity * 0.8f); 
                ctx.SM.ChangeState(ctx.States.Jump);
                return;
            }
            
            // 按"上" → 攀爬上平台
            if (Input.NormalizedInputY > 0)
            {
                StartClimb();
                return;
            }

            // 按"下" → 放弃抓取
            if (Input.NormalizedInputY < 0)
            {
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }

            // 反向移动 → 脱离
            if (Input.NormalizedInputX != 0 && Input.NormalizedInputX != ctx.FacingDirection)
            {
                ctx.CheckFlip(Input.NormalizedInputX);
                ctx.SM.ChangeState(ctx.States.Fall);
                return;
            }
        }
        
        /// <summary>
        /// 计算攀爬路径：
        /// 1. 向上移动，让角色脚底与平台顶面齐平（不多不少）
        /// 2. 水平前移，翻上平台站稳
        /// 平台顶面 Y 通过向下 Raycast 精确获取。
        /// </summary>
        private void StartClimb()
        {
            PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", "🧗 Starting climb-up!", true);
            
            _phase = ClimbPhase.ClimbingUp;
            _climbTimer = 0f;
            
            var collSize = ctx.Collider.size;
            float halfHeight = collSize.y * 0.5f;
            
            _climbStartPos = ctx.transform.position;
            
            // ---- 精确寻找平台顶面 ----
            // 策略：从角色头顶上方、稍微偏向平台内侧的位置向下打射线
            LayerMask solidMask = Physics.GroundLayer | Physics.WallLayer;
            float probeX = _climbStartPos.x + ctx.FacingDirection * (collSize.x * 0.5f + 0.15f);
            Vector2 probeOrigin = new Vector2(probeX, _climbStartPos.y + halfHeight + 2f);
            RaycastHit2D downHit = Physics2D.Raycast(probeOrigin, Vector2.down, 4f, solidMask);
            
            float platformTopY;
            if (downHit.collider != null)
            {
                platformTopY = downHit.point.y;
            }
            else
            {
                // 保底：用碰撞体全高估算
                platformTopY = _climbStartPos.y + collSize.y;
            }
            
            // 阶段1终点：角色中心 = 平台顶面 + 半身高 (脚踩平台)
            float standY = platformTopY + halfHeight + 0.02f;
            _climbMidPos = new Vector2(_climbStartPos.x, standY);
            
            // 阶段2终点：向面朝方向平移，完全站到平台上
            float forwardDist = collSize.x * 1.2f;
            _climbEndPos = new Vector2(_climbStartPos.x + ctx.FacingDirection * forwardDist, standY);
            
            PanCake.Metroidvania.Utils.DebugLogger.Log("EdgeGrabState", 
                $"Climb path: start={_climbStartPos} mid={_climbMidPos} end={_climbEndPos} platformTop={platformTopY}", true);
            
            ctx.PlayAnimation("EdgeClimb");
        }
    }
}
