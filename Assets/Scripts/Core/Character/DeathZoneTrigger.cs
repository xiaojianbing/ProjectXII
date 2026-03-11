using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PanCake.Metroidvania.Utils;

namespace ProjectXII.Core.Character
{
    /// <summary>
    /// 深渊/死亡区域触发器。
    /// 角色掉入后：时间暂停 → 屏幕渐黑 → 传送回重生点 → 屏幕变亮 → 时间恢复。
    /// 挂在一个带 BoxCollider2D(IsTrigger=true) 的 GameObject 上。
    /// </summary>
    public class DeathZoneTrigger : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("掉入深渊扣血量")]
        [SerializeField] private float damage = 20f;
        
        [Tooltip("重生点（为空则自动推算左右边缘位置）")]
        [SerializeField] private Transform respawnPoint;
        
        [Tooltip("深渊左边缘重生 X 偏移（相对于深渊中心）")]
        [SerializeField] private float leftEdgeOffsetX = -5f;
        
        [Tooltip("深渊右边缘重生 X 偏移（相对于深渊中心）")]
        [SerializeField] private float rightEdgeOffsetX = 5f;
        
        [Tooltip("重生高度 Y")]
        [SerializeField] private float respawnY = -0.95f;
        
        [Header("Cinematic")]
        [Tooltip("渐黑等待时长")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        [Tooltip("黑屏停留时长")]
        [SerializeField] private float blackHoldDuration = 0.3f;
        [Tooltip("渐亮时长")]
        [SerializeField] private float fadeInDuration = 0.4f;
        
        private bool _isRespawning = false;
        private Image _fadeImage;

        private void Awake()
        {
            EnsureFadeOverlay();
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isRespawning) return;
            
            var stats = other.GetComponent<CharacterStats>();
            if (stats == null) return;
            
            DebugLogger.Log(this, $"☠️ {other.name} fell into the abyss! Dealing {damage} damage.", true);
            
            // 扣血
            stats.TakeDamage(new HitData
            {
                Damage = damage,
                KnockbackDirection = Vector2.zero,
                KnockbackForce = 0f,
                HitStopDuration = 0f
            });
            
            if (!stats.IsDead)
            {
                StartCoroutine(RespawnSequence(other));
            }
            else
            {
                DebugLogger.Log(this, $"💀 {other.name} is dead!", true);
            }
        }
        
        /// <summary>
        /// 电影感重生流程：冻结 → 渐黑 → 传送 → 渐亮 → 恢复
        /// 使用 unscaledDeltaTime 驱动，不受 timeScale=0 影响
        /// </summary>
        private IEnumerator RespawnSequence(Collider2D player)
        {
            _isRespawning = true;
            
            // 冻结角色
            var physics = player.GetComponent<PhysicsController>();
            if (physics != null) physics.SetVelocity(Vector2.zero);
            
            // 冻结时间
            Time.timeScale = 0f;
            
            // ---- 渐黑 ----
            yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));
            
            // ---- 黑屏停留 ----
            yield return new WaitForSecondsRealtime(blackHoldDuration);
            
            // ---- 传送：根据掉落位置选择左/右边缘重生 ----
            Vector3 respawnPos;
            if (respawnPoint != null)
            {
                respawnPos = respawnPoint.position;
            }
            else
            {
                // 判断从哪一侧掉入（玩家 X 相对深渊中心）
                float centerX = transform.position.x;
                float playerX = player.transform.position.x;
                float offsetX = (playerX < centerX) ? leftEdgeOffsetX : rightEdgeOffsetX;
                respawnPos = new Vector3(centerX + offsetX, respawnY, 0f);
            }
            player.transform.position = respawnPos;
            if (physics != null) physics.SetVelocity(Vector2.zero);
            
            DebugLogger.Log(this, $"🔄 Respawned at {respawnPos}.", true);
            
            // ---- 渐亮 ----
            yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
            
            // 恢复时间
            Time.timeScale = 1f;
            _isRespawning = false;
        }
        
        /// <summary>
        /// 用 unscaledDeltaTime 驱动的 alpha 渐变
        /// </summary>
        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            if (_fadeImage == null) EnsureFadeOverlay();
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                _fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            _fadeImage.color = new Color(0, 0, 0, toAlpha);
        }
        
        /// <summary>
        /// 自动创建全屏黑色遮罩 Canvas（如果不存在）
        /// </summary>
        private void EnsureFadeOverlay()
        {
            // 查找是否已有
            var existing = GameObject.Find("DeathFadeCanvas");
            if (existing != null)
            {
                _fadeImage = existing.GetComponentInChildren<Image>();
                return;
            }
            
            // 创建 Canvas
            var canvasGo = new GameObject("DeathFadeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // 保证在最上层
            canvasGo.AddComponent<CanvasScaler>();
            
            // 创建全屏 Image
            var imgGo = new GameObject("FadeOverlay");
            imgGo.transform.SetParent(canvasGo.transform, false);
            _fadeImage = imgGo.AddComponent<Image>();
            _fadeImage.color = new Color(0, 0, 0, 0); // 初始完全透明
            
            // 拉伸填满屏幕
            var rt = _fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            DontDestroyOnLoad(canvasGo);
        }
        
        // ========== Gizmos: 红色边框标识深渊区域 ==========
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            var coll = GetComponent<BoxCollider2D>();
            if (coll != null)
            {
                Vector3 center = transform.position + (Vector3)coll.offset;
                Vector3 size = coll.size;
                Gizmos.DrawCube(center, size);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, new Vector3(10f, 2f, 0f));
            }
        }
    }
}
