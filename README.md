# ModerBox
![Build Status](https://github.com/ModerRAS/ModerBox/actions/workflows/dotnet.yaml/badge.svg)

自用工具箱

## 现有功能
1. 针对Comtrade格式的录波内容的批量谐波计算
    - 可以递归计算整个文件夹内所有的录波文件的每个模拟量通道的谐波
    - 可以计算单个文件中每个通道谐波的最大值
    - 可选计算精度为每个点还是每个周波
    - 将计算内容导出为Excel文件方便后面分析