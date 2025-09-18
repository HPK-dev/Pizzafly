# 3D披薩麵團物理模擬系統 - 最終實現方案

## 一、系統概述

### 1.1 核心需求總結
- **多麵團支援**：同時處理最多30個麵團實例
- **延展機制**：每個區域有獨立延展進度(0-1)，完成區域基本不再變形
- **自然成形**：透過局部操作自然形成圓形，無需強制圓形偏移
- **性能目標**：維持60fps，使用LOD系統優化

### 1.2 技術架構選擇

基於需求分析，採用**均勻網格變形方案**（Plan2的基礎架構）而非徑向格子系統，原因：
- 更適合批量處理多個麵團（記憶體局部性更好）
- LOD實現更直接（簡單的網格細分）
- 延展進度追蹤更精確（每個頂點獨立）

## 二、資料結構設計

### 2.1 核心頂點結構

```
DoughVertex {
    position: Vector3          // 當前世界座標
    restPosition: Vector3      // 初始位置（計算變形量）
    velocity: Vector3          // 速度（用於動態響應）
    stretchProgress: float     // 延展進度 [0,1]
    thickness: float          // 局部厚度
    temperature: float        // 預留：溫度值
    isLocked: bool           // 延展完成後的鎖定標記
    gridIndex: int2          // 在網格中的索引位置
}
```

### 2.2 麵團實例結構

```
DoughInstance {
    vertices: DoughVertex[]      // 頂點陣列
    triangles: int[]            // 三角形索引
    bounds: Bounds              // 邊界框
    lodLevel: int              // 當前LOD級別
    averageProgress: float      // 平均延展進度
    spatialHash: SpatialHash    // 空間雜湊表（用於碰撞查詢）
}
```

### 2.3 批處理管理器

```
DoughBatchManager {
    instances: DoughInstance[]   // 所有麵團實例
    activeLODs: int[]           // 每個實例的活動LOD
    visibilityMask: BitArray    // 可見性遮罩
    updateQueue: PriorityQueue  // 更新優先佇列
}
```

## 三、變形演算法

### 3.1 統一變形框架

採用基於影響力場的變形模型，支援多種工具類型：

```
通用變形演算法：

輸入: toolPosition, toolShape, toolParameters
輸出: 更新的頂點位置和狀態

對每個受影響頂點 v:
    1. 計算工具影響力:
       influence = ComputeInfluence(v, tool)
       
    2. 檢查延展鎖定:
       if (v.stretchProgress >= 0.95):
           v.isLocked = true
           influence *= 0.1  // 大幅降低但不完全消除影響
       
    3. 更新延展進度:
       if (!v.isLocked):
           progressDelta = influence * tool.strength * deltaTime
           v.stretchProgress = min(v.stretchProgress + progressDelta, 1.0)
       
    4. 計算位移:
       displacement = ComputeDisplacement(v, tool, influence)
       displacement *= (1 - v.stretchProgress * 0.9)  // 延展越多，位移越小
       
    5. 應用變形:
       v.position += displacement
       v.thickness *= (1 - influence * thicknessReduction * deltaTime)
```

### 3.3 單一工具交互模式

系統同時只處理一種變形工具，簡化計算：

```
工具切換狀態機:
    enum ToolMode {
        NONE,           // 無工具
        DUAL_HAND,      // 雙手合併成橢圓
        ROLLING_PIN     // 擀麵杖
    }
    
    當切換工具時:
        1. 清除當前工具的速度緩存
        2. 重置影響力場
        3. 更新碰撞體形狀
```

#### 按壓工具（支援橢圓形）
```
影響力計算（橢圓形支援）:
    // 將點變換到橢圓局部空間
    localPos = InverseTransform(v.position, tool.transform)
    normalizedDist = sqrt(localPos.x²/a² + localPos.z²/b²)  // a,b為橢圓半軸
    
    if (normalizedDist < 1.0):
        rawInfluence = 1 - normalizedDist
        influence = smoothstep(0, 1, rawInfluence)
    else:
        influence = 0

位移計算:
    // 使用橢圓法線方向
    ellipseNormal = ComputeEllipseNormal(localPos, a, b)
    worldNormal = TransformDirection(ellipseNormal, tool.transform)
    
    displacement.xz = worldNormal.xz * influence * expansionRate
    displacement.y = -influence * compressionAmount
```

