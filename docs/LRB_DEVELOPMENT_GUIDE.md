# 2025GuoMei Lrb_Assets 开发规范指南

版本: v1.0  
适用范围: `Assets/Lrb_Assets` 主责模块  
文档定位: 开发规范（建议型）  
更新日期: 2026-03-20

**相关文档**：全项目技术栈、目录、模块边界与风险总览见 [国美_项目结构盘点.md](./国美_项目结构盘点.md)（接手/协作速读）。

---

## Part A: 提纲版（章节树）

### 1. 架构总览
- 目标: 明确 Lrb 子系统在项目中的职责边界与主流程位置。
- 边界: 只描述 `Lrb_Assets` 主实现；Fy 侧仅写对接接口，不展开其内部逻辑。
- 关键接口: `EventManager`、`SaveLoadSystem`、`SceneManager`。
- 常见错误: 把 Fy 的 `BigBag` 当作 Lrb 主背包实现。

### 2. InventorySystem 规范
- 目标: 统一背包系统数据模型、UI 生命周期、交互入口。
- 边界: `InventoryController` + `InventorySO` + `ItemDatabaseSO` + UI 子模块。
- 关键接口: `AddItemToInventory`、`OnSaveGame`、`OnLoadGame`、`LoadDataFromSave`。
- 常见错误: 读档后未执行 `itemID -> ItemSO` 重连。

### 3. ClueBoard 规范
- 目标: 统一线索采集、推理链判定、演出状态与存档恢复行为。
- 边界: `ClueBoardController`、`ClueBoardSO`、`UIClueBoard`、Bridge 脚本。
- 关键接口: `TryCollectClue`、`CheckChainCompletion`、`ExportSaveData`、`ImportSaveData`。
- 常见错误: 跳过 `ResetToDefaultState` 导致状态残留。

### 4. PinTu 规范
- 目标: 统一拼图关卡加载、胜利结算、奖励入包、返回主流程。
- 边界: `PuzzleManager`、`PuzzlePiece`、`PuzzleLevelData`。
- 关键接口: `InitializeLevel`、`OnPieceLocked`、`Save`、`OnClickReturnToMainGame`。
- 常见错误: 未经 `SaveLoadSystem.SaveGame()` 直接切场景。

### 5. 共享状态锁规范
- 目标: UI/玩法界面打开期间，统一玩家移动锁管理。
- 边界: `PlayerStateManager` 是唯一移动锁路由层。
- 关键接口: `SetMovementLock(bool, object)`。
- 常见错误: 外部系统直接操作 `PlayerMove` 开关，绕开 `PlayerStateManager`。

### 6. 数据资产规范
- 目标: 保证 `_Data` 层可维护、可读档、可回溯。
- 边界: `_Data/Inventory`、`_Data/ClueBoard`、`_Data/PinTu`。
- 关键接口: `ItemID`、`ClueLink`、`ScriptableObject` 配置引用。
- 常见错误: ID 命名与代码枚举/字符串不一致。

### 7. 跨系统集成规范（Fy 边界）
- 目标: 定义输入/输出/依赖，不覆盖 Fy 内部实现。
- 边界: `Fy_DialogManager -> DialogueClueBridge`，`ChuPuMananger/PuzzleManager -> InventoryController`。
- 关键接口: `EventNameEnum.Save/Load`、`SceneManager.LoadScene(...)`。
- 常见错误: 跨系统直接写对方内部状态而非走公开入口。

### 8. 调试与回归检查
- 目标: 给接手开发者提供最小回归集合。
- 边界: 仅覆盖 Lrb 相关流程。
- 关键接口: 存档读写、道具增删、线索激活、拼图奖励。
- 常见错误: 只测单系统，不测“对话 -> 线索 -> 存档 -> 读档 -> UI 恢复”全链路。

### 9. 第三方插件集成边界
- 目标: 仅说明项目集成方式和注意事项。
- 边界: 不展开插件内部机制。
- 关键接口: Odin Inspector 标注、DOTween 动画调用。
- 常见错误: 将插件用法当作架构依赖强绑定。

