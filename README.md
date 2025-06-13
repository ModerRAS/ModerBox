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