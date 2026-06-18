# Floatly 对照 Dayflow 与 GoDo 的功能分析

> 目的：评估当前 Floatly 是否适合吸收 Dayflow 与 GoDo 的能力，并给出可执行的 Windows 原生实现路线。

## 1. 结论

Floatly 不适合直接复制 Dayflow 或 GoDo 的代码，但非常适合吸收它们的产品能力。

- Dayflow 适合作为 Floatly 的长期大模块参考：自动工作日志、时间线、日报、周报、AI 总结。
- GoDo 适合作为 Floatly 的近期功能补强参考：待办增强、重复提醒、详细描述、笔记系统、工具入口。
- Floatly 应保持 Windows 原生 WPF/.NET 底座，不建议改成 Electron，也不建议把网页/iframe 作为核心架构。

推荐方向：

1. 先吸收 GoDo 的待办增强能力。
2. 再把现有速记便签升级为独立笔记系统。
3. 然后新增 Dayflow 式的工作日志独立模块。
4. 最后接入本地/云端 AI，做日报、周报和问答。

## 2. 当前 Floatly 基础

当前项目是 .NET 8 + WPF 的 Windows 原生桌面应用，源码目录仍叫 `DeskLite/`，产品名是 Floatly。

已有基础能力：

- 常驻桌面小组件。
- 系统托盘。
- 全局快捷键。
- 开机自启动。
- 本地 JSON 存储。
- 设置页。
- 待办、提醒、番茄钟、速记便签、倒数日、天气、日历、下班倒计时、薪水助手。
- Inno Setup 安装包与绿色版发布流程。

关键文件：

- `DeskLite/DeskLite.csproj`：.NET 8 WPF 项目配置。
- `DeskLite/MainWindow.xaml.cs`：主窗口、定时刷新、托盘、快捷键、待办提醒入口。
- `DeskLite/TrayService.cs`：托盘菜单与气泡通知。
- `DeskLite/Models/AppSettings.cs`：用户设置模型。
- `DeskLite/Models/AppDataFile.cs`：业务数据模型。
- `DeskLite/Services/JsonStore.cs`：本地 JSON 读写与旧数据迁移。
- `DeskLite/Services/TodoStore.cs`：待办与便签存储逻辑。

这个基础已经覆盖了 Dayflow 和 GoDo 都需要的桌面壳能力，因此新增功能应按 Floatly 的服务/模型/窗口结构重写，而不是移植外部项目代码。

## 3. Dayflow 分析

Dayflow 是 macOS 原生 Swift/SwiftUI 应用，核心是自动记录屏幕活动并生成工作时间线。它依赖 macOS 的屏幕录制权限、ScreenCaptureKit、菜单栏、Keychain、Sparkle 更新等能力。

Dayflow 的主要产品能力：

- 自动记录屏幕活动。
- 把截图和上下文聚合成时间线。
- 生成每日 standup。
- 生成周报。
- 支持自然语言询问工作日志。
- 支持本地模型和云端 AI provider。
- 本地优先存储，可清理历史录制。

对 Floatly 的价值：

- 可作为“工作日志模块”的产品参考。
- 可增强 Floatly 的生产力定位，让它从桌面信息面板升级成个人工作助理。
- 可与 Floatly 已有的待办、番茄钟、工作时间、便签结合，形成比 Dayflow 更贴近 Windows 日常办公的上下文。

不适合直接复用的部分：

- Swift/SwiftUI UI。
- macOS ScreenCaptureKit。
- macOS Keychain。
- Sparkle 更新。
- macOS 菜单栏和权限体系。

Windows 对应实现：

