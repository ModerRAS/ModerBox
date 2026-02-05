# ModerBox

<p align="center">
  <img src="ModerBox/Assets/avalonia-logo.png" alt="ModerBox Logo" width="150"/>
</p>

<p align="center">
  <a href="https://github.com/ModerRAS/ModerBox/actions/workflows/dotnet.yaml">
    <img src="https://github.com/ModerRAS/ModerBox/actions/workflows/dotnet.yaml/badge.svg" alt="Build Status">
  </a>
</p>

<p align="center">
  <strong>一款现代化、跨平台的电力系统工具箱</strong>
  <br />
  <br />
  <a href="https://github.com/ModerRAS/ModerBox/releases">查看最新版本</a>
  ·
  <a href="https://github.com/ModerRAS/ModerBox/issues">报告问题</a>
</p>

---

**ModerBox** 是一款专为电力系统工程师和技术人员打造的强大桌面工具箱。它采用 [Avalonia](https://avaloniaui.net/) 框架构建，拥有现代化的 Fluent Design 用户界面，并提供流畅的跨平台体验。

本工具箱集成了多种实用功能，旨在简化和自动化复杂的工程数据处理任务，尤其是针对 **COMTRADE** 格式的录波文件分析。

## ✨ 主要功能

*   ⚡️ **批量谐波分析**:
    *   能够递归式地批量处理指定文件夹下的所有 COMTRADE 录波文件。
    *   自动分析文件中每一个模拟量通道的谐波数据。
    *   支持按"周波"或"采样点"两种模式进行高精度计算。
*   📊 **一键导出报告**:
    *   将海量的计算结果一键导出为结构化的 Excel 表格，极大地方便了后续的深度分析和报告撰写。
*   🖥️ **现代化 & 跨平台**:
    *   基于 Avalonia UI，完美支持 Windows、macOS 和 Linux。
    *   界面设计遵循 Fluent Design，美观、直观且易于使用。

*   🧰 **滤波器分合闸波形检测（流式 & 增量续跑）**:
  *   扫描 COMTRADE `*.cfg`，CFG 预过滤后按需加载 DAT，降低无效 I/O。
  *   计算结果流式写入 SQLite，最终从 SQLite 流式导出 Excel，显著降低内存峰值。
  *   内置 processed_files（已读文件表），支持断点/增量续跑：已处理的录波自动跳过。
  *   说明文档：`Docs/滤波器分合闸波形检测-流式SQLite与增量处理.md`

*   🔌 **电缆走向自动化绘制**:
  *   根据配置文件自动规划最优电缆路由，采用 Dijkstra 算法计算观测点间最短路径。
  *   智能生成 L 型正交连接，支持穿管点成对配置。
  *   在终点附近自动放置业务统计表格，支持碰撞检测和位置优先级调整。
  *   生成高质量 PNG/JPG 输出，支持自定义底图和样式。
  *   说明文档：`Docs/电缆走向绘制功能文档.md`

## 🚀 如何开始

使用 ModerBox 非常简单：

1.  前往本仓库的 **[Releases 页面](https://github.com/ModerRAS/ModerBox/releases)**。
2.  下载最新版本的软件包（例如 `ModerBox-win-x64.zip`）。
3.  解压后，直接运行 `ModerBox.exe` 即可启动！

## 🛠️ 技术栈

*   **[.NET](https://dotnet.microsoft.com/)**
*   **[C#](https://docs.microsoft.com/en-us/dotnet/csharp/)**
*   **[Avalonia UI](https://avaloniaui.net/)**: 一个用于 .NET 的跨平台 UI 框架。

## 📄 许可证

本项目采用 [GPL-3.0 License](LICENSE) 许可协议。详情请参阅 `LICENSE` 文件。