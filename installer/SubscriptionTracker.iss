; Inno Setup script for the Windows installer of Subscription Tracker.
; Built automatically by GitHub Actions (.github/workflows/release.yml):
;   iscc /DMyAppVersion=1.2.0 /DSourceDir=..\publish\windows installer\SubscriptionTracker.iss

#define MyAppName "Subscription Tracker"
#define MyAppExeName "SubscriptionTracker.exe"
#ifndef MyAppVersion
  #define MyAppVersion "1.2.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\windows"
#endif

[Setup]
AppId={{8F6C2B1E-4D3A-4C8B-9E2F-7A1D5B6C3E90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=KaByH-xd
AppPublisherURL=https://github.com/KaByH-xd/Subscription_Tracking-Analysis_app
AppSupportURL=https://github.com/KaByH-xd/Subscription_Tracking-Analysis_app/issues
; Per-user install: no administrator rights needed
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\SubscriptionTracker
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=SubscriptionTracker-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
CloseApplications=force

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Background mode (on by default, can be turned off in the app's Settings):
; the app starts without a window at sign-in and shows payment reminders on time.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "SubscriptionTracker"; ValueData: """{app}\{#MyAppExeName}"" --background"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the background process before the files are removed
Filename: "{sys}\taskkill.exe"; Parameters: "/im {#MyAppExeName} /f"; Flags: runhidden; RunOnceId: "StopApp"