---

## Part B: 正文版（开发规范）

## 1. 架构总览

### 推荐做法
- 将 `Assets/Lrb_Assets` 视为一个独立玩法域，按“系统层 + 数据层 + 资源层”维护。
- 系统层固定目录:
  - `Scripts/InventorySystem`
  - `Scripts/ClueBoard`
  - `Scripts/PinTu`
  - `Scripts/PlayerStateManager.cs`
- 数据层固定目录:
  - `Assets/Lrb_Assets/_Data/Inventory`
  - `Assets/Lrb_Assets/_Data/ClueBoard`
  - `Assets/Lrb_Assets/_Data/PinTu`
- 资源层固定目录:
  - `Assets/Lrb_Assets/_Prefabs`
  - `Assets/Lrb_Assets/Animation`
  - `Assets/Lrb_Assets/InkShader`
  - `Assets/Lrb_Assets/Audio`

### 反例风险
- 把系统脚本散落到多目录，后续定位依赖成本高。
- 把 `_Data` 当成临时素材目录，导致配置引用失控。

### 最小实现路径
1. 新功能先归类到三大系统之一（Inventory / ClueBoard / PinTu）。
2. 运行时逻辑放 `Scripts`，可配置参数放 `_Data`。
3. 与 Fy 对接只通过公开接口与事件，不直接写入对方私有状态。

---

## 2. InventorySystem 规范

涉及代码:
- `Assets/Lrb_Assets/Scripts/InventorySystem/InventoryController.cs`
- `Assets/Lrb_Assets/Scripts/InventorySystem/Model/InventorySO.cs`
- `Assets/Lrb_Assets/Scripts/InventorySystem/Model/ItemDatabaseSO.cs`
- `Assets/Lrb_Assets/Scripts/InventorySystem/Interactive/SceneItem.cs`

### 推荐做法
- 统一以 `InventoryController` 作为背包入口，不在外部直接改 `inventoryData.inventoryItems`。
- 存档恢复后必须执行重连:
  - 先覆盖存档列表
  - 再调用 `inventoryData.LoadDataFromSave(itemDatabase)`
  - 最后 `InformAboutChange()` 刷新 UI
- 道具入包建议走公开方法:
  - `AddItemToInventory(ItemID, quantity)`
  - `AddItemToInventory(string id, quantity)`
  - `AddItemToInventory(ItemSO, quantity, itemState)`
- 打开/关闭背包时，统一通过 `PlayerStateManager.SetMovementLock(...)` 管理移动锁。

### 反例风险
- 读档仅恢复 `itemID` 不重连 `ItemSO`，会出现空引用或 UI 无法展示图标。
- 新系统直接操作 UI 层而不改数据层，导致存档与显示不一致。

### 最小实现路径
1. 在 `ItemDatabaseSO` 配置新道具并保证 `itemID` 唯一。
2. 外部仅调用 `InventoryController` 的入包/删包公开方法。
3. 如引入新存档字段，先定义落点，再补 `OnSaveGame/OnLoadGame` 对应逻辑。

---

## 3. ClueBoard 规范

涉及代码:
- `Assets/Lrb_Assets/Scripts/ClueBoard/ClueBoardController.cs`
- `Assets/Lrb_Assets/Scripts/ClueBoard/Model/ClueBoardSO.cs`
- `Assets/Lrb_Assets/Scripts/ClueBoard/BridgeOthers/DialogueClueBridge.cs`
- `Assets/Lrb_Assets/Scripts/ClueBoard/UI/UIClueBoard.cs`

### 推荐做法
- 线索状态变更统一走 `ClueBoardSO`:
  - 收集: `TryCollectClue(clueLink, worldPos)`
  - 阅读: `SetClueAsRead(index)`
  - 存档导出: `ExportSaveData()`
  - 存档导入: `ImportSaveData(savedClues, finishedChains)`
- 推理链判定由数据层负责，演出由控制层负责:
  - 数据判定: `CheckChainCompletion(...)`
  - 演出执行: `ClueBoardController.PlayDeductionSequence(...)`
