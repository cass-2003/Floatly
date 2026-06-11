<div align="center">
  <img src="DeskLite/Assets/app-icon.png" alt="Floatly Logo" width="120" height="120">
  
  # Floatly（浮岛）
  
  ### 🪶 轻量级 Windows 桌面小组件
  
  常驻桌面的一小块「今日信息条」：时钟、农历、黄历、天气、待办、倒数日  
  不占地方 · 低资源占用 · 简洁优雅
  
  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
  [![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
  [![WPF](https://img.shields.io/badge/UI-WPF-blue)](https://github.com/dotnet/wpf)
  
  [快速开始](#-快速开始) · [功能特性](#-核心功能) · [截图预览](#-截图预览) · [路线图](#-路线图)
  
</div>

---

## ✨ 核心功能

<table>
<tr>
<td width="33%">

### 🕐 时钟与历法
- 公历 + 农历双历显示
- 节气、生肖、干支
- **黄历详情**（51万年历风格）
- 周历/月历切换
- **2026年节假日**标注

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
- 卡片式待办列表，计数徽章
- 增删勾、编辑、置顶（★）
- 时间前缀 `14:00 周会` + **到时托盘提醒**
- **查看全部**窗口（进行中 / 已完成 / 全部 + 搜索）
- 超过 5 条时「还有 N 条」提示；已完成历史可恢复
- 全局快捷键 `Ctrl+Shift+N` 快速添加

</td>
</tr>
</table>

### 🎨 可选模块（自由组合 + 排序）

| 模块 | 说明 |
|------|------|
| 📅 **倒数日** | 内置节日 + 自定义事件，带进度条 |
| 🍅 **番茄钟** | 25/5 专注计时，进度条 + 托盘提醒 |
| 📊 **年进度** | 全年已过百分比与可视化进度条 |
| 💬 **每日一句** | 离线语录库，按日随机展示 |
| 📝 **速记便签** | 多条便签、置顶/颜色标签、搜索与剪贴板导出 |

### 🎯 窗口与外观

- ✨ 无边框透明面板，四边/四角调整大小
- 🌓 深色/浅色主题切换
- 🎚️ 透明度（30-100%）+ 字号（10-16pt）滑块调节
- 📌 窗口置顶 / 鼠标穿透
- 🚀 开机自启动
- ⌨️ 全局快捷键：`Ctrl+Shift+D` 显示/隐藏，`Ctrl+Shift+N` 快速添加待办
- 🖱️ 右键菜单快捷操作

---

## 📸 截图预览

<div align="center">
  
  _(开发中，截图待补充)_
  
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

- Windows 10 / 11
- [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)

### 运行项目

```powershell
# 克隆项目
git clone https://github.com/cass-2003/Floatly.git
cd Floatly/DeskLite

# 运行开发版
dotnet run
```

### 发布 Release

```powershell
# 构建框架依赖版本（约 2-5 MB）
cd DeskLite
dotnet publish -c Release

# 输出目录
# DeskLite\bin\Release\net8.0-windows\publish\Floatly.exe
```

### 首次使用

1. 启动后图标会出现在系统托盘
2. 双击托盘图标显示主窗口
3. 右键托盘图标 → **设置** 进行个性化配置
4. 拖拽窗口边缘调整大小，拖拽内容区域移动位置

---

## 🛠️ 技术栈

<table>
<tr>
<td width="50%">

**核心框架**
- 🎯 **.NET 8** + **WPF** 无边框透明窗口
- 🖥️ **Windows Forms** 系统托盘集成
- 📦 单项目、单 exe，极简依赖

</td>
<td width="50%">

**数据服务**
- 🌤️ **Open-Meteo** 天气（免费、无需 API Key）
- 🗓️ **lunar-csharp** 农历 / 黄历计算
- 💾 本地 JSON 存储，仅 **1 个 NuGet** 依赖

</td>
</tr>
</table>

---

## 📂 数据存储

应用数据保存在 `%AppData%\Floatly\` 目录：

| 文件 | 说明 |
|------|------|
| `settings.json` | 窗口位置、主题、模块开关与顺序、城市等偏好配置 |
| `data.json` | 待办、倒数日、速记便签等业务数据 |

**💡 提示**：托盘菜单 → **导出数据备份** 可将 `data.json` 另存到桌面（`floatly-backup-日期.json`）。

> 从旧版 DeskLite 升级时，首次启动会自动将 `%AppData%\DeskLite\` 中的设置与数据迁移到 `%AppData%\Floatly\`。

---

## 🎛️ 托盘菜单

| 菜单项 | 快捷键 | 说明 |
|--------|--------|------|
| 显示/隐藏 | `Ctrl+Shift+D` | 切换主窗口可见性 |
| 设置... | - | 打开设置窗口 |
| 添加待办 | `Ctrl+Shift+N` | 弹出输入框快速添加 |
| 查看全部待办 | - | 打开待办管理窗口（历史 / 搜索 / 编辑） |
| 添加倒数日... | - | 新建自定义倒数事件 |
| 日历 → 回到今天 | - | 日历导航快捷跳转 |
| 日历 → 跳转日期... | - | 选择特定日期 |
| 导出数据备份 | - | 备份 `data.json` 到桌面 |
| 退出 | - | 关闭应用 |

---

## 🗺️ 路线图

以下功能在 [DESIGN.md](DESIGN.md) 中规划，**尚未实现**：

<table>
<tr>
<td width="50%">

**功能增强**
- ⏱️ 番茄钟、习惯打卡
- 👁️ 护眼提醒
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

</td>
</tr>
</table>

> 💡 详细设计、实现状态对照与体积/内存目标见 [DESIGN.md](DESIGN.md)

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
