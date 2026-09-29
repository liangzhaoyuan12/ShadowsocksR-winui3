# ShadowsocksR WinUI3

[English](README.md) | [简体中文](README.zh-CN.md)

一款基于 **WinUI 3** 与 **.NET 8** 的现代化 ShadowsocksR（SSR）Windows 代理客户端，拥有符合 Windows 11 设计语言的原生界面，轻量流畅，开箱即用。

## 功能特性

- **服务器管理** - 添加、编辑、排序、分组管理服务器，支持 SSR 链接导入与剪贴板分享，支持负载均衡与负载均衡分组
- **服务器订阅** - 支持订阅自动更新，一键批量导入节点
- **二维码支持** - 显示节点二维码、扫描屏幕二维码导入、自定义生成二维码
- **代理模式** - 系统代理一键开关，支持 PAC 模式与全局模式
- **智能分流** - 内置 GFWList、大陆白名单、大陆 IP 列表、局域网等多种 PAC 规则，支持自定义用户规则
- **端口转发** - 支持本地端口映射，将本地端口流量转发至远程服务器
- **本地代理** - 支持 SOCKS5 / HTTP 代理，可选局域网共享与访问认证
- **测速统计** - 节点连接统计、速度测试、服务器连接记录与历史峰值
- **系统托盘** - 后台常驻，原生托盘图标与右键菜单
- **实用功能** - 开机自启、在线更新检查、全局日志查看器
- **多语言界面** - 简体中文、繁體中文、English

## 运行环境

- Windows 10 1809（17763）及以上，推荐 Windows 11
- 从源码构建需要：
  - Visual Studio 2022（17.8 或更高版本）
  - .NET 8 SDK
  - VS 工作负载：**.NET 桌面开发** 和 **Windows 应用开发**（WinUI / Windows App SDK 工具）

## 从源码构建

```powershell
git clone https://github.com/liangzhaoyuan12/ShadowsocksR-winui3.git
cd ShadowsocksR-winui3
```

用 Visual Studio 打开 `ShadowsocksR-winui3.sln` 直接生成，或在开发者命令提示符中执行：

```powershell
msbuild ShadowsocksR-winui3.sln /restore /p:Configuration=Release /p:Platform=x64
```

支持平台：`x86`、`x64`、`ARM64`。

## 打包（MSIX）

本项目为单项目 MSIX 应用，生成可安装的 `.msix`：

```powershell
msbuild ShadowsocksR-winui3.csproj /p:Configuration=Release /p:Platform=x64 `
  /p:GenerateAppxPackageOnBuild=true /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly
```

生成的安装包位于 `AppPackages\` 目录。

> **注意：** 签名证书（`*.pfx`）有意未提交到仓库。如果打包时提示缺少证书，请在 Visual Studio 项目 **属性 > 打包和发布 > 打包 > 选择证书** 中新建测试证书。

## 项目结构

| 目录 | 说明 |
| --- | --- |
| `Controller/` | 核心控制器（系统代理、PAC、监听、更新） |
| `Model/` | 数据模型（服务器、配置） |
| `Encryption/`、`Obfs/` | SSR 加密与混淆实现 |
| `View/`、`Views/` | 托盘图标、辅助窗口与 WinUI 3 页面 |
| `Data/`、`Resources/` | PAC 文件、翻译与嵌入资源 |
| `3rd/` | 第三方代码（SimpleJson、ZXing、DNS） |

## 开源协议

基于 [GNU General Public License v3.0](LICENSE) 开源。

## 免责声明

本项目仅供学习与研究使用，请遵守所在国家或地区的法律法规。
