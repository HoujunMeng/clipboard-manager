; ────────────────────────────────────────────
; 历史粘贴板 — Inno Setup 安装脚本
;
; 用法：
;   1. 先执行 dotnet publish 生成发布文件
;   2. 用 Inno Setup Compiler 打开此脚本编译
;
; 生成文件：Output/ClipboardManager-Setup-1.0.0.exe
; ────────────────────────────────────────────

#define AppName        "历史粘贴板"
#define AppEnglishName "ClipboardManager"
#define AppVersion     "1.0.0"
#define AppPublisher   "HoujunMeng"
#define AppURL         "https://github.com/HoujunMeng/clipboard-manager"
#define AppExeName     "ClipboardManager.exe"
#define SourceDir      "..\publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppEnglishName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=..\Output
OutputBaseFilename=ClipboardManager-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; 64-bit only
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; 安装器界面语言
[Languages]
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

; 安装的文件（从 dotnet publish 输出目录）
[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 开始菜单快捷方式
[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\卸载 {#AppName}"; Filename: "{uninstallexe}"

; 注册表：开机自启（默认值，用户可在设置中更改）
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#AppEnglishName}"; \
    ValueData: """{app}\{#AppExeName}"""; \
    Flags: uninsdeletevalue

; 执行后运行应用
[Run]
Filename: "{app}\{#AppExeName}"; \
    Description: "启动 {#AppName}"; \
    Flags: nowait postinstall skipifsilent
