<div align="center">
  <img src="DeskLite/Assets/app-icon.png" alt="Floatly Logo" width="120" height="120">
  
  # Floatly（浮岛）
  
  ### 🪶 轻量级 Windows 桌面小组件
  
  常驻桌面的一小块「今日信息条」：时钟、黄历、天气、待办、番茄钟、下班倒计时……  
  不占地方 · 低资源占用 · 简洁优雅 · 数据本地保存
  
  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
  [![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
  [![WPF](https://img.shields.io/badge/UI-WPF-blue)](https://github.com/dotnet/wpf)
  [![Release](https://img.shields.io/github/v/release/cass-2003/Floatly?label=release)](https://github.com/cass-2003/Floatly/releases/tag/v2.0.2)
  
  [下载 v2.0.2](https://github.com/cass-2003/Floatly/releases/tag/v2.0.2) · [快速开始](#-快速开始) · [功能特性](#-核心功能) · [路线图](#-路线图)
  
</div>

---

## ✨ 核心功能

<table>
<tr>
<td width="33%">

### 🕐 时钟与黄历
- 公历 + 农历双历显示
- **黄历详情**（51 万年历风格，可折叠）
- 节气、生肖、干支、宜忌
- 周历 / 月历切换、翻页
- **2026 年节假日**休/班标注
- 日历格 **日期备注**（点击日期添加）

</td>
<td width="33%">

### 🌤️ 天气预报
- 实时温度与天气状况
- 今明日高低温
- 日出日落时间
- 自动定位 + 手动城市
- Open-Meteo 免费接口

</td>
<td width="33%">

### ✅ 今日清单
- 卡片式待办，计数徽章
- 增删勾、编辑、置顶（★）
- 时间前缀 `14:00 周会` + **到时托盘提醒**
- **查看全部**窗口（进行中 / 已完成 / 全部 + 搜索）
- 超过 5 条时「还有 N 条」；已完成可恢复
- 全局快捷键快速添加（可自定义）

</td>
</tr>
</table>

### 🎛️ 可选模块（自由组合 + 排序）

在 **设置 → 模块** 中开关，并可用上移/下移调整显示顺序：

| 模块 | 说明 |
|------|------|
| 📅 **倒数日** | 内置节日 + 自定义事件，带进度条 |
| 🍅 **番茄钟** | 25/5 专注计时（可配置长休息），进度条 + 托盘提醒 |
| ⏰ **下班倒计时** | 上下班时间、工作日模式、日进度条 |
| 💰 **摸鱼小助手** | 按月薪实时计算收入，金色每秒跳动显示 |
| 📊 **年进度** | 全年已过百分比与可视化进度条 |
| 💬 **每日一句** | 离线语录库，按日随机展示 |
| 📝 **速记便签** | 最多 20 条，置顶/颜色标签、搜索、复制、独立编辑窗口 |

### 🎯 窗口与外观

- ✨ 无边框透明面板，四边/四角调整大小
- 🌓 深色/浅色主题切换
- 🎨 **皮肤**：默认 / 纯色 / 自定义图片 / **视频背景**（循环静音）
- 🔤 自定义 **字体**、**字号**（10–16 pt）、**字体颜色**
- 🎚️ 透明度（30–100%）滑块调节
- 📌 窗口置顶 / 鼠标穿透
- 🚀 开机自启动
- ⌨️ **可自定义全局快捷键**（默认 `Ctrl+Shift+D` 显示/隐藏，`Ctrl+Shift+N` 快速添加待办）
- 🖱️ 右键标题区快捷操作

---

## 📸 截图预览

<div align="center">
  
  | 浅色主题 | 深色主题 |
  |---------|---------|
  | ![Light Theme](docs/screenshots/light.png) | ![Dark Theme](docs/screenshots/dark.png) |
  
  | 黄历详情 | 周历视图 |
  |---------|---------|
  | ![Huangli](docs/screenshots/huangli.png) | ![Calendar](docs/screenshots/calendar.png) |
  
</div>

---

## 🚀 快速开始

### 环境要求

- Windows 10 / 11（x64）
- [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)（使用安装包时会检测并提示）

### 下载安装（推荐）

从 [Releases v2.0.2](https://github.com/cass-2003/Floatly/releases/tag/v2.0.2) 下载：

| 文件 | 说明 |
|------|------|
| `Floatly-Setup-2.0.2.exe` | 中文安装向导（Inno Setup） |
| `Floatly-win-x64.zip` | 绿色版（需已安装 .NET 8 桌面运行时） |

### 从源码运行

```powershell
git clone https://github.com/cass-2003/Floatly.git
cd Floatly/DeskLite
dotnet run
```

### 构建与发布

```powershell
# 框架依赖发布（约 2–5 MB，需用户安装 .NET 8 桌面运行时）
cd DeskLite
dotnet publish -c Release -r win-x64 --self-contained false

# 输出目录
# DeskLite\bin\Release\net8.0-windows\win-x64\publish\Floatly.exe
```

**制作安装包**（需 [Inno Setup 6](https://jrsoftware.org/isinfo.php)）：

```powershell
# 1. 将 publish 输出复制到 release/Floatly/
# 2. 编译安装脚本
iscc installer\Floatly.iss
# 输出：release\Floatly-Setup-2.0.2.exe
```

### 首次使用

1. 启动后图标出现在系统托盘
2. 双击托盘图标显示主窗口
3. 右键托盘 → **设置...** 进行个性化配置
4. 拖拽窗口边缘调整大小，拖拽内容区域移动位置

---

## 🛠️ 技术栈

<table>
<tr>
<td width="50%">

**核心框架**
- 🎯 **.NET 8** + **WPF** 无边框透明窗口
- 🖥️ **Windows Forms** 系统托盘集成
- 📦 单项目、单 exe（`Floatly.exe`），极简依赖

</td>
<td width="50%">

**数据服务**
- 🌤️ **Open-Meteo** 天气（免费、无需 API Key）
- 🗓️ **lunar-csharp** 农历 / 黄历计算
- 💾 本地 JSON 存储，仅 **1 个 NuGet** 依赖

</td>
</tr>
</table>

> 源码目录仍为 `DeskLite/`（历史命名），编译产物与安装包均为 **Floatly**。

---

## 📂 数据存储

应用数据保存在 `%AppData%\Floatly\`：

| 文件 / 目录 | 说明 |
|-------------|------|
| `settings.json` | 窗口位置、主题、模块开关与顺序、皮肤、热键、字体等 |
| `data.json` | 待办、倒数日、速记便签、日期备注等业务数据 |
| `skins/` | 导入的自定义皮肤图片/视频 |

**💡 提示**：托盘菜单 → **导出数据备份** 可将 `data.json` 另存到桌面（`floatly-backup-日期.json`）。

> 从旧版 DeskLite 升级时，首次启动会自动将 `%AppData%\DeskLite\` 中的设置与数据迁移到 `%AppData%\Floatly\`。

---

## 🎛️ 托盘菜单

| 菜单项 | 默认快捷键 | 说明 |
|--------|-----------|------|
| 显示/隐藏 | `Ctrl+Shift+D` | 切换主窗口可见性（可在设置中修改） |
| 设置... | — | 打开设置窗口 |
| 添加待办 | `Ctrl+Shift+N` | 弹出输入框快速添加 |
| 查看全部待办 | — | 待办管理窗口（历史 / 搜索 / 编辑） |
| 添加倒数日... | — | 新建自定义倒数事件 |
| 日历 → 回到今天 | — | 日历导航快捷跳转 |
| 日历 → 跳转日期... | — | 选择特定日期 |
| 导出数据备份 | — | 备份 `data.json` 到桌面 |
| 退出 | — | 关闭应用 |

---

## 🗺️ 路线图

以下功能在 [DESIGN.md](DESIGN.md) 中规划，**尚未实现**：

<table>
<tr>
<td width="50%">

**功能增强**
- 👁️ 护眼提醒
- 🏃 习惯打卡
- 🚀 快捷启动（4 格）
- 📥 数据导入（当前仅支持导出）

</td>
<td width="50%">

**体验优化**
- 📌 贴边收纳自动隐藏
- 🌍 第二时区显示
- 🌙 月相展示
- 🔋 电量监控
- 📅 周进度 / 距周末倒计时
- 📦 自包含单文件打包
- 🗓️ 2027+ 节假日数据

</td>
</tr>
</table>

> 详细设计、实现状态对照与体积/内存目标见 [DESIGN.md](DESIGN.md)  
> Release 说明统一使用中文，模板见 [docs/RELEASE_NOTES_zh.md](docs/RELEASE_NOTES_zh.md)

---

## 📄 许可证

[MIT License](LICENSE)

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

<div align="center">
  
  Made with ❤️ using .NET 8 & WPF
  
  ⭐ 如果这个项目对你有帮助，请给个 Star！
  
</div>
