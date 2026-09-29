# ShadowsocksR WinUI3

[English](README.md) | [简体中文](README.zh-CN.md)

A modern ShadowsocksR (SSR) proxy client for Windows, built with **WinUI 3** and **.NET 8**, featuring a native interface that matches the Windows 11 design language. Lightweight, smooth, and ready to use out of the box.

## Download

**This app is available on the Microsoft Store.** Download and install it from the Store:

<a href="https://get.microsoft.com/installer/download/9N0LBTN9Z8TL?referrer=appbadge" target="_self" >
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

> **Note:** Release builds are **not** published as GitHub Releases. The Microsoft Store is the only official distribution channel.

## Features

- **Server management** - Add, edit, reorder, and group servers; import via SSR links or clipboard; load balancing and balance-in-group strategies
- **Server subscriptions** - Automatic subscription updates and one-click bulk node import
- **QR code support** - Display node QR codes, scan QR codes from the screen, and generate custom QR codes
- **Proxy modes** - One-click system proxy toggle with PAC mode and global mode
- **Smart routing** - Built-in PAC rules for GFWList, China whitelist, China IP list, LAN, and more, with custom user rules
- **Port forwarding** - Forward local port traffic to remote servers
- **Local proxy** - SOCKS5 / HTTP proxy with optional LAN sharing and authentication
- **Speed and statistics** - Connection statistics, speed tests, server logs, and peak speed records
- **System tray** - Runs in the background with a native tray icon and menu
- **Utilities** - Start with Windows, in-app update check, and a global log viewer
- **Multi-language UI** - Simplified Chinese, Traditional Chinese, and English

## Requirements

- Windows 10 1809 (17763) or later, Windows 11 recommended
- To build from source:
  - Visual Studio 2022 (17.8 or later)
  - .NET 8 SDK
  - VS workloads: **.NET desktop development** and **Windows application development** (WinUI / Windows App SDK tooling)

## Build from source

```powershell
git clone https://github.com/liangzhaoyuan12/ShadowsocksR-winui3.git
cd ShadowsocksR-winui3
```

Open `ShadowsocksR-winui3.sln` in Visual Studio and build, or build from a Developer Command Prompt:

```powershell
msbuild ShadowsocksR-winui3.sln /restore /p:Configuration=Release /p:Platform=x64
```

Supported platforms: `x86`, `x64`, `ARM64`.

## Package (MSIX)

This is a single-project MSIX app. To produce an installable `.msix`:

```powershell
msbuild ShadowsocksR-winui3.csproj /p:Configuration=Release /p:Platform=x64 `
  /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly
```

The package is written to `AppPackages\`.

> **Note:** The signing certificate (`*.pfx`) is intentionally not committed to the repository. If packaging fails because the certificate is missing, create a new test certificate in Visual Studio under project **Properties > Package and Publish > Packaging > Choose Certificate**.

## Project layout

| Directory | Description |
| --- | --- |
| `Controller/` | Core controller (system proxy, PAC, listeners, updates) |
| `Model/` | Data models (servers, configuration) |
| `Encryption/`, `Obfs/` | SSR encryption and obfuscation implementations |
| `View/`, `Views/` | Tray icon, helper windows, and WinUI 3 pages |
| `Data/`, `Resources/` | PAC files, translations, and embedded resources |
| `3rd/` | Third-party code (SimpleJson, ZXing, DNS) |

## License

Licensed under the [GNU General Public License v3.0](LICENSE).

## Disclaimer

This project is for learning and research purposes only. Please comply with the laws and regulations of your country or region when using it.
