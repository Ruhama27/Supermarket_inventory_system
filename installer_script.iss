; Inno Setup Script for Stock-Sync Supermarket Inventory & POS
; Download Inno Setup free at: https://jrsoftware.org/isdl.php

#define MyAppName "Stock-Sync"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Stock-Sync Retail Solutions"
#define MyAppURL "https://github.com/Ruhama27/Supermarket_inventory_system"
#define MyAppExeName "Stock_Sync.exe"

[Setup]
AppId={{D37F21F6-3E12-421A-9E9C-A4E26A96E781}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=README.md
OutputDir=dist
OutputBaseFilename=StockSync_Setup_v1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "Stock_Sync\Stock_Sync\bin\Release\Stock_Sync.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "Stock_Sync\Stock_Sync\bin\Release\Stock_Sync.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "setup_database.sql"; DestDir: "{app}"; Flags: ignoreversion
Source: "upgrade_database.sql"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