#### 擀麵杖（圓柱形）
```
影響力計算:
    closestPoint = ProjectPointOnLine(v.position, pin.start, pin.end)
    distance = ||v.position - closestPoint||
    
    if (distance < pin.radius):
        influence = smoothstep(1, 0, distance / pin.radius)
        
        // 方向性調製
        dirDot = dot(normalize(v.position - closestPoint), rollDirection)
        influence *= smoothstep(-0.5, 1.0, dirDot)

位移計算:
    displacement = rollDirection * influence * stretchSpeed
    displacement += lateralDir * influence * lateralStretch * 0.3
    displacement.y = -influence * flattenAmount
```

## 四、約束系統（簡化版）

### 4.1 距離約束

使用Position Based Dynamics (PBD)方法，但降低迭代次數以支援多麵團：

```
距離約束求解（每幀1-2次迭代）:
    對每條邊 (i, j):
        if (vertices[i].isLocked && vertices[j].isLocked):
            continue  // 兩端都鎖定，跳過
            
        currentDist = ||pi - pj||
        error = currentDist - restLength
        
        if (|error| > tolerance):
            correction = normalize(pi - pj) * error * 0.5
            
            // 考慮鎖定權重
            wi = vertices[i].isLocked ? 0.1 : 1.0
            wj = vertices[j].isLocked ? 0.1 : 1.0
            
            totalWeight = wi + wj
            pi -= correction * (wi / totalWeight) * stiffness
            pj += correction * (wj / totalWeight) * stiffness
```

