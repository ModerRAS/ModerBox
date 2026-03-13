# ModerBox.Cli - Command-Line Interface

## OVERVIEW
CLI version for automation and server environments. Uses Spectre.Console for interactive UI.

## STRUCTURE
```
ModerBox.Cli/
├── ModerBox.Cli.csproj   # net10.0, Spectre.Console
├── Program.cs            # Main entry (interactive/command mode)
├── README.md            # CLI documentation
└── Commands/
    ├── HarmonicCommand.cs           # Batch harmonic analysis
    ├── FilterWaveformCommand.cs     # Filter waveform detection
    ├── CurrentDifferenceCommand.cs  # Current difference analysis
    ├── QuestionBankCommand.cs       # Question bank conversion
    └── CableRoutingCommand.cs       # Cable routing
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add new command | Commands/{Feature}Command.cs |
| Modify help text | Program.cs |

## CONVENTIONS
- Commands: static class with `RunAsync(string[]? args)` method
- Return: `int` (0=success, 1=error)
- Use `Spectre.Console` for UI
- Always handle null args: `args ??= []`

## KEY CLASSES
| Class | Role |
|-------|------|
| Program | Entry point, mode routing |
| HarmonicCommand | Harmonic calculation |
| FilterWaveformCommand | Filter waveform detection |
