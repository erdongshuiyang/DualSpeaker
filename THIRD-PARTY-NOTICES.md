# Third-party notices

The DualSpeaker application code is released under the MIT license in `LICENSE`. Third-party components retain their own copyright and license terms.

| Component | Version / source | License | Included notice |
| --- | --- | --- | --- |
| NAudio.Core, NAudio.Wasapi, NAudio.WinMM | 2.2.1, [NAudio](https://github.com/naudio/NAudio) / NuGet | MIT | [licenses/NAudio-LICENSE.txt](licenses/NAudio-LICENSE.txt) |
| Windows process audio loopback API reference | [Microsoft ApplicationLoopback sample](https://github.com/microsoft/Windows-classic-samples/tree/main/Samples/ApplicationLoopback) | MIT | [licenses/Microsoft-sample-LICENSE.txt](licenses/Microsoft-sample-LICENSE.txt) |
| Fusion Pixel Font, 12px proportional zh_hans | 2026.10.16, [Fusion Pixel Font](https://github.com/TakWolf/fusion-pixel-font) | SIL OFL 1.1 | [fonts/OFL.txt](fonts/OFL.txt), [fonts/README.md](fonts/README.md) and component notices in `fonts/LICENSES/` |

The bundled font is unmodified and loaded privately by this process. System fonts such as Script MT Bold, Brush Script MT, Segoe Script, Segoe UI and Consolas are used only when installed; those font files are not distributed by this project. Logo appearance can vary with installed fonts.

NAudio packages are downloaded from NuGet at build time with fixed versions and package SHA-256 verification. Runtime DLLs are included in portable releases with their MIT notice. No GitHub CLI, Git executable or development credentials are part of the source or runtime distribution.
