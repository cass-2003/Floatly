# Floatly 对照 Dayflow / GoDo 的执行审计

> 更新时间：2026-06-18  
> 来源文档：`docs/FLOATLY-DAYFLOW-GODO-ANALYSIS.md`、`docs/FLOATLY-INDEPENDENT-WINDOWS-DESIGN.md`  
> 目的：把 Dayflow / GoDo 的长期能力分析落到当前 Floatly 代码库，判断哪些可以近期做、哪些需要数据迁移、哪些必须后置。

## 1. 审计结论

原分析文档方向成立：Floatly 不应该复制 Dayflow 或 GoDo 的技术栈，而应该吸收它们的产品能力，并继续保持 WPF/.NET 原生桌面应用。

但从当前代码状态看，实际落地顺序需要更谨慎：

1. TodoList 可以近期继续增强，但必须先扩 `TodoItem` 数据模型。
2. Notes 不建议新增完全独立的 `NoteDocument` 作为第一步，应优先扩展已有 `ScratchNote`。
3. Dayflow 式工作日志属于中长期大模块，必须独立 SQLite 存储，不能塞入现有 `data.json`。
4. 工具箱可以先做系统浏览器快捷入口，不建议第一阶段引入 WebView2。
5. AI 能力必须后置到数据、隐私、存储稳定之后，不应先做 UI 按钮。

## 2. 当前代码现实

### 2.1 当前业务存储

当前业务数据在 `AppDataFile` 中：

```csharp
public sealed class AppDataFile
{
    public List<TodoItem> Todos { get; set; } = [];
    public List<CountdownItem> Countdowns { get; set; } = [];
    public List<ScratchNote> Notes { get; set; } = [];
    public string Scratch { get; set; } = string.Empty;
    public Dictionary<string, string> DateNotes { get; set; } = new(StringComparer.Ordinal);
}
```

当前 `JsonStore` 只有 `settings.json` 和 `data.json` 两个主要读写路径。小数据继续使用 JSON 是合理的，但工作日志、截图索引、AI 摘要、长文本全文索引不适合继续塞进 `data.json`。

### 2.2 当前 TodoItem

当前待办模型：

```csharp
public sealed class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string? Time { get; set; }
    public string Date { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public string? DueDate { get; set; }
    public bool Done { get; set; }
    public bool Pinned { get; set; }
}
```

这能支撑轻量今日待办，但不能支撑 GoDo 式分类、描述、提前提醒、重复规则、子任务和真实优先级。

### 2.3 当前 ScratchNote

当前便签模型：