- `DialogueClueBridge` 只负责“对话事件 -> 线索激活”桥接，不承载 UI 业务流程。

### 反例风险
- 在 UI 层直接改 `isCollected/isRead`，会绕过存档与事件回调。
- 不做导入前重置，旧运行状态会污染新读档结果。

### 最小实现路径
1. 先在 `_Data/ClueBoard` 配 `ClueSO`、`DeductionChainSO`、管理资产。
2. 对话或互动触发时调用 `DialogueClueBridge.OnDialogueEvent(...)`。
3. 验证链完成后静态连线和读档恢复行为一致。

---

## 4. PinTu 规范

涉及代码:
- `Assets/Lrb_Assets/Scripts/PinTu/PuzzleManager.cs`
- `Assets/Lrb_Assets/Scripts/PinTu/PuzzlePiece.cs`
- `Assets/Lrb_Assets/Scripts/PinTu/PuzzleLevelData.cs`

### 推荐做法
- 关卡配置固定放 `PuzzleLevelData`（完整图、切片、目标坐标、吸附阈值）。
- 场景内只保留 `PuzzleManager` 作为流程控制中枢:
  - 初始化关卡: `InitializeLevel()`
  - 完成计数: `OnPieceLocked()`
  - 返回主流程: `OnClickReturnToMainGame()`
- 结算奖励入包通过 `InventoryController.Instance.AddItemToInventory(...)`，并走统一 Save 事件。

### 反例风险
- 拼图结算直接改 `bagData`，会与背包运行态脱节。
- 返回主场景前不保存，导致奖励丢失。

### 最小实现路径
1. 在 `_Data/PinTu` 新建关卡资产并填充碎片配置。
2. 场景入口设置 `PuzzleManager.NextLevelToLoad` 后再切到拼图场景。
3. 完成后触发 Save 并返回 `PersistentScenes`。

---

## 5. 共享状态锁规范（PlayerStateManager）

涉及代码:
- `Assets/Lrb_Assets/Scripts/PlayerStateManager.cs`
- 依赖组件: `PlayerMove`（位于 Fy 侧）

### 推荐做法
- 所有“需要暂停角色控制”的系统统一调用:
  - `SetMovementLock(true, source)` 开锁
  - `SetMovementLock(false, source)` 解锁
- 将 `PlayerStateManager` 作为唯一入口，避免多个系统同时直接改 `PlayerMove.enabled`。

### 反例风险
- 多个系统各自控制 `PlayerMove`，出现锁状态打架，导致角色无法恢复移动。

### 最小实现路径
1. 打开背包/线索板/关键 UI 时加锁。
2. 关闭界面或流程结束时解锁。
3. 所有新玩法禁止直接引用 `PlayerMove` 作为控制入口。

---

## 6. 数据资产规范（_Data）

### 推荐做法
- `Inventory`:
  - 保证 `ItemID`、`ItemSO.itemID`、调用字符串一致。
  - 新道具一律进 `ItemDatabaseSO`。
- `ClueBoard`:
  - `ClueLink` 唯一且稳定，不随 UI 文案改动。
  - `DeductionChainSO` 配置只引用已存在的 `ClueSO`。
- `PinTu`:
  - `pieceSprites` 与 `correctPositions` 索引一一对应。
  - `snapThreshold` 调整后同步做手感回归。

### 反例风险
- 修改 ID 但未迁移旧存档，读档后会出现丢道具/丢线索。
- 配置数量不匹配导致运行时索引错误。

### 最小实现路径
1. 任何新增配置先做 ID 校验。
2. 提交前检查关键 SO 引用是否为空。
3. 若变更 ID，记录迁移说明并验证旧档行为。

---

## 7. 跨系统集成规范（与 Fy 的边界）

### 推荐做法
- 只定义接口契约，不重写 Fy 内部模块。
- 已存在且应保持的对接模式:
  - 对话激活线索: `Fy_DialogManager -> DialogueClueBridge`
  - 玩法结算发奖: `ChuPuMananger/PuzzleManager -> InventoryController`
  - 存档广播: `EventManager + EventNameEnum.Save/Load`
  - 场景切换: `SceneManager.LoadScene("PersistentScenes" / "PinTu_*" / "ChuPu")`

