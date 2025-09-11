; -- GRF Editor --

[Setup]
AppName=GRF Editor
AppVersion={#VERSION_NAME}
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
function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    // 528040 = .NET Framework 4.8
    if Release >= 528040 then
      Result := True;
  end;
end;


function InitializeSetup(): Boolean;
var ErrorCode: Integer;
begin
  if not IsDotNet48Installed then
  begin
    MsgBox('.NET Framework 4.8 is required. The installer will now open the download page.', mbInformation, MB_OK);
    ShellExec('', 'https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;  // cancel setup
  end
  else
    Result := True;   // continue setup
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