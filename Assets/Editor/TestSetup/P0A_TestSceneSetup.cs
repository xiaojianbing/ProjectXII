#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectXII.Player;
using ProjectXII.Core.Character;

namespace ProjectXII.Editor.TestSetup
{
    /// <summary>
    /// 一键生成 P0-A 物理移动测试场景。
    /// 位于顶部菜单：ProjectXII -> Test -> Setup P0-A Locomotion Scene
    /// </summary>
    public class P0A_TestSceneSetup
    {
        // ========== 场景布局安全约束 ==========
        // 角色碰撞体尺寸
        const float CHAR_STANDING_HEIGHT = 2.0f;
        const float CHAR_CROUCHING_HEIGHT = 1.0f; // = STANDING * crouchColliderScale(0.5)
        const float CHAR_WIDTH = 0.8f;
        // 平台间最小净空 — 任何两块碰撞体之间必须满足以下间距
        const float MIN_VERTICAL_CLEARANCE = 2.5f;   // 垂直净空 > 站立高度，否则角色无法站立通过
        const float CROUCH_TUNNEL_CLEARANCE = 1.4f;   // 蹲下通道净空: > 蹲下高度 且 < 站立高度
        const float MIN_HORIZONTAL_GAP = 1.2f;         // 水平间距 > 角色宽度，否则角色被卡住
        const float PLATFORM_THICKNESS = 0.6f;         // 标准平台厚度
        [MenuItem("ProjectXII/Test/Setup P0-A Locomotion Scene")]
        public static void SetupScene()
        {
            // 在建场景前先自动生成 Layer，防止报警告
            EnsureLayerExists(8, "Ground");
            EnsureLayerExists(9, "Wall");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "P0A_LocomotionTest";

            // 1. Scene Environment (Camera OrthoSize=8, 视野高度 16 单位, 角色 1/8 屏幕)
            GameObject env = new GameObject("Environment");
            
            // 地面 Layer 8 (Ground) — 分成左右两段，中间留深渊 (x=-4 到 x=4)
            CreateBlock("Ground_Left", env.transform, new Vector3(-13f, -3f, 0), new Vector3(18f, 2f, 1f), 8);
            CreateBlock("Ground_Right", env.transform, new Vector3(13f, -3f, 0), new Vector3(18f, 2f, 1f), 8);
            
            // 墙壁 Layer 9 (Wall) — 起到边界围栏作用
            CreateBlock("LeftWall", env.transform, new Vector3(-25f, 8f, 0), new Vector3(2f, 24f, 1f), 9);
            CreateBlock("RightWall", env.transform, new Vector3(25f, 8f, 0), new Vector3(2f, 24f, 1f), 9);
            
            // 多个漂浮平台 — 阶梯式分布，测试不同高度跳跃
            CreateBlock("Platform_1", env.transform, new Vector3(-8f, 0f, 0), new Vector3(5f, 0.6f, 1f), 8);
            CreateBlock("Platform_2", env.transform, new Vector3(0f, 3f, 0), new Vector3(6f, 0.6f, 1f), 8);
            CreateBlock("Platform_3", env.transform, new Vector3(8f, 6f, 0), new Vector3(5f, 0.6f, 1f), 8);
            CreateBlock("Platform_4", env.transform, new Vector3(16f, 3f, 0), new Vector3(5f, 0.6f, 1f), 8);
            CreateBlock("Platform_5", env.transform, new Vector3(-16f, 5f, 0), new Vector3(5f, 0.6f, 1f), 8);
            
            // T11-T13: 蹲下测试区域 (低矮通道)
            // Platform_1 顶面 = 0 + PLATFORM_THICKNESS/2 = 0.3
            // 通道净空 = CROUCH_TUNNEL_CLEARANCE (1.4) → 蹲下可过，站立不可
            float platform1Top = 0f + PLATFORM_THICKNESS * 0.5f;
            float ceilingBottom = platform1Top + CROUCH_TUNNEL_CLEARANCE;
            float ceilingCenterY = ceilingBottom + PLATFORM_THICKNESS * 0.5f;
            CreateBlock("LowCeiling", env.transform, new Vector3(-8f, ceilingCenterY, 0), new Vector3(4f, PLATFORM_THICKNESS, 1f), 8);
            
            // T10: 深渊死亡区域 — 覆盖整个地图底部的 Trigger
            GameObject deathZone = new GameObject("DeathZone_Abyss");
            deathZone.transform.SetParent(env.transform);
            deathZone.transform.position = new Vector3(0, -10f, 0);
            var dzColl = deathZone.AddComponent<BoxCollider2D>();
            dzColl.size = new Vector2(80f, 4f);
            dzColl.isTrigger = true;
            var dzTrigger = deathZone.AddComponent<DeathZoneTrigger>();
            var dzSo = new SerializedObject(dzTrigger);
            dzSo.FindProperty("damage").floatValue = 20f;
            dzSo.FindProperty("leftEdgeOffsetX").floatValue = -5f;
            dzSo.FindProperty("rightEdgeOffsetX").floatValue = 5f;
            dzSo.FindProperty("respawnY").floatValue = -0.95f;
            dzSo.ApplyModifiedProperties();

            // 2. Player Setup
            GameObject playerGo = new GameObject("Player_Test");
            playerGo.transform.position = new Vector3(-4, 0, 0);
            
            var rb = playerGo.AddComponent<Rigidbody2D>();
            var coll = playerGo.AddComponent<BoxCollider2D>();
            var stats = playerGo.AddComponent<CharacterStats>();
            var physics = playerGo.AddComponent<PhysicsController>();
            var ctx = playerGo.AddComponent<CharacterContext>();
            var input = playerGo.AddComponent<PlayerInputHandler>();
            var controller = playerGo.AddComponent<PlayerController>();

            // Rigidbody 配置
            rb.isKinematic = true;
            rb.useFullKinematicContacts = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // BoxCollider 尺寸 (OrthoSize=8 → 视野 16 单位 → 角色 2.0 = 1/8 屏幕高度)
            coll.size = new Vector2(0.8f, 2.0f);

            // 2.5 Visuals Setup (Tofu Placeholder)
            GameObject visualsGo = new GameObject("Visuals");
            visualsGo.transform.SetParent(playerGo.transform);
            visualsGo.transform.localPosition = Vector3.zero;
            
            var spriteRenderer = visualsGo.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateTofuSprite(coll.size);

            // 3. Movement Data 配置
            CharacterMovementData moveData = CreateOrLoadMovementData();
            
            // 使用 SerializedObject 注入私有字段
            var ctxSo = new SerializedObject(ctx);
            ctxSo.FindProperty("_moveData").objectReferenceValue = moveData;
            ctxSo.ApplyModifiedProperties();

            // 4. Physics Controller 配置
            var physSo = new SerializedObject(physics);
            physSo.FindProperty("groundLayer").intValue = 1 << 8;
            physSo.FindProperty("wallLayer").intValue = 1 << 9;
            
            // 创建检测锚点 (碰撞体 0.8×2.0)
            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerGo.transform);
            groundCheck.transform.localPosition = new Vector3(0, -1.0f, 0); // 碰撞体底部
            physSo.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
            physSo.FindProperty("groundCheckSize").vector2Value = new Vector2(0.6f, 0.1f);

            GameObject wallCheck = new GameObject("WallCheck");
            wallCheck.transform.SetParent(playerGo.transform);
            wallCheck.transform.localPosition = new Vector3(0.4f, 0, 0); // 碰撞体侧边
            physSo.FindProperty("wallCheckPoint").objectReferenceValue = wallCheck.transform;
            physSo.FindProperty("wallCheckSize").vector2Value = new Vector2(0.1f, 0.6f);

            GameObject ledgeCheck = new GameObject("LedgeCheck");
            ledgeCheck.transform.SetParent(playerGo.transform);
            ledgeCheck.transform.localPosition = new Vector3(0.4f, 1.0f, 0); // 碰撞体顶部
            physSo.FindProperty("ledgeCheckPoint").objectReferenceValue = ledgeCheck.transform;
            
            physSo.ApplyModifiedProperties();

            // 居中相机 (OrthoSize = 8, 视野高度 = 16 单位)
            var cam = Object.FindObjectOfType<Camera>();
            if(cam != null)
            {
                cam.transform.position = new Vector3(0, 3, -10);
                cam.orthographicSize = 8f;
            }

            // 5. 注入 FileLogger
            GameObject loggerGo = new GameObject("System_Logger");
            var fileLogger = loggerGo.AddComponent<PanCake.Metroidvania.Utils.FileLogger>();
            var loggerSo = new SerializedObject(fileLogger);
            loggerSo.FindProperty("logFileName").stringValue = "P0A_Locomotion_Log";
            loggerSo.FindProperty("includeStackTrace").boolValue = true; // 开启堆栈以便分析报错
            loggerSo.ApplyModifiedProperties();