### 反例风险
- 在 Fy 脚本内直接硬写 Lrb 内部结构体字段。
- 在 Lrb 脚本内假设 Fy 具体实现细节（而非依赖公开方法）。

### 最小实现路径
1. 新对接优先新增桥接层脚本，不跨系统直接改内部状态。
2. 对接输入输出写成“参数契约”并固定在文档。
3. 提交前做一次跨系统场景流回归。

---

## 8. 调试与回归检查

### 推荐做法
- 每次改动后至少跑以下 8 项:
  1. 新档进入后背包可开关且角色移动锁生效。
  2. 道具拾取后 UI 与数量正确。
  3. 存档后退出再加载，道具恢复正确。
  4. 对话触发线索后线索板展示正确。
  5. 线索阅读后颜色状态可保持。
  6. 推理链完成后连线演出与静态线状态正确。
  7. 拼图完成后奖励入包成功。
  8. 从拼图返回 `PersistentScenes` 后数据不丢失。

### 反例风险
- 只在编辑器单场景调试，未验证跨场景和读档路径。

### 最小实现路径
1. 先跑单系统功能。
2. 再跑三条跨系统时序（见下一章）。
3. 最后执行一次保存/退出/加载的全链路回归。

---

## 9. 三条关键跨系统时序（必须回归）

### 时序 A: 对话触发线索
1. Fy 对话流程在事件点调用 `DialogueClueBridge.OnDialogueEvent(...)`。
2. Bridge 根据 `ClueLink` 从 `ClueDatabaseSO` 查找目标线索。
3. `ClueBoardSO.TryCollectClue(...)` 激活线索并更新运行态。
4. `ClueBoardSO` 通过事件通知 UI 刷新显示。

### 时序 B: 拼图结算入包
1. `PuzzleManager` 判定全碎片锁定后执行胜利流程。
2. `PuzzleManager.Save()` 触发奖励逻辑。
3. 奖励通过 `InventoryController.AddItemToInventory(...)` 进入背包系统。
4. 返回主流程前执行 `SaveLoadSystem.SaveGame()` 并切回 `PersistentScenes`。

### 时序 C: 存档/读档恢复
1. 存档时广播 `EventNameEnum.Save`。
2. `InventoryController`、`ClueBoardController` 等各自写入 `gameData` 对应字段。
3. 读档后广播 `EventNameEnum.Load`。
4. Inventory 执行 `LoadDataFromSave` 重连；ClueBoard 执行 `ImportSaveData` 叠加恢复并刷新 UI。

---

## 10. 第三方插件集成边界（仅集成说明）

### Odin Inspector
- 用途: Inspector 可视化与编辑器交互增强（如 `[Header]`、`[BoxGroup]`、`[Required]`）。
- 规范: 仅用于提升可配置性，不用于承载运行时业务逻辑。

### DOTween
- 用途: 拼图和 UI 演出动画（如拼图散落、胜利 UI 动画）。
- 规范: 动画对象销毁前建议 `DOKill` 清理，避免残留 Tween。

---

## 11. 与 Fy BigBag 的并存/迁移边界

### 当前约束
- Lrb 主背包实现是 `Assets/Lrb_Assets/Scripts/InventorySystem`。
- Fy `BigBag` 视为并存系统，不在本指南中定义为主线实现。

### 建议策略
- 新玩法默认接入 Lrb `InventoryController`。
- 若旧逻辑仍在 Fy `BigBag`，仅做兼容对接，不做耦合扩散。
- 后续若迁移，先迁调用层，再迁数据层，最后统一 UI 层。

---

## 12. 维护说明

- 本文档用于“接手 Lrb_Assets 模块的开发者”。
- 规范为建议型，不作为硬门禁。
- 当你只聚焦某子系统（例如只做 ClueBoard）时，可按章节裁剪为子文档。