| Dayflow 能力 | Windows/Floatly 实现建议 |
| --- | --- |
| 屏幕录制/截图 | 先用 GDI/CopyFromScreen 做 MVP，后续升级 Windows Graphics Capture 或 Desktop Duplication API |
| 当前应用识别 | Win32 `GetForegroundWindow`、`GetWindowText`、进程名 |
| 本地数据库 | SQLite，不建议放进现有 `data.json` |
| 截图文件 | `%AppData%\Floatly\work-journal\frames\` |
| 时间线窗口 | 新增 `WorkJournalWindow.xaml` |
| AI 分析 | 先接 Ollama，再扩展 Gemini/OpenAI/Claude CLI |
| API key 保存 | Windows DPAPI 或 Credential Manager |
| 自动清理 | 设置保留天数、最大磁盘占用 |
| 隐私控制 | 暂停记录、排除应用、排除窗口标题关键词、排除全屏/隐私模式 |

## 4. GoDo 分析

GoDo 是 Electron 桌面待办应用，代码主要集中在 `main.js`、`renderer.js`、`index.html` 和 `styles.css`。它更像单人快速成型的桌面工具，功能密集但架构相对单体。

GoDo 的主要产品能力：

- 桌面浮动待办。
- 分类待办。
- 任务详细描述。
- 提前提醒。
- 重复提醒：每天、工作日、每周、每月。
- 富文本任务标题和描述。
- 多文件笔记。
- CodeMirror 代码编辑器。
- 常用网站 iframe 嵌入。
- 主题、透明度、精简模式、托盘、全局快捷键。

GoDo 对 Floatly 的价值：

- 待办增强非常适合直接吸收。
- 笔记系统可以作为速记便签的升级方向。
- 网站嵌入可以转化为“工具箱/快捷入口”，但不建议照搬 iframe 方案。

不建议直接复用的部分：

- Electron 主进程/渲染进程代码。
- 单体式 `renderer.js` 状态管理。
- iframe 嵌入外部网站。
- `nodeIntegration: true` + `contextIsolation: false` 的 Electron 安全配置。

GoDo 与当前 Floatly 对照：

| 能力 | GoDo | 当前 Floatly | 建议 |
| --- | --- | --- | --- |
| 桌面常驻窗口 | 有 | 已有 | 保持 Floatly 实现 |
| 托盘 | 有 | 已有 | 保持 Floatly 实现 |
| 全局快捷键 | 有 | 已有 | 保持 Floatly 实现 |
| 开机启动 | 有 | 已有 | 保持 Floatly 实现 |
| 待办 | 分类、描述、提醒、重复、富文本 | 轻量待办、时间、截止日、置顶 | 吸收分类、描述、提前提醒、重复 |
| 笔记 | 多文件、CodeMirror、自动保存 | 速记便签、颜色、置顶、搜索 | 新增独立笔记窗口 |
| 网站 | iframe 嵌入 | 无 | 做快捷入口或 WebView2 工具箱 |
| 主题 | 多预设 | 深浅主题、皮肤、字体、透明度 | Floatly 已更成熟 |
| 存储 | electron-store | JSON | 小数据继续 JSON，大数据改 SQLite |

## 5. 功能融合设计

### 5.1 待办增强

当前 `TodoItem` 很轻量，适合扩展。

建议新增字段：

```csharp
public string? Category { get; set; }
public string? Description { get; set; }
public int? ReminderOffsetMinutes { get; set; }
public string RepeatType { get; set; } = "none";
public DateTime CreatedAt { get; set; } = DateTime.Now;
public DateTime UpdatedAt { get; set; } = DateTime.Now;
```

`RepeatType` 建议取值：

- `none`
- `daily`
- `weekdays`
- `weekly`
- `monthly`

提醒逻辑建议：

- `Time` 继续表示当天提醒时间。
- `DueDate` 继续表示截止日期。
- `ReminderOffsetMinutes` 表示提前提醒。
- 重复任务不应每天生成实体副本，优先在提醒和展示层计算“今天是否出现”。
- 完成重复任务时，需要记录完成实例日期，避免永久完成整个重复任务。

需要新增或调整：

- `TodoItem` 模型。
- `TodoStore` 排序、筛选、迁移逻辑。
- `TodoReminderService` 支持提前提醒和重复规则。
- `TodoListWindow` 增加分类、描述、重复、提前提醒编辑。
- 设置页可选增加默认分类管理。

### 5.2 笔记系统

当前 `ScratchNote` 适合轻量便签，但不适合承载代码笔记和长文档。

建议新增独立模型：

```csharp
public sealed class NoteDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = "markdown";
    public List<string> Tags { get; set; } = [];
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
```

实现建议：

- 新增 `NotesWindow.xaml`。
- 左侧列表，右侧编辑器。
- 支持搜索、置顶、标签。
- MVP 先做纯文本/Markdown。
- 代码高亮后续用 AvalonEdit 或 WebView2 + Monaco，不建议第一阶段就做。

与现有速记便签的关系：

- 主面板继续展示轻量便签。
- 独立笔记窗口承载长文档。
- 可支持“从便签转为笔记”。

### 5.3 工具箱/网站入口

GoDo 的 iframe 网站嵌入在 Electron 中也不稳定，很多站点会因为 CSP 或 X-Frame-Options 拒绝被嵌入。WPF 中如果要内嵌网页，应使用 WebView2，但这会增加运行时依赖和复杂度。

推荐先做工具箱，而不是内嵌浏览器：

- 常用网站快捷入口。
- 翻译、搜索、AI 工具入口。
- 支持自定义名称、URL、图标。
- 默认用系统浏览器打开。
- 后续可对少数页面提供 WebView2 独立窗口。

### 5.4 工作日志模块

Dayflow 能力不应塞进主面板，而应作为独立窗口和后台服务。

建议模块结构：

```text
DeskLite/
  Models/
    WorkJournalActivity.cs
    WorkJournalFrame.cs
    WorkJournalSummary.cs
    WorkJournalSettings.cs
  Services/
    ActivityTrackerService.cs
    ScreenCaptureService.cs
    WorkJournalStore.cs
    TimelineBuilderService.cs
    AiAnalysisService.cs
    PrivacyFilterService.cs
  WorkJournalWindow.xaml
  WorkJournalWindow.xaml.cs
```

主面板只放轻量入口：

- 记录状态：记录中 / 已暂停。
- 今日活动时长。
- 今日摘要一句话。
- 打开工作日志按钮。

后台服务职责：

- 定时记录前台窗口。
- 按配置定时截图。
- 按时间段聚合活动。
- 后台队列调用 AI 分析。
- 自动清理旧截图和旧记录。

数据存储建议：

- 工作日志使用 SQLite。
- 截图、缩略图、导出文件使用独立目录。
- 不放入 `data.json`，避免文件无限膨胀和读写阻塞。

## 6. 隐私与安全设计

工作日志和截图属于高敏感数据，必须把隐私控制放在第一阶段，而不是后期补。

必须具备：

- 全局暂停/继续记录。
- 托盘菜单快速暂停。
- 设置页显示记录状态。
- 排除应用列表，例如密码管理器、浏览器隐私窗口、聊天软件。
- 排除窗口标题关键词。
- 保留天数。
- 最大磁盘占用。
- 一键删除全部工作日志。
- 导出前确认。

AI provider 规则：

- 默认优先本地 Ollama。
- 云端 provider 必须明确提示会上传被分析内容。
- API key 不应明文写入 JSON。
- AI 分析失败不能影响主程序运行。

## 7. 存储策略

当前 JSON 适合设置、待办、倒数日、便签等小数据。

建议继续使用 JSON 的数据：

- `settings.json`
- `data.json`
- `weather-cache.json`
- 小规模工具箱配置

建议改用 SQLite 的数据：

- 工作日志活动记录。
- 截图索引。
- AI 摘要。
- 聊天查询记录。
- 大量笔记全文搜索索引。

建议目录：

```text
%AppData%\Floatly\
  settings.json
  data.json
  weather-cache.json
  notes.json
  toolbox.json
  work-journal\
    journal.db
    frames\
    thumbnails\
    exports\
