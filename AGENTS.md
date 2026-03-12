# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-12
**Commit:** 9b9f588

## OVERVIEW
Cross-platform power system toolbox (电力系统工具箱) built with Avalonia UI (.NET 10), focusing on COMTRADE waveform file analysis.

## STRUCTURE
```
ModerBox.sln           # 24 projects
├── ModerBox/          # Main Avalonia UI app (MVVM + ReactiveUI)
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
| Run tests | `dotnet test` |

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
dotnet run --project ModerBox/ModerBox.csproj

# Publish (Native AOT)
dotnet publish ModerBox/ModerBox.csproj -c Release -r win-x64 -p:PublishAot=true
```

## NOTES
- GBK encoding support for COMTRADE files
- Uses Velopack for auto-updates
- CI: GitHub Actions with nightly releases to R2 storage
