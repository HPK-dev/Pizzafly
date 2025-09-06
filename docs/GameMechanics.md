# 🛠️ Pizzafly 遊戲機制詳解

本文檔詳細說明 Pizzafly 的各項遊戲機制，為開發團隊提供技術實現指南。

## 目錄

1. [核心系統架構](#核心系統架構)
2. [經營系統](#經營系統)
3. [物理互動系統](#物理互動系統)
4. [配料系統](#配料系統)
5. [角色與AI系統](#角色與ai系統)
6. [事件系統](#事件系統)
7. [多人網路系統](#多人網路系統)
8. [UI/UX系統](#uiux系統)
9. [進度與存檔系統](#進度與存檔系統)
10. [性能優化建議](#性能優化建議)

---

## 核心系統架構

### 主要模組
- **GameManager** - 遊戲狀態管理
- **RestaurantManager** - 餐廳經營邏輯
- **PhysicsManager** - 物理系統控制
- **OrderSystem** - 訂單管理
- **EventSystem** - 事件觸發與處理
- **NetworkManager** - 多人同步
- **UIManager** - 介面管理

### 資料流向
```
顧客點餐 → 訂單系統 → 廚房系統 → 物理互動 → 完成訂單 → 評分系統 → 經營數據更新
```

---

## 經營系統

### 💰 財務管理
```csharp
public class FinancialSystem 
{
    public float CurrentMoney { get; set; }
    public float DailyRevenue { get; set; }
    public float DailyCosts { get; set; }
    
    // 成本計算
    public void CalculateCosts()
    {
        // 食材成本
        // 員工薪資
        // 設備維護
        // 租金
    }
}
```

### 📱 智慧手機系統
- **評論系統**：顧客滿意度 → 評分 → 線上評論
- **新聞系統**：隨機事件影響市場
- **行情系統**：食材價格波動（供需模型）

### 🌟 聲望與評級
```csharp
public enum CustomerSatisfaction 
{
    Terrible = 1,    // 😡
    Poor = 2,        // 😞 
    Average = 3,     // 😐
    Good = 4,        // 😊
    Excellent = 5    // 🤩
}
```

---

## 物理互動系統

### 🌀 物理效果實現

#### 重力翻轉器
```csharp
public class GravityController 
{
    public Vector3 gravityDirection = Vector3.down;
    
    public void FlipGravity()
    {
        gravityDirection = -gravityDirection;
        Physics.gravity = gravityDirection * 9.81f;
    }
}
```

#### 磁性披薩盤
```csharp
public class MagneticPlate : MonoBehaviour 
{
    public float magneticForce = 10f;
    public LayerMask metalObjects;
    
    void Update()
    {
        AttractMetalObjects();
    }
    
    void AttractMetalObjects()
    {
        Collider[] metals = Physics.OverlapSphere(transform.position, magneticForce);
        foreach(var metal in metals)
        {
            if(metal.CompareTag("Metal"))
            {
                Vector3 direction = (transform.position - metal.transform.position).normalized;
                metal.attachedRigidbody?.AddForce(direction * magneticForce);
            }
        }
    }
}
```

#### 時空傳送門
```csharp
public class Portal : MonoBehaviour 
{
    public Transform destination;
    public float teleportCooldown = 2f;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Pizza") || other.CompareTag("Ingredient"))
        {
            TeleportObject(other.gameObject);
        }
    }
    
    void TeleportObject(GameObject obj)
    {
        // 特效
        // 隨機目標（可能是馬桶）
        // 傳送邏輯
    }
}
```

---

## 配料系統

### 🧂 配料分類與屬性
```csharp
public enum IngredientType 
{
    Normal,      // 普通配料
    Living,      // 活體配料
    Emotional,   // 情緒配料  
    Dimensional, // 4D配料
    Sonic        // 聲音配料
}

public class Ingredient : MonoBehaviour 
{
    public IngredientType type;
    public string ingredientName;
    public float cost;
    public float satisfactionModifier;
    public List<SpecialEffect> effects;
    
    // 活體配料行為
    public void SimulateLivingBehavior() 
    {
        if(type == IngredientType.Living)
        {
            // 魚游泳動畫
            // 蔬菜逃跑邏輯
        }
    }
}
```

### 🎨 配料組合系統
```csharp
public class PizzaRecipe 
{
    public List<Ingredient> ingredients;
    public string recipeName;
    public bool isSecretRecipe;
    
    public float CalculateSatisfaction()
    {
        float baseSatisfaction = 1f;
        float combinationBonus = CalculateCombinationBonus();
        return baseSatisfaction * combinationBonus;
    }
    
    float CalculateCombinationBonus()
    {
        // 檢查特殊組合
        // 例如：恐龍肉 + 時光配料 = 史前體驗
        return 1f;
    }
}
```

---

## 角色與AI系統

### 🧑‍🍳 員工AI行為樹
```
員工行為根節點
├── 檢查任務優先級
├── 移動到工作站
├── 執行任務
│   ├── 製作披薩
│   ├── 清理廚房
│   └── 服務顧客
└── 處理特殊事件
```

### 動物員工特殊行為
```csharp
public class AnimalEmployee : Employee 
{
    public AnimalType animalType;
    
    public override void PerformTask()
    {
        switch(animalType)
        {
            case AnimalType.Dolphin:
                // 需要水池環境檢查
                if(!IsInWater()) return;
                break;
            case AnimalType.Monkey:
                // 偷吃機率計算
                if(Random.Range(0f, 1f) < 0.1f) StealFood();
                break;
            case AnimalType.Parrot:
                // 爆料顧客隱私
                if(Random.Range(0f, 1f) < 0.05f) RevealCustomerSecrets();
                break;
        }
        base.PerformTask();
    }
}
```

### 🧍 特殊顾客AI
```csharp
public class SpecialCustomer : Customer 
{
    public SpecialCustomerType type;
    
    public override void PlaceOrder()
    {
        switch(type)
        {
            case SpecialCustomerType.TimeTravel:
                // 點史前配料
                OrderPrehistoricIngredients();
                break;
            case SpecialCustomerType.Invisible:
                // 只能通過聲音識別
                EnableAudioOnlyMode();
                break;
            case SpecialCustomerType.Shadow:
                // 需要特定燈光
                RequireLightingAdjustment();
                break;
        }
    }
}
```

---

## 事件系統

### 🌀 事件觸發機制
```csharp
public class EventManager : MonoBehaviour 
{
    public List<GameEvent> availableEvents;
    public List<GameEvent> activeEvents;
    
    void Update()
    {
        CheckEventTriggers();
        UpdateActiveEvents();
    }
    
    void CheckEventTriggers()
    {
        foreach(var gameEvent in availableEvents)
        {
            if(gameEvent.ShouldTrigger())
            {
                TriggerEvent(gameEvent);
            }
        }
    }
}
```

### 事件類型實現
```csharp
public class PizzaRainEvent : GameEvent 
{
    public override void Execute()
    {
        // 生成從天而降的披薩
        for(int i = 0; i < 50; i++)
        {
            Vector3 randomPos = GetRandomSkyPosition();
            GameObject pizza = Instantiate(pizzaPrefab, randomPos, Quaternion.identity);
            pizza.GetComponent<Rigidbody>().AddForce(Vector3.down * 10f);
        }
    }
}

public class GravityAnomalyEvent : GameEvent 
{
    public override void Execute()
    {
        // 局部重力異常
        Collider[] affectedObjects = Physics.OverlapSphere(epicenter, radius);
        foreach(var obj in affectedObjects)
        {
            if(obj.GetComponent<Rigidbody>())
            {
                obj.GetComponent<Rigidbody>().useGravity = false;
                // 添加漂浮效果
            }
        }
    }
}
```

### 🔗 事件連鎖反應
```csharp
public class ChainEventSystem 
{
    public Dictionary<string, List<string>> eventChains;
    
    public void InitializeChains()
    {
        // 披薩雨 + 重力異常 = 顧客飛天抓披薩
        eventChains["PizzaRain_GravityAnomaly"] = new List<string> 
        { 
            "CustomersFlyingToCatchPizza" 
        };
    }
}
```

---

## 多人網路系統

### 🌐 網路架構
```csharp
public class NetworkManager : MonoBehaviourPunPV 
{
    // 使用 Photon PUN 2 或類似解決方案
    
    [PunRPC]
    void SyncPizzaState(int pizzaID, Vector3 position, Vector3 rotation)
    {
        // 同步披薩狀態
    }
    
    [PunRPC]
    void TriggerChaosEvent(string eventName, float[] parameters)
    {
        // 同步混亂事件
    }
}
```

### 競爭模式實現
```csharp
public class CompetitiveMode : MonoBehaviour 
{
    public void ExecuteSabotage(SabotageType type, int targetPlayerID)
    {
        switch(type)
        {
            case SabotageType.HackOrders:
                // 竄改對手訂單
                break;
            case SabotageType.FakeIngredients:
                // 送過期食材
                break;
            case SabotageType.TrafficJam:
                // 堵住對手門口
                break;
        }
    }
}
```

---

## UI/UX系統

### 📱 智慧手機介面
```csharp
public class SmartphoneUI : MonoBehaviour 
{
    public GameObject reviewsPanel;
    public GameObject newsPanel;
    public GameObject marketPanel;
    
    public void ShowReviews()
    {
        // 顯示顧客評論與評分
        UpdateReviewsList();
    }
    
    public void ShowMarketPrices()
    {
        // 顯示食材市價波動圖表
        UpdatePriceChart();
    }
}
```

### 🎮 第一人稱控制
```csharp
public class FirstPersonController : MonoBehaviour 
{
    public float mouseSensitivity = 2f;
    public float moveSpeed = 5f;
    public LayerMask interactableLayer;
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }
    
    void HandleInteraction()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 3f, interactableLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                interactable?.Interact();
            }
        }
    }
}
```

---

## 進度與存檔系統

### 💾 存檔資料結構
```csharp
[System.Serializable]
public class SaveData 
{
    public float currentMoney;
    public List<string> unlockedIngredients;
    public List<string> completedMissions;
    public Dictionary<string, int> employeeStats;
    public List<string> unlockedTitles;
    public RestaurantUpgrades upgrades;
    
    // 序列化為 JSON
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
}
```

### 🏆 成就系統
```csharp
public class AchievementSystem 
{
    public List<Achievement> achievements;
    
    public void CheckAchievements(GameAction action)
    {
        foreach(var achievement in achievements)
        {
            if(achievement.CheckCondition(action))
            {
                UnlockAchievement(achievement);
            }
        }
    }
}
```

---

## 性能優化建議

### 🚀 物理優化
- 使用 **物理群組** 減少不必要的碰撞檢測
- **LOD系統** 根據距離調整物理精度
- **對象池** 管理經常生成/銷毀的披薩和配料

### 🎨 渲染優化
- **批次渲染** 相同材質的配料
- **遮擋剔除** 隱藏不可見的廚房設備
- **動態載入** 根據需求載入資源

### 📡 網路優化
- **狀態壓縮** 只同步關鍵變化
- **預測演算法** 減少網路延遲影響
- **頻率控制** 根據重要性調整同步頻率

### 💾 記憶體優化
```csharp
public class MemoryManager 
{
    public void OptimizeMemory()
    {
        // 清理未使用的配料
        Resources.UnloadUnusedAssets();
        
        // 垃圾回收
        System.GC.Collect();
        
        // 壓縮材質
        CompressTextures();
    }
}
```

---

## 開發階段建議

### Phase 1: 核心系統
1. 基礎經營系統
2. 簡單物理互動
3. 基本UI框架

### Phase 2: 內容擴充
1. 配料系統完善
2. 特殊顧客AI
3. 事件系統

### Phase 3: 多人與優化
1. 網路同步
2. 競爭模式
3. 性能優化

### Phase 4: 打磨與發布
1. 平衡調整
2. Bug修復
3. 最終優化

---

**這份文檔將隨開發進度持續更新，請定期檢查最新版本！** 🛠️✨