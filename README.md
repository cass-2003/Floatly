<div align="center">
  <img src="DeskLite/Assets/app-icon.png" alt="Floatly Logo" width="112" height="112">

  # Floatly（浮岛）

  ### 轻量级 Windows 桌面小组件

  把时钟、天气、黄历、待办、番茄钟、下班倒计时和薪水助手放进一块常驻桌面的毛玻璃信息面板。  
  本地保存 · 可自由缩放 · 可穿透鼠标 · 面向 Windows 桌面日常使用

  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows)](https://www.microsoft.com/windows)
  [![UI](https://img.shields.io/badge/UI-WPF%20%2B%20HandyControl-blue)](https://github.com/dotnet/wpf)
  [![Release](https://img.shields.io/github/v/release/cass-2003/Floatly?label=release)](https://github.com/cass-2003/Floatly/releases/tag/v2.0.22)
  [![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

  [下载 v2.0.22](https://github.com/cass-2003/Floatly/releases/tag/v2.0.22) · [快速开始](#快速开始) · [功能](#功能) · [开发](#开发)
</div>

---

## 当前状态

Floatly 2.0 正在进行 UI 重构，主小组件和设置页已经切到新的毛玻璃设计方向，但卡片比例、字体密度、细节动效和设计稿对齐仍在持续打磨。`v2.0.22` 是当前可用版本，适合体验和反馈，不代表最终视觉定稿。

---

## 功能

### 桌面小组件

- **时钟与日期**：24 小时制、秒显开关、公历日期、城市展示。
- **天气**：Open-Meteo 天气、温度区间、体感温度、明日天气、日出日落、天气图标。
- **定位兜底**：Windows 定位、访客天气接口、IP 定位、经纬度反查、手动城市多级兜底。
- **黄历与日历**：农历、干支生肖、宜忌、周历/月历、日期备注、节假日休/班标记。
- **今日待办**：卡片预览、完成/恢复、置顶、搜索、独立待办管理窗口、到点托盘提醒。
- **番茄钟**：专注/短休息/长休息循环，工作时长、休息时长和长休息轮次可配置。
- **下班倒计时**：上班/下班时间、工作日模式、今日进度条。
- **摸鱼小助手**：按月薪、工作天数和每日工时实时估算今日已赚薪水。
- **倒数日与年进度**：内置节日、自定义目标日、年度百分比和进度条。
- **每日一句**：离线语录，每日展示。
- **速记便签**：最多 20 条便签，支持置顶、颜色标签、复制、搜索和独立编辑。

### 窗口与外观

- 无边框窗口，支持四边四角拖动缩放。
- 窗口置顶、鼠标穿透、开机自启动。
- 深色/浅色主题、窗口透明度、字号、字体、字体颜色。
- 默认、纯色、图片、视频皮肤模式；视频皮肤仍属于实验体验，建议优先使用默认/图片皮肤。
- 毛玻璃卡片、背景遮罩、图标资源和卡片背景图。
- 全局快捷键：默认 `Ctrl+Shift+D` 显示/隐藏，`Ctrl+Shift+N` 快速添加待办，可在设置页修改。

### 设置页

设置页目前包含窗口行为、工作时间、时钟显示、快捷键、摸鱼收入、天气、日历、主题、字体、皮肤、窗口大小和模块管理等配置。左侧旧导航已移除，恢复默认、关于和版本信息放到底栏；关于会跳转到 GitHub 仓库。

---

## 快速开始

### 环境要求

- Windows 10 / 11 x64
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 下载安装

从 [GitHub Releases v2.0.22](https://github.com/cass-2003/Floatly/releases/tag/v2.0.22) 下载：

| 文件 | 说明 |
|------|------|
| `Floatly-Setup-2.0.22.exe` | 推荐，中文安装向导 |
| `Floatly-2.0.22-win-x64.zip` | 绿色版，解压后运行 `Floatly.exe` |

### 首次使用

1. 启动后会出现桌面小组件和系统托盘图标。
2. 右键小组件或托盘图标打开设置、待办、倒数日、数据备份等入口。
3. 拖动窗口内容区域移动位置，拖动边缘或角落调整大小。
4. 如果开启鼠标穿透，可通过托盘菜单或快捷键重新显示/操作。

---

## 开发

### 本地运行

```powershell
git clone https://github.com/cass-2003/Floatly.git
cd Floatly
dotnet run --project .\DeskLite\DeskLite.csproj
```

### Debug 构建

```powershell
dotnet build .\DeskLite\DeskLite.csproj
```

### Release 构建

项目根目录提供打包脚本，会发布框架依赖版并调用 Inno Setup 生成安装包。

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

输出目录：

| 路径 | 说明 |
|------|------|
| `release\Floatly\` | win-x64 发布目录 |
| `release\Floatly-Setup-2.0.22.exe` | Inno Setup 安装包 |
| `release\Floatly-2.0.22-win-x64.zip` | 绿色压缩包，可用 `Compress-Archive` 从发布目录生成 |

### GitHub Release

Release 说明统一使用中文，模板见 [docs/RELEASE_NOTES_zh.md](docs/RELEASE_NOTES_zh.md)。当前版本说明见 [docs/releases/v2.0.22.md](docs/releases/v2.0.22.md)。

```powershell
gh release create v2.0.22 `
  --repo cass-2003/Floatly `
  --title "Floatly v2.0.22" `
  --notes-file docs/releases/v2.0.22.md `
  release/Floatly-Setup-2.0.22.exe `
  release/Floatly-2.0.22-win-x64.zip
```

---

## 技术栈

| 领域 | 方案 |
|------|------|
| 框架 | .NET 8、WPF、Windows Forms 托盘 |
| UI 控件 | HandyControl |
| 农历黄历 | lunar-csharp |
| SVG 渲染 | SharpVectors.Wpf |
| 天气 | Open-Meteo，无需 API Key |
| 存储 | `%AppData%\Floatly\` 下的本地 JSON |
| 平台能力 | Windows 全局热键、透明/穿透窗口、无边框缩放、开机自启动 |

源码目录仍叫 `DeskLite/`，这是历史命名；应用名、程序集和发布产物均为 `Floatly`。

---

## 数据存储

应用数据保存在 `%AppData%\Floatly\`：

| 文件/目录 | 内容 |
|-----------|------|
| `settings.json` | 窗口位置、主题、透明度、模块开关、热键、字体、皮肤、天气城市等设置 |
| `data.json` | 待办、倒数日、速记便签、日期备注等业务数据 |
| `weather-cache.json` | 天气与定位缓存 |
| `skins\` | 导入的自定义图片/视频皮肤 |

从旧版 DeskLite 升级时，会尝试把 `%AppData%\DeskLite\` 中的设置和数据迁移到 `%AppData%\Floatly\`。

---

## 托盘与快捷操作

| 操作 | 默认快捷键 | 说明 |
|------|------------|------|
| 显示/隐藏 | `Ctrl+Shift+D` | 切换主窗口可见性 |
| 快速添加待办 | `Ctrl+Shift+N` | 弹出待办输入框 |
| 设置 | 无 | 打开设置页 |
| 查看全部待办 | 无 | 打开待办管理窗口 |
| 添加倒数日 | 无 | 新建自定义倒数事件 |
| 日历回到今天/跳转日期 | 无 | 快速导航日历 |
| 导出数据备份 | 无 | 将业务数据备份到桌面 |
| 退出 | 无 | 关闭 Floatly |

---

## 路线图

仍在计划或打磨中的方向：

- 主小组件继续对齐设计稿，完善毛玻璃质感、卡片比例、字体层级和动效。
- 设置页继续收紧细节，减少输入控件遮挡和高密度场景下的溢出。
- 视频皮肤体验稳定化。
- 数据导入、贴边收纳、第二时区、月相、电量监控、周进度/距周末倒计时。
- 2027+ 节假日数据和更完整的发布自动化。

---

## 许可证

[MIT License](LICENSE)

---

<div align="center">
  Made with .NET 8 & WPF
</div>
