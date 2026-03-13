# ModerBox.MCP - Model Context Protocol Server

## OVERVIEW
MCP server providing all ModerBox functionality as tools for AI clients. Uses stdio transport.

## STRUCTURE
```
ModerBox.MCP/
├── ModerBox.MCP.csproj   # net10.0
└── Tools/
    ├── HarmonicTools.cs
    ├── FilterWaveformTools.cs
    ├── CurrentDifferenceTools.cs
    ├── ThreePhaseIdeeTools.cs
    ├── QuestionBankTools.cs
    ├── ComtradeExportTools.cs
    ├── CableRoutingTools.cs
    ├── VideoAnalysisTools.cs
    └── PeriodicWorkTools.cs
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add new tool | Tools/{Feature}Tools.cs |
| Tool registration | Class with [McpServerToolType] |

## CONVENTIONS
- Tools: class with `[McpServerToolType]` attribute
- Methods: `[McpServerTool]` + `[Description]`
- Async-first

## ADDING NEW TOOLS
1. Create `ModerBox.MCP/Tools/{Feature}Tools.cs`
2. Add class with `[McpServerToolType]` attribute
3. Add methods with `[McpServerTool]` + `[Description]`
4. Build: `dotnet build ModerBox.MCP/ModerBox.MCP.csproj`

## AVAILABLE TOOLS
| Tool | Description |
|------|-------------|
| analyze_harmonic | Analyze harmonic from COMTRADE |
| filterwaveform_detect | Detect filter switch events |
| analyze_current_difference | Ground electrode current diff |
| convert_questionbank | Question bank format conversion |