            PanCake.Metroidvania.Utils.DebugLogger.Log("TestSetup", "✅ [P0-A] Locomotion Test Scene created! Layers 8 (Ground) and 9 (Wall) must be defined in Unity Tags & Layers.", true);
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 pos, Vector3 scale, int layer)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = pos;
            block.transform.localScale = scale;
            block.layer = layer; // 确保 Unity 项目里有这层
            
            // 换成 2D 碰撞体
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.AddComponent<BoxCollider2D>();
            
            return block;
        }

        private static CharacterMovementData CreateOrLoadMovementData()
        {
            string folderPath = "Assets/Settings";
            string path = $"{folderPath}/PlayerMovementData.asset";
            
            var data = AssetDatabase.LoadAssetAtPath<CharacterMovementData>(path);
            
            if (data == null)
            {
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }
                data = ScriptableObject.CreateInstance<CharacterMovementData>();
                
                // 设置一些好用的默认值
                data.moveSpeed = 12f;
                data.jumpHeight = 4f;
                data.timeToJumpApex = 0.35f;
                data.dashSpeed = 25f;
                data.dashDuration = 0.15f;
                data.coyoteTime = 0.15f;
                
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
            }
            
            return data;
        }

        // ========== 自动层级生成工具 ==========
        
        /// <summary>
        /// 生成一个带绿色边框的白色方块 Sprite (生化危机 Tofu 风格)
        /// </summary>
        private static Sprite CreateTofuSprite(Vector2 size)
        {
            int pixelsPerUnit = 32;
            int width = Mathf.RoundToInt(size.x * pixelsPerUnit);
            int height = Mathf.RoundToInt(size.y * pixelsPerUnit);

            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            Color fillColor = new Color(0.9f, 0.9f, 0.9f, 1f); // 灰白色
            Color borderColor = Color.green;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 边缘 2个像素画绿边，其余填白
                    bool isBorder = x < 2 || x >= width - 2 || y < 2 || y >= height - 2;
                    pixels[y * width + x] = isBorder ? borderColor : fillColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        /// <summary>
        /// 检查并在 TagManager 中自动生成对应的 Layer
        /// </summary>
        private static void EnsureLayerExists(int layerIndex, string layerName)
        {
            if (layerIndex < 8 || layerIndex > 31)
            {
                PanCake.Metroidvania.Utils.DebugLogger.LogError("TestSetup", "Layer index must be between 8 and 31.");
                return;
            }

            // 加载 Unity 内部的 TagManager 资产
            Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(asset[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            if (layers == null || !layers.isArray) return;

            SerializedProperty layerSP = layers.GetArrayElementAtIndex(layerIndex);
            
            // 如果该层为空，填入我们的名字
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                PanCake.Metroidvania.Utils.DebugLogger.Log("TestSetup", $"✅ Auto-created Layer {layerIndex}: '{layerName}' in TagManager.", true);
            }
            else if (layerSP.stringValue != layerName)
            {
                PanCake.Metroidvania.Utils.DebugLogger.LogWarning("TestSetup", $"⚠️ Layer {layerIndex} is already occupied by '{layerSP.stringValue}'. " +
                                 $"The test scene expects it to be '{layerName}'. Physics might behave unexpectedly.", true);
            }
        }
    }
}
#endif
