; -- GRF Editor --

[Setup]
AppName=GRF Editor
AppVersion=1.8.4.8
DefaultDirName={pf}\GRF Editor
DefaultGroupName=GRF Editor
UninstallDisplayIcon={app}\GRF Editor.exe
Compression=lzma2
SolidCompression=yes
OutputDir=C:\Users\Tokei\Desktop\Releases\GRF Editor
OutputBaseFilename=GRF Editor Installer
WizardImageFile=setupBackground.bmp
DisableProgramGroupPage=yes
ChangesAssociations=yes
DisableDirPage=no
DisableWelcomePage=no

[Files]
Source: "GrfCL\bin\Release\GrfCL.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "GrfCL\bin\Release\GrfCL.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "GRFEditor\bin\Release\GRF Editor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "GRFEditor\bin\Release\GRF Editor.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "GRFEditor\Resources\app.ico"; DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
Type: files; Name: "{app}\gpf.ico"
Type: files; Name: "{app}\grf.ico"
Type: files; Name: "{app}\rgz.ico"
Type: files; Name: "{app}\grfkey.ico"
Type: files; Name: "{app}\crash.log"
Type: files; Name: "{app}\debug.log"
Type: filesandordirs; Name: "{app}\tmp"
Type: files; Name: "{userappdata}\GRF Editor\gpf.ico"
Type: files; Name: "{userappdata}\GRF Editor\grf.ico"
Type: files; Name: "{userappdata}\GRF Editor\rgz.ico"
Type: files; Name: "{userappdata}\GRF Editor\grfkey.ico"
Type: files; Name: "{userappdata}\GRF Editor\crash.log"
Type: files; Name: "{userappdata}\GRF Editor\debug.log"
Type: filesandordirs; Name: "{userappdata}\GRF Editor\~tmp"

[Icons]
Name: "{group}\GRF Editor"; Filename: "{app}\GRF Editor.exe"
Name: "{commondesktop}\GRF Editor"; Filename: "{app}\GRF Editor.exe"

[CustomMessages]
DotNetMissing=GRF Editor requires .NET Framework 3.5 Client Profile or higher (SP1). Do you want to download it? Setup will now exit!

[Code]
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, release, serviceCount: cardinal;
    check45, success: boolean;
begin
    // .NET 4.5 installs as update to .NET 4.0 Full
    if version = 'v4.5' then begin
        version := 'v4\Full';
        check45 := true;
    end else
        check45 := false;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0/4.5 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 uses additional value Release
    if check45 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= 378389);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;


function InitializeSetup(): Boolean;
var ErrorCode: Integer;
begin
    if not (IsDotNetDetected('v4\Client', 0) or IsDotNetDetected('v4.5', 0) or IsDotNetDetected('v4\Full', 0) or IsDotNetDetected('v3.5', 0)) then 
    begin
      Result := False;
      if (MsgBox(ExpandConstant('{cm:dotnetmissing}'), mbConfirmation, MB_YESNO) = idYes) then
      begin
        ShellExec('open',
        'http://www.microsoft.com/en-ca/download/details.aspx?id=22',
        '','',SW_SHOWNORMAL,ewNoWait,ErrorCode);
      end;
    end 
    else
        result := true;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usPostUninstall:
      begin
        RegDeleteKeyIncludingSubkeys(HKCR, 'grfeditor.grf');
        RegDeleteKeyIncludingSubkeys(HKCR, 'grfeditor.gpf');
        RegDeleteKeyIncludingSubkeys(HKCR, 'grfeditor.rgz');
        RegDeleteKeyIncludingSubkeys(HKCR, 'grfeditor.spr');
        RegDeleteKeyIncludingSubkeys(HKCR, 'grfeditor.grfkey');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.grf');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.gpf');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.rgz');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.grfkey');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\.spr');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe.grf');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe.gpf');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe.rgz');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe.grfkey');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe.spr');
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\Applications\GRF Editor.exe');

        if (FileExists(ExpandConstant('{app}\config.txt')) or FileExists(ExpandConstant('{userappdata}\GRF Editor\config.txt'))) then
        begin
        if (MsgBox('Program settings have been found, would you like to remove them?', mbConfirmation, MB_YESNO) = idYes) then
          begin
            DeleteFile(ExpandConstant('{app}\config.txt'));
            DeleteFile(ExpandConstant('{userappdata}\GRF Editor\config.txt'));
           end
        end
      end;
  end;
end;