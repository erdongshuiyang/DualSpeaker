# 架构与验证范围

## 音频路径

`ProcessAudioCapture` 通过 Windows 进程音频回环接口采集其他应用的声音，排除本进程树，按 48 kHz 双声道送入共享 `AudioRing`。`OutputProvider` 为每台音响独立读取缓冲，处理软件增益、主音量、静音、左右分配、延迟与小幅重采样，再由 `WasapiOut` 输出。

系统默认输出会暂时切换到选定的电脑音源端点，并使该端点保持静音。停止时通过 `DefaultAudio` 恢复启动前状态；CLI 监视进程为异常退出保留一次恢复途径。恢复仍依赖原设备可用，不能保证断开设备后的默认状态完全复原。

## 模块

| 文件 | 职责 |
| --- | --- |
| `src/AudioEngine.cs` | 配置、设备枚举、音源缓冲、输出、系统音频恢复 |
| `src/ProcessAudioCapture.cs` | 进程音频回环激活与采集 |
| `src/Calibration.cs`、`src/RawMicrophone.cs` | 测试信号测量、麦克风采集与复测 |
| `src/MediaPlaybackPresence.cs` | 静音媒体会话与已验证的播放器兼容处理 |
| `src/MediaVolumeKeys.cs`、`src/VolumeOverlay.cs` | 标准音量键、可配置步长与浮窗 |
| `src/MainForm*.cs` | 界面布局、用户操作与状态 |
| `src/RetroControls.cs`、`src/Studio*.cs` | 金属面板、旋钮、仪表、字标与局部动画 |
| `src/HighDpi.cs`、`src/UniformConsoleLayout.cs` | DPI 与整页等比缩放 |
| `src/PixelTypography.cs` | 私有加载像素中文字体 |
| `src/SignalChecks.cs`、`src/CommandLine.cs` | CLI、纯算法检查与硬件诊断 |

## 校准与漂移

麦克风校准用两组不同频段的测试信号识别左右设备，通过带通滤波与归一化互相关估算时间差。需要足够有效测量且结果稳定，才应用补偿；之后重新录音验证，两段复测的中位残差都在 15 ms 以内才显示通过。

这包含当前位置的声音传播距离。通过并不构成任意场景、任意时长或所有设备上的同步精度保证。平时播放不持续录音；缓冲反馈和小幅重采样能约束软件时钟漂移，却不能观测音响后来新增的内部缓存。

## 数据与联网

运行时数据位于 EXE 所在目录：`settings.json`、`restore-audio.json`、`live-status.json`、`application.log`，以及按需产生的 `diagnostics/` 和测量报告。它们不作为版本库输入，也不由发布脚本打包。应用代码不含联网、遥测或上传逻辑。

构建脚本只为恢复固定版本的 NAudio 包访问 NuGet；包使用 SHA-256 校验。中文字体和许可证保留在源码仓库，英文字体使用操作系统已安装版本。

## 验证边界

- `--selftest`：合成信号上的校准算法检查，不访问真实麦克风或音响。
- `--volume-selftest`：总音量、双路比例、静音保留与按键步长逻辑，不访问真实音响。
- GitHub Actions：Windows 编译、上述两项检查、白名单打包。CI 不模拟真实蓝牙连接。
- 实际听音、两路同步、休眠重连、播放器兼容与系统声音恢复，需要真实 Windows 桌面和设备验证。

CLI 还包含用于开发的硬件诊断命令；其中一些会播放测试音、改变声音设置或录音。请阅读对应实现后在专门测试环境中使用，不将它们作为日常操作或无硬件 CI 的默认步骤。