```csharp
public sealed class ScratchNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = ScratchNoteColors.Default;
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

这已经不是单条 scratch，而是轻量多便签系统。Notes 第一阶段应扩展这个模型，而不是另起 `NoteDocument`。除非后续明确要把“短便签”和“长文档笔记”拆成两个产品概念，否则新增 `NoteDocument` 会造成重复模型和迁移负担。

## 3. 与独立窗口设计文档的关系

`FLOATLY-INDEPENDENT-WINDOWS-DESIGN.md` 解决的是窗口和产品边界：

- 主面板保持轻量。
- TodoList 用独立窗口管理任务。
- Notes 用独立窗口管理便签/知识。

本执行审计解决的是落地顺序和工程风险：

- 哪些字段要先扩。
- 哪些能力要先做真实功能。
- 哪些模块不能进入 JSON。
- 哪些 UI 不能先做成假按钮。

两份文档应该并行存在：

- 独立窗口设计文档：近期 UI 与交互验收依据。
- Dayflow / GoDo 执行审计：中长期功能与架构路线依据。

## 4. TodoList 增强执行方案

### 4.1 推荐近期扩展字段

建议分两步扩 `TodoItem`。

第一步，低风险字段：

```csharp
public string? Category { get; set; }
public string? Description { get; set; }
public string Priority { get; set; } = "normal";
public DateTime CreatedAt { get; set; } = DateTime.Now;
public DateTime UpdatedAt { get; set; } = DateTime.Now;
```

建议 `Priority` 取值：

- `low`
- `normal`
- `high`

第二步，提醒和重复：

```csharp
public int? ReminderOffsetMinutes { get; set; }
public string RepeatType { get; set; } = "none";
public List<string> CompletedRepeatDates { get; set; } = [];
```

建议 `RepeatType` 取值：

- `none`
- `daily`
- `weekdays`
- `weekly`
- `monthly`

### 4.2 迁移策略

旧数据反序列化时新字段会使用默认值。仍建议在 `JsonStore.LoadData()` 后增加数据归一化：

- 空 `Category` 保持 null。
- 空 `Description` 保持 null。
- 空或非法 `Priority` 归一为 `normal`。
- 空或非法 `RepeatType` 归一为 `none`。
- 缺失 `CreatedAt/UpdatedAt` 时用今天或文件加载时间补齐。

这样可以避免旧数据在 UI 中显示成异常状态。

### 4.3 服务层影响

需要调整：

- `TodoStore.GetTodayActiveTodos()`
- `TodoStore.GetActiveTodos()`
- `TodoStore.GetAllTodos()`
- `TodoReminderService.CheckDue()`
- `TodoEditPrompt`
- `TodoListWindow`

提醒逻辑变化最大。当前提醒逻辑只在 `Time == current HH:mm` 且当天未提醒时触发。加入提前提醒和重复任务后，需要明确：

- 一条重复任务今天是否应该出现。
- 今天是否已经完成该重复实例。
- 今天是否已经提醒该重复实例。
- 提前提醒是否已经触发。

建议提醒 key 从 `todo.Id` 改成包含日期和提醒类型：

```text
{todo.Id}:{yyyy-MM-dd}:due
{todo.Id}:{yyyy-MM-dd}:offset:{minutes}
```

## 5. Notes 执行方案

### 5.1 第一阶段不要新增 NoteDocument

原分析文档提到新增 `NoteDocument`。从当前代码看，第一阶段更合理的是扩 `ScratchNote`，因为：

- 主面板已经基于 `ScratchNote` 展示便签预览。
- `ScratchPadWindow` 已经支持列表、搜索、编辑、颜色、置顶、复制、删除。
- `TodoStore` 已经有便签 CRUD。
- 旧 `Scratch` 到 `Notes` 的迁移已经存在。

新增独立 `NoteDocument` 会引入两套笔记模型，短期收益不高。

### 5.2 推荐扩展 ScratchNote

建议新增：

```csharp
public string Category { get; set; } = "idea";
public List<string> Tags { get; set; } = [];
public bool Favorite { get; set; }
public bool Archived { get; set; }
public bool Deleted { get; set; }
public string TemplateType { get; set; } = "blank";
```

保留现有：

- `Title`
- `Content`
- `Color`
- `Pinned`
- `CreatedAt`
- `UpdatedAt`

可运行时计算，不入库：

- 字数。
- 摘要预览。
- 最近编辑文案。
- 分类统计。

### 5.3 NotesWindow 与 ScratchPadWindow 的关系

可以有两种路线：

路线 A：重构 `ScratchPadWindow`

- 优点：复用现有入口和主面板逻辑。
- 缺点：改动范围大，容易影响当前便签可用性。

路线 B：新增 `NotesWindow`

- 优点：可以保留当前 `ScratchPadWindow` 稳定可用，同时做新窗口。
- 缺点：短期会有两个便签窗口概念，需要统一入口。

推荐路线：先新增或重命名为 `NotesWindow`，内部仍使用 `ScratchNote` 数据。等新窗口稳定后，再让主面板“便签”入口打开新窗口，旧 `ScratchPadWindow` 可以删除或降级为快速记录对话框。

### 5.4 Notes 第一版范围

第一版只做真实可用功能：

- 三栏布局。
- 搜索。
- 分类筛选。
- 卡片网格。
- 详情预览。
- 新建便签。
- 编辑标题和内容。
- 收藏/置顶。
- 归档/删除。
- 标签显示和简单编辑。

第一版不做：

- AI 总结。
- 语音输入。
- 云同步。
- 知识关联。
- 复杂富文本。

这些能力没有后端能力前不要展示成可点击功能。

## 6. ToolBox 执行方案

GoDo 的 iframe 网站嵌入不适合照搬。当前项目也没有 WebView2 依赖。

第一阶段建议做工具箱快捷入口：

```csharp
public sealed class ToolLink
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Category { get; set; } = "default";
    public bool Pinned { get; set; }
}
```

存储建议：

- 数量少时放 `data.json` 或 `toolbox.json`。
- 默认使用系统浏览器打开。
- 后续确实需要内嵌浏览器时再评估 WebView2。

不建议第一阶段做：

- iframe 嵌入。
- WebView2 常驻页面。
- 网站登录态管理。
- 自动注入脚本。

## 7. WorkJournal 执行方案

### 7.1 为什么必须独立模块

Dayflow 式工作日志涉及：

- 前台应用识别。
- 高频时间记录。
- 可选截图。
- 长期历史数据。
- 隐私排除规则。
- AI 摘要和问答。

这些都不适合放进现有 `TodoStore` 或 `data.json`。

### 7.2 推荐架构

建议新增：

```text
Models/
  WorkJournalActivity.cs
  WorkJournalSettings.cs
  WorkJournalSummary.cs

