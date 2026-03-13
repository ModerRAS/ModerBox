# ModerBox.Comtrade - COMTRADE File Parsing Core

## OVERVIEW
IEC 60255-24:2013 / IEEE Std C37.111-2013 COMTRADE waveform file parser. Supports ASCII/BINARY/FLOAT32 formats, auto-detects encoding (UTF-8/GBK).

## STRUCTURE
```
ModerBox.Comtrade/
├── Comtrade.cs           # Main parser (808 lines)
├── ComtradeInfo.cs       # Data model
├── AnalogInfo.cs         # Analog channel model
└── DigitalInfo.cs       # Digital channel model
```

## WHERE TO LOOK
| Task | Location |
|------|----------|
| Parse COMTRADE | Comtrade.cs |
| Data model | ComtradeInfo.cs |

## CONVENTIONS
- Static methods: `Comtrade.ReadComtradeAsync()`, `Comtrade.ReadComtradeCFG()`
- Encoding auto-detection: UTF-8/GBK fallback
- Async-first API

## KEY METHODS
| Method | Description |
|--------|-------------|
| `ReadComtradeAsync(cfgFilePath, loadDat)` | Full read (CFG + DAT) |
| `ReadComtradeCFG(cfgFilePath, allocateDataArrays)` | CFG only |
| `DetectEncoding(filePath)` | Auto-detect file encoding |

## NOTES
- 808 lines - largest single file
- Supports: ASCII, BINARY, BINARY32, FLOAT32 formats
