# Floatly Release Notes 模板（中文）

> **政策**：自 v1.0.1 起，所有 GitHub Release 说明文字均使用简体中文撰写。  
> 发布新版本时，复制下方模板，替换版本号与具体内容后，用 `gh release create` 或 `gh release edit` 上传。

---

## 模板

```markdown
## Floatly vX.Y.Z

### 新增
- （本版本新功能，每条一行）

### 改进
- （体验优化、性能提升、UI 调整等）

### 修复
- （Bug 修复）

### 安装说明
- 需要 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0)
- 或下载 **Floatly-Setup-x.y.z.exe** 安装包（自动检测运行时）
- 绿色版：**Floatly-x.y.z-win-x64.zip**（需自行安装运行时）

### 升级说明
- （如有数据迁移、设置变更、破坏性改动，在此说明；无则删除本节）
```

---

## 发布命令示例

```powershell
# 新建 Release
gh release create vX.Y.Z `
  --repo cass-2003/Floatly `
  --title "Floatly vX.Y.Z" `
  --notes-file docs/releases/vX.Y.Z.md `
  release/Floatly-Setup-X.Y.Z.exe `
  release/Floatly-X.Y.Z-win-x64.zip

# 仅更新说明文字
gh release edit vX.Y.Z --repo cass-2003/Floatly --notes-file docs/releases/vX.Y.Z.md
```

---

## 撰写要点

1. **面向用户**：说明「能做什么」「修了什么」，避免堆砌 commit hash。
2. **分类清晰**：新增 / 改进 / 修复 三栏；安装说明每版保留。
3. **版本一致**：标题、安装包文件名、Tag 三者版本号对齐。
4. **中文为主**：专有名词（Floatly、.NET、WPF）可保留英文。

---

## 历史版本存档

| 版本 | 说明文件 |
|------|----------|
| v2.0.2 | [releases/v2.0.2.md](releases/v2.0.2.md) |
| v2.0.1 | [releases/v2.0.1.md](releases/v2.0.1.md) |
| v2.0.0 | [releases/v2.0.0.md](releases/v2.0.0.md) |
| v1.0.1 | [releases/v1.0.1.md](releases/v1.0.1.md) |