```

## 8. 分阶段路线

### 阶段 1：GoDo 式待办增强

目标：先把现有待办升级成更完整的日程/任务系统。

任务：

- 扩展 `TodoItem` 字段。
- 增加任务分类。
- 增加任务描述。
- 增加提前提醒。
- 增加重复规则。
- 更新 `TodoReminderService`。
- 更新 `TodoListWindow` 编辑体验。
- 保持主面板只展示高密度摘要，不塞太多表单。

收益：

- 改动范围可控。
- 与现有 Floatly 用户价值直接相关。
- 为 Dayflow 的日报上下文打基础。

### 阶段 2：笔记系统

目标：从速记便签扩展到真正的笔记/片段管理。

任务：

- 新增 `NoteDocument` 模型。
- 新增 `NotesWindow`。
- 支持标题、正文、标签、搜索、置顶。
- 支持自动保存。
- 保留现有速记便签。

后续增强：

- Markdown 预览。
- 代码高亮。
- 便签转笔记。

### 阶段 3：工具箱入口

目标：替代 GoDo 的 iframe 网站嵌入，做更稳定的 Windows 工具入口。

任务：

- 新增 `ToolboxWindow`。
- 支持自定义网站/本地工具。
- 默认系统浏览器打开。
- 可选 WebView2 预览。

### 阶段 4：Dayflow MVP 工作日志

目标：不接 AI，先完成可用的本地活动时间线。

任务：

- 新增 `ActivityTrackerService` 记录前台窗口。
- 新增 `WorkJournalStore` 使用 SQLite。
- 新增 `WorkJournalWindow` 显示今日时间线。
- 托盘加入暂停/继续工作日志。
- 设置页加入工作日志开关、保留天数、排除应用。

MVP 数据：

- 时间。
- 应用进程名。
- 窗口标题。
- 活动时长。
- 空闲状态。

### 阶段 5：截图与隐私控制

目标：加入低频截图，但保证用户可控。

任务：

- 新增 `ScreenCaptureService`。
- 截图间隔可配置，默认 60 秒或更长。
- 支持排除应用和窗口标题关键词。
- 支持自动清理。
- 支持一键删除。
- 支持截图缩略图。

### 阶段 6：AI 总结

目标：生成活动块摘要、日报、周报。

任务：

- 新增 `AiAnalysisService`。
- 首先支持 Ollama。
- 后续支持 Gemini/OpenAI/Claude CLI。
- 后台队列处理，不阻塞 UI。
- 失败重试和失败提示。
- 工作日志窗口展示 AI 摘要。

### 阶段 7：工作日志问答

目标：实现 Dayflow 的“Chat with your work journal”能力。

任务：

- 建立摘要索引。
- 支持按日期范围检索。
- 支持自然语言查询。
- 支持导出 Markdown。

## 9. 建议优先级

短期优先：

1. 待办分类、描述、提前提醒、重复规则。
2. 独立笔记窗口。
3. 工具箱入口。

中期优先：

1. 工作日志 MVP。
2. SQLite 存储层。
3. 隐私设置。

长期优先：

1. 截图分析。
2. AI 日报/周报。
3. 工作日志问答。

## 10. 最终判断

Floatly 的优势是 Windows 原生、小而常驻、贴近日常桌面使用。Dayflow 的优势是自动化工作记录和 AI 时间线。GoDo 的优势是待办和笔记功能密集。

最佳路线不是三者合并成一个巨型应用，而是：

- 保持 Floatly 的轻量桌面小组件定位。
- 把 GoDo 的待办/笔记能力作为近期增强。
- 把 Dayflow 的工作日志能力作为独立高级模块。
- 所有大数据、截图和 AI 分析都与现有小组件数据隔离。

这样 Floatly 可以从“桌面信息面板”逐步升级为“Windows 个人生产力助手”，同时不牺牲现在最有价值的轻量感。
