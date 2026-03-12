# ModerBox - Main Avalonia UI Application

## OVERVIEW
Main desktop application - cross-platform UI using Avalonia 11.3.x with ReactiveUI MVVM pattern.

## STRUCTURE
```
ModerBox/
├── Program.cs          # Entry point
├── App.axaml.cs        # Application bootstrap
├── Env.cs              # Environment config
├── ViewLocator.cs      # View resolution
├── ViewModels/
│   ├── ViewModelBase.cs      # Base class (ReactiveUI)
│   ├── MainWindowViewModel.cs # Navigation + Pages collection
│   ├── HomePageViewModel.cs
│   ├── HarmonicCalculateViewModel.cs
│   ├── FilterWaveformSwitchCopyViewModel.cs
│   ├── FilterWaveformSwitchIntervalViewModel.cs
│   ├── CurrentDifferenceAnalysisViewModel.cs
│   ├── NewCurrentDifferenceAnalysisViewModel.cs
│   ├── ThreePhaseIdeeAnalysisViewModel.cs
│   ├── ComtradeExportViewModel.cs
│   ├── CableRoutingViewModel.cs
│   ├── PeriodicWorkViewModel.cs
│   ├── QuestionBankConversionViewModel.cs
│   └── WaveformViewModelBase.cs
└── Views/
    ├── MainWindow.axaml.cs
    └── UserControls/
        ├── HomePage.axaml.cs
        ├── HarmonicCalculate.axaml.cs
        ├── FilterWaveformSwitchCopy.axaml.cs
        ├── FilterWaveformSwitchInterval.axaml.cs
        ├── CurrentDifferenceAnalysis.axaml.cs
        ├── NewCurrentDifferenceAnalysis.axaml.cs
        ├── ThreePhaseIdeeAnalysis.axaml.cs
        ├── ComtradeExport.axaml.cs
        ├── CableRouting.axaml.cs
        ├── PeriodicWork.axaml.cs
        └── QuestionBankConversion.axaml.cs
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Add new page | ViewModels/{Feature}ViewModel.cs + Views/UserControls/{Feature}.axaml.cs |
| Modify navigation | MainWindowViewModel.Pages |
| Page base class | WaveformViewModelBase.cs |

## CONVENTIONS
- ViewModels: inherit `ViewModelBase`, use `RaiseAndSetIfChanged`
- Commands: `ReactiveCommand<Unit, Unit>`
- Navigation: Pages collection in MainWindowViewModel
- Each feature: one ViewModel + one UserControl pair

## KEY CLASSES
| Class | Role |
|-------|------|
| ViewModelBase | ReactiveUI base with RaiseAndSetIfChanged |
| MainWindowViewModel | Manages Pages, navigation |
| ViewLocator | Resolves View from ViewModel name |
