# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-13
**Commit:** 9b9f588

## OVERVIEW
Cross-platform power system toolbox (电力系统工具箱) built with Avalonia UI (.NET 10), focusing on COMTRADE waveform file analysis.

## STRUCTURE
```
ModerBox.sln           # 25 projects
├── ModerBox/          # Main Avalonia UI app (MVVM + ReactiveUI)
├── ModerBox.MCP/      # MCP Server (Model Context Protocol)
├── ModerBox.Cli/      # Command-line interface
├── ModerBox.Common/   # Utilities (DynamicTable, DataWriter, FileHelper)
├── ModerBox.Comtrade/ # COMTRADE file parsing core
├── ModerBox.Comtrade.*/ # Feature modules (Harmonic, FilterWaveform, CurrentDifference, PeriodicWork, Export, CableRouting)
├── ModerBox.UIAutomation.*/ # Android UI automation
├── ModerBox.QuestionBank/  # Question bank converter
└── *.Test/            # Unit tests (MSTest)
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add new feature | ModerBox/ViewModels/ + ModerBox/Views/UserControls/ |
| COMTRADE parsing | ModerBox.Comtrade/Comtrade.cs |
| Excel export | ModerBox.Common/DataWriter.cs |
| Harmonic analysis | ModerBox.Comtrade.Harmonic/ |
| Cable routing | ModerBox.CableRouting/ |
| MCP server tools | ModerBox.MCP/Tools/ |
| Run tests | `dotnet test` |

## MCP SERVER (ModerBox.MCP)
MCP (Model Context Protocol) server providing all ModerBox functionality as tools for AI clients.

### Running MCP Server
```bash
# Run MCP server (stdio transport)
dotnet run --project ModerBox.MCP/ModerBox.MCP.csproj

# Or build and run
dotnet build ModerBox.MCP/ModerBox.MCP.csproj -c Release
./ModerBox.MCP/bin/Release/net10.0/ModerBox.MCP
```

### Available MCP Tools
| Tool | Description |
|------|-------------|
| **Harmonic** | |
| `analyze_harmonic` | Analyze harmonic data from COMTRADE files |
| **FilterWaveform** | |
| `filterwaveform_detect` | Detect filter switch events with streaming |
| `filterwaveform_copy` | Filter and copy COMTRADE files by date/channel |
| `switchoperation_report` | Generate switch operation report |
| **CurrentDifference** | |
| `analyze_current_difference` | Analyze ground electrode current difference |
| `generate_current_difference_chart` | Generate line chart from results |
| **ThreePhaseIdee** | |
| `analyze_threephase_idee` | Analyze three-phase IDEE (\|IDEE1-IDEE2\| peak) |
| `analyze_threephase_idee_idel` | Analyze three-phase IDEE (\|IDEE1-IDEL1\| peak) |
| **QuestionBank** | |
| `convert_questionbank` | Convert question bank between formats |
| `detect_questionbank_format` | Auto-detect question bank format |
| **ComtradeExport** | |
| `export_comtrade_channels` | Export selected channels |
| `list_comtrade_channels` | List available channels |
| **CableRouting** | |
| `execute_cable_routing` | Execute cable routing from config |
| `create_cable_routing_config` | Create sample config |
| `load_cable_routing_config` | Load config from JSON |
| **VideoAnalysis** | |
| `analyze_video` | Analyze single video |
| `analyze_video_folder` | Batch analyze videos |
| **PeriodicWork** | |
| `execute_periodic_work` | Execute periodic work analysis |

### Adding New MCP Tools
1. Create new file in `ModerBox.MCP/Tools/{Feature}Tools.cs`
2. Add `[McpServerToolType]` class with `[McpServerTool]` methods
3. Use `[Description]` attribute for tool documentation
4. Build: `dotnet build ModerBox.MCP/ModerBox.MCP.csproj`

## CODE MAP
| Symbol | Type | Location |
|--------|------|----------|
| Comtrade | class | ModerBox.Comtrade/Comtrade.cs |
| DataWriter | class | ModerBox.Common/DataWriter.cs |
| DynamicTable<T> | class | ModerBox.Common/DynamicTable.cs |
| ViewModelBase | class | ModerBox/ViewModels/ViewModelBase.cs |
| MainWindowViewModel | class | ModerBox/ViewModels/MainWindowViewModel.cs |

## CONVENTIONS
- **Namespace**: `ModerBox.{Module}.{Feature}` (e.g., `ModerBox.Comtrade.Harmonic`)
- **ViewModels**: Inherit `ViewModelBase` (ReactiveUI)
- **Extension methods**: `{CoreClass}{Feature}Extension.cs`
- **Test naming**: `{ClassName}Test.cs`, method: `Method_Scenario_Expected`
- **Test data**: `TestData/` folder with `<CopyToOutputDirectory>`

## ANTI-PATTERNS (THIS PROJECT)
- NONE FOUND - Clean codebase, no TODO/FIXME/HACK comments

## COMMANDS
```bash
# Build
dotnet build ModerBox.sln

# Test
dotnet test

# Run app
dotnet run --project ModerBox/ModerBox.Cli/ModerBox.Cli.csproj

# Run MCP server
dotnet run --project ModerBox.MCP/ModerBox.MCP.csproj

# Run CLI in interactive mode
dotnet run --project ModerBox.Cli/ModerBox.Cli.csproj

# Run CLI with command
dotnet run --project ModerBox.Cli/ModerBox.Cli.cs清单 -- harmonic --source "C:\data"

# Publish (Native AOT)
dotnet publish ModerBox/ModerBox.csproj -c Release -r win-x64 -p:PublishAot=true

# Publish CLI
dotnet publish ModerBox.Cli/ModerBox.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## MODERBOX.CLI
Command-line interface for automation and server environments.

### Available Commands
| Command | Description |
|---------|-------------|
| `harmonic`, `h` | Batch harmonic analysis |
| `filter`, `f` | AC filter waveform detection with streaming |
| `current-diff`, `cd` | Ground electrode current difference analysis |
| `question-bank`, `qb` | Question bank format conversion |
| `cable`, `c` | Cable routing drawing |

### Example
```bash
# Interactive mode
dotnet run --project ModerBox.Cli

# Command mode
dotnet run --project ModerBox.Cli -- harmonic --source "C:\data" --target "result.xlsx"
```

## NOTES
- GBK encoding support for COMTRADE files
- Uses Velopack for auto-updates
- CI: GitHub Actions with nightly releases to R2 storage
