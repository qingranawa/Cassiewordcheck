; CassieWordCheck — Inno Setup 6 安装脚本
; 配合 GitHub Actions CI/CD 使用，打 tag 后自动编译打包上传

#define MyAppName "CASSIE CWC Tool"
#define MyAppPublisher "清然"
#define MyAppURL "https://github.com/qingranawa/Cassiewordcheck"
#define MyAppExeName "CassieWordCheck.exe"

; 版本号从命令行传入（/dMyAppVersion=x.y.z），默认 2.4.3
#ifndef MyAppVersion
  #define MyAppVersion "2.4.3"
#endif

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=CASSIE-CWC-Tool-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
DisableWelcomePage=no
CloseApplications=yes
SetupIconFile=data\AAA.ico
UninstallDisplayIcon={app}\CassieWordCheck.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
; 注：中文界面由应用自身提供，安装器用英文界面即可
; 需要简体中文时可从 https://github.com/jrsoftware/issrc 下载 ChineseSimplified.isl

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式(&D)"; GroupDescription: "快捷方式："; Flags: checkedonce

[Files]
Source: "dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "data\AAA.ico"; DestDir: "{app}\data"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 CASSIE CWC Tool"; Flags: postinstall nowait skipifsilent shellexec