Services/
  ActivityTrackerService.cs
  WorkJournalStore.cs
  PrivacyFilterService.cs
  IdleDetectionService.cs

WorkJournalWindow.xaml
WorkJournalWindow.xaml.cs
```

截图和 AI 后置：

```text
Services/
  ScreenCaptureService.cs
  AiAnalysisService.cs
```

### 7.3 存储建议

工作日志必须使用 SQLite 或等价数据库，不进入 `data.json`。

建议目录：

```text
%AppData%\Floatly\work-journal\
  journal.db
  frames\
  thumbnails\
  exports\
```

第一阶段只记录：

- 开始时间。
- 结束时间。
- 应用进程名。
- 窗口标题。
- 空闲状态。

截图阶段再增加：

- frame id。
- 文件路径。
- 缩略图路径。
- 捕获时间。
- 隐私过滤状态。

### 7.4 隐私必须第一阶段进入设置

即使 MVP 不截图，也必须有：

- 开关：启用/暂停工作日志。
- 托盘暂停入口。
- 排除应用列表。
- 排除窗口标题关键词。
- 保留天数。
- 一键清除日志。

如果先做后台记录再补隐私设置，会破坏用户信任。

## 8. AI 执行边界

AI 能力不能早于数据和隐私能力。

推荐顺序：

1. 本地数据结构稳定。
2. 用户能查看和删除数据。
3. 隐私排除规则生效。
4. 本地 Ollama 接入。
5. 云端 provider 接入。

API key 不能明文进入 `settings.json`。建议：

- Windows DPAPI。
- Windows Credential Manager。

云端 AI 必须提示：

- 会上传哪些内容。
- 上传到哪个 provider。
- 是否包含截图。
- 是否包含窗口标题。

## 9. 风险清单

### P0 风险

- 工作日志或截图数据进入 `data.json`，导致文件膨胀和主程序卡顿。
- AI 功能在隐私设置前上线。
- Notes 做出 AI/语音/云同步按钮但没有真实功能。
- 重复任务完成逻辑错误，导致任务永久消失或每天重复弹提醒。

### P1 风险

- `ScratchNote` 和 `NoteDocument` 并存，造成用户不知道便签在哪里。
- WebView2 过早引入，增加发布包体积和运行依赖。
- TodoList 视觉优先级和真实优先级不一致。
- 分类/标签字段没有迁移和默认值，旧数据在 UI 中显示异常。

### P2 风险

- 主面板继续堆复杂功能，破坏 Floatly 常驻小组件的轻量感。
- 设置页继续膨胀，没有分组和搜索。
- 统计卡片过早复杂化，但数据量不足以支撑。

## 10. 建议执行路线

### 近期：1 到 2 个版本

1. TodoItem 增加 `Category/Description/Priority/CreatedAt/UpdatedAt`。
2. TodoListWindow 把视觉优先级改为真实字段。
3. TodoEditPrompt 增加分类、描述、优先级。
4. ScratchNote 增加 `Category/Tags/Favorite/Archived/Deleted/TemplateType`。
5. NotesWindow 第一版三栏 UI，只做真实可用功能。

### 中期：3 到 5 个版本

1. Todo 重复规则和提前提醒。
2. ToolBox 快捷入口。
3. WorkJournal MVP：只记录前台应用和窗口标题。
4. 设置页增加隐私和工作日志配置。
5. SQLite 存储层。

### 长期：5 个版本以后

1. 截图与缩略图。
2. 本地 Ollama 摘要。
3. 云端 AI provider。
4. 日报/周报。
5. 工作日志问答。
6. Notes Markdown/导出/知识关联。

## 11. 对原分析文档的修订建议

建议保留 `FLOATLY-DAYFLOW-GODO-ANALYSIS.md`，但后续可以修正两点：

1. 笔记系统章节从“新增 `NoteDocument`”改为“优先扩展 `ScratchNote`；若未来区分短便签和长文档，再新增 `NoteDocument`”。
2. 增加与 `FLOATLY-INDEPENDENT-WINDOWS-DESIGN.md` 的关系说明，避免看起来像两套互相竞争的路线。

## 12. 最终判断

Floatly 当前最稳的升级路径是：

1. 先把现有 TodoList 和 Notes 做实。
2. 再做 ToolBox。
3. 然后做 WorkJournal MVP。
4. 最后做截图和 AI。

这个顺序能最大化利用当前 WPF 桌面壳、托盘、快捷键、本地 JSON 和独立窗口能力，同时避免过早引入 SQLite、WebView2、截图和 AI 带来的复杂度。