參考：[Position Based Dynamics](http://matthias-mueller-fischer.ch/publications/posBasedDyn.pdf)

### 4.2 體積保持（簡化）

採用快速近似方法而非精確計算：

```
快速體積補償:
    1. 計算平均厚度變化率
    2. 如果總體積偏差 > 10%:
       對所有頂點統一調整厚度
```

### 4.3 拉普拉斯平滑（選擇性）

只對變形邊界區域應用，減少計算量：

```
選擇性平滑:
    對變形邊界頂點（0.2 < stretchProgress < 0.8）:
        smoothedPos = 0.7 * currentPos + 0.3 * neighborsAverage
        保持y坐標不變
```

## 五、多人遊戲LOD系統設計

### 5.1 LOD級別定義（多人版本）

```
LOD配置:
    LOD0: 50×50 頂點（完整物理 + 完整同步）
        - 正在被任何玩家操作的麵團
        - 所有玩家5m內的麵團
        
    LOD1: 25×25 頂點（簡化物理 + 壓縮同步）
        - 5-15m範圍內的麵團
        - 最近被操作過的麵團（5秒內）
        
    LOD2: 12×12 頂點（最小物理 + 關鍵點同步）
        - 15m以外的麵團
        - 只同步形狀輪廓和平均進度
        
    LOD3: 靜態快照（無物理 + 快照同步）
        - 超過30m或不可見的麵團
        - 每5秒同步一次狀態快照
```

### 5.2 網路同步策略

```
同步資料結構:
DoughNetworkData {
    // 始終同步（所有LOD）
    networkId: uint
    position: Vector3
    rotation: Quaternion
    averageProgress: float
    
    // LOD0 額外資料（~2KB）
    vertexPositions: CompressedVector3[]  // 量化到16位
    stretchProgress: byte[]  // 量化到8位
    
    // LOD1 額外資料（~500B）
    keyVertices: CompressedVector3[25×25]  // 降採樣
    regionProgress: byte[5×5]  // 區域平均
    
    // LOD2 額外資料（~150B）
    boundingPoints: Vector3[12]  // 輪廓點
    quadrantProgress: byte[4]  // 四象限進度
}

同步頻率:
    LOD0: 10Hz（被操作時提升至20Hz）
    LOD1: 5Hz
    LOD2: 2Hz
    LOD3: 0.2Hz
```

### 5.3 權威性分配

```
網路權威模型（使用Netcode for GameObjects）:

1. 操作權轉移:
   當玩家開始操作麵團:
       RequestOwnership(doughId)
       if (granted):
           本地LOD = LOD0
           開始本地物理模擬
           提高同步頻率
   
2. 權威性層級:
   - 主控玩家: 完整物理模擬（LOD0）
   - 其他玩家: 根據距離接收不同LOD資料
   
3. 權威釋放:
   操作結束後3秒:
       ReleaseOwnership()
       降低同步頻率
       其他玩家可請求權威
```

### 5.4 插值與預測

```
客戶端處理:
對於非權威麵團:
    if (LOD == 0):
        // 接收完整頂點資料，進行插值
        foreach vertex:
            smoothPosition = Lerp(lastReceived, newReceived, t)
            
    elif (LOD == 1):
        // 接收關鍵點，其他頂點插值
        UpdateKeyVertices(received)
        InterpolateIntermediateVertices()
        
    elif (LOD == 2):
        // 接收輪廓，生成近似形狀
        UpdateBoundingShape(received)
        GenerateApproximateMesh()
        
    elif (LOD == 3):
        // 使用快照，無插值
        DisplayStaticSnapshot()
```

## 六、批處理優化

### 6.1 空間劃分策略

使用混合方法優化查詢：

```
空間查詢系統:
    全域八叉樹: 快速定位相關麵團
    局部網格: 每個麵團內部的頂點查詢
    
查詢流程:
    1. 使用八叉樹找到可能相交的麵團
    2. 對每個相關麵團，使用其局部網格精確查詢
```

### 6.2 並行計算架構

使用Unity Job System + Burst Compiler：

```csharp
// Job定義偽代碼
ParallelDeformationJob {
    輸入: 工具參數陣列, deltaTime
    輸出: 頂點位置/狀態更新
    
    Execute(int doughIndex):
        if (LOD[doughIndex] > LOD1):
            return  // 低LOD不執行物理
            
        對該麵團的所有頂點:
            執行變形計算
            更新延展進度
}

約束求解Job {
    分批處理，每批處理8-10個麵團
}
```

### 6.4 網路壓縮優化

```
頂點壓縮方案:
1. 位置量化:
   // 原始: Vector3 (12 bytes)
   // 壓縮: 3×uint16 (6 bytes)
   CompressPosition(Vector3 pos):
       x_compressed = (pos.x - bounds.min.x) / bounds.size.x * 65535
       y_compressed = (pos.y - bounds.min.y) / bounds.size.y * 65535
       z_compressed = (pos.z - bounds.min.z) / bounds.size.z * 65535
       return uint16[3]{x_compressed, y_compressed, z_compressed}

2. 進度壓縮:
   // 原始: float (4 bytes)
   // 壓縮: byte (1 byte)
   progress_byte = (byte)(stretchProgress * 255)

3. 差分編碼（用於連續幀）:
   只傳送變化的頂點索引和增量
   DeltaFrame {
       changedVertices: List<(index, deltaPos, deltaProgress)>
   }

4. 區域聚合（LOD1-2）:
   將NxN頂點聚合為單一資料點
   傳送平均位置和進度
```

### 6.5 網路流量預估

```
流量計算（每個麵團）:
LOD0（操作中）: 
    - 50×50頂點 × 7bytes（位置+進度） = 17.5KB
    - 20Hz更新 = 350KB/s
    - 使用差分編碼後: ~50KB/s

LOD1（觀察中）:
    - 25×25頂點 × 7bytes = 4.4KB
    - 5Hz更新 = 22KB/s
    - 使用區域聚合: ~10KB/s

LOD2（遠距離）:
    - 12個輪廓點 × 6bytes = 72B
    - 2Hz更新 = 144B/s

總流量（最壞情況）:
    - 1個LOD0 + 5個LOD1 + 24個LOD2 ≈ 100KB/s per client
```

## 七、渲染優化

### 7.1 網格實例化

```
渲染策略:
    使用GPU Instancing渲染相同LOD的麵團
    材質屬性透過StructuredBuffer傳遞
    
    MaterialPropertyBlock設定:
        _StretchProgressBuffer: 每頂點延展進度
        _ThicknessBuffer: 每頂點厚度
```

### 7.2 視覺化著色器

```hlsl
// Shader偽代碼
頂點著色器:
    傳遞stretchProgress、thickness和vertexNormal到片段
    計算凹陷深度: depression = 1.0 - position.y / restPosition.y

片段著色器:
    // 基礎顏色映射
    if (stretchProgress < 0.3):
        color = lerp(rawDoughColor, workingColor, stretchProgress/0.3)
    elif (stretchProgress < 0.9):
        color = workingColor
    else:
        color = lerp(workingColor, finishedColor, (stretchProgress-0.9)/0.1)
    
    // 凹陷邊緣陰影
    if (depression > 0.1):
        // 計算邊緣梯度（基於鄰近頂點的高度差）
        edgeGradient = ComputeHeightGradient(worldPos)
        shadowIntensity = saturate(edgeGradient * 2.0) * depression
        color *= (1.0 - shadowIntensity * 0.4)  // 最多變暗40%
    
    // 環境遮蔽增強
    ao = 1.0 - (depression * 0.3)
    color *= ao
```

## 八、參數配置建議

### 8.1 核心參數範圍

```
物理參數:
    stiffness: 0.3-0.5 (降低以支援多實例)
    damping: 0.92-0.95
    constraintIterations: 1-2 (效能優先)
    
工具參數:
    pressRadius: 0.15-0.25 (相對於麵團半徑)
    pressStrength: 1.0-2.0
    rollSpeed: 0.8-1.2
    
LOD閾值:
    highImportance: 10.0
    mediumImportance: 3.0
    lowImportance: 1.0
```

### 8.2 性能目標分配

```
每幀時間預算(16.67ms @ 60fps):
    物理模擬: 6-8ms
    約束求解: 2-3ms
    網格更新: 2-3ms
    渲染: 4-5ms
    其他: 1-2ms
```

## 九、實現步驟

- [ ] 階段一：單麵團原型
  1. 實現基礎網格生成和變形
  2. 完成延展進度追蹤
  3. 實現基本視覺化
- [ ] 階段二：物理完善
  1. 加入簡化的距離約束
  2. 實現延展鎖定機制
  3. 調整參數達到良好手感
- [ ] 階段三：多實例支援
  1. 實現批處理管理器
  2. 整合Job System
  3. 加入LOD系統
- [ ] 階段四：優化
  1. 記憶體佈局優化
  2. GPU實例化渲染
  3. 性能調優
- [ ] 階段五：打磨
  1. 參數微調
  2. 邊緣情況處理
  3. 視覺效果增強

## 十、關鍵實現參考

1. **Verlet Integration**: [Wikipedia - Verlet Integration](https://en.wikipedia.org/wiki/Verlet_integration)
2. **Position Based Dynamics**: [Müller et al. 2007](http://matthias-mueller-fischer.ch/publications/posBasedDyn.pdf)
3. **Spatial Hashing**: [Optimized Spatial Hashing for Collision Detection](https://matthias-research.github.io/pages/publications/tetraederCollision.pdf)
4. **Unity Job System**: [Unity Documentation](https://docs.unity3d.com/Manual/JobSystem.html)

## 十一、注意事項

### 關鍵風險點
1. **批量約束求解穩定性**：降低迭代次數可能導致抖動
2. **LOD切換視覺跳變**：需要平滑過渡機制
3. **記憶體占用**：30個麵團可能需要約30-50MB

### 解決策略
1. 使用時間步長補償（sub-stepping）提高穩定性
2. LOD切換時使用漸變混合（1-2幀過渡）
3. 實現麵團物件池，避免頻繁分配

## 十二、麵團間碰撞處理

### 12.1 碰撞檢測策略

```
兩階段碰撞檢測:
    1. 粗糙階段（AABB）:
       對每對麵團(i,j):
           if (!Intersects(bounds[i], bounds[j])):
               continue  // 跳過不相交的對
    
    2. 精細階段（僅對LOD0和LOD1）:
       使用空間雜湊快速找到潛在碰撞頂點對
       對每對潛在碰撞頂點:
           檢測並解決穿透
```

### 12.2 碰撞響應

```
軟體碰撞響應（基於罰力法）:
    對碰撞頂點對(v1, v2):
        penetrationDepth = (r1 + r2) - distance(v1, v2)
        
        if (penetrationDepth > 0):
            // 計算分離方向
            normal = normalize(v2.pos - v1.pos)
            
            // 考慮延展鎖定的質量權重
            m1 = v1.isLocked ? 10.0 : 1.0
            m2 = v2.isLocked ? 10.0 : 1.0
            
            // 分配位移
            totalMass = m1 + m2
            v1.pos -= normal * penetrationDepth * (m2/totalMass) * 0.5
            v2.pos += normal * penetrationDepth * (m1/totalMass) * 0.5
            
            // 速度阻尼
            v1.velocity *= 0.9
            v2.velocity *= 0.9
```

### 12.3 碰撞優化

```
優化策略:
    1. 只對高LOD麵團進行精確碰撞
    2. 使用時間相干性緩存潛在碰撞對
    3. 每幀限制最大碰撞對數（如50對）
    4. 優先處理穿透深度較大的碰撞
```

參考：[Spatial Hashing for Soft Bodies](https://matthias-research.github.io/pages/publications/tetraederCollision.pdf)