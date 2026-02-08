; Use Unicode for proper Chinese character support
Unicode True

!define PRODUCT_NAME "Dynamic Wallpaper Manager"
!define PRODUCT_VERSION "0.9.0"
!define PRODUCT_PUBLISHER "Dynamic Wallpaper Manager Team"
!define PRODUCT_WEB_SITE "https://github.com/yourusername/Dynamic-Wallpaper-Manager"

; Include Modern UI
!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

; Function to kill running process
!macro KillProcess
    DetailPrint "正在检查并关闭运行中的程序..."
    nsExec::ExecToStack 'taskkill /F /IM "Dynamic Wallpaper Manager.exe" /T'
    Pop $0 ; Return value
    Pop $1 ; Output
    ${If} $0 == 0
        DetailPrint "已关闭运行中的程序"
    ${Else}
        DetailPrint "未发现运行中的程序"
    ${EndIf}
    Sleep 500 ; Wait for process to fully terminate
!macroend

; General settings
Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${PRODUCT_NAME}-${PRODUCT_VERSION}.exe"
InstallDir "$LOCALAPPDATA\DynamicWallpaperManager"
InstallDirRegKey HKCU "Software\${PRODUCT_NAME}" "InstallDir"
RequestExecutionLevel user

; Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!define MUI_WELCOMEPAGE_TITLE "欢迎安装 ${PRODUCT_NAME}"
!define MUI_WELCOMEPAGE_TEXT "这将在您的计算机上安装 ${PRODUCT_NAME} ${PRODUCT_VERSION}。$\r$\n$\r$\n${PRODUCT_NAME} 是一个动态壁纸管理工具，让您的桌面更加生动。$\r$\n$\r$\n建议在继续之前关闭所有其他应用程序。$\r$\n$\r$\n点击 下一步 继续。"
!insertmacro MUI_PAGE_WELCOME

; License page (optional - uncomment if you have a license file)
; !insertmacro MUI_PAGE_LICENSE "License.txt"

; Directory page
!insertmacro MUI_PAGE_DIRECTORY

; Components page with startup option
Page custom StartupOptionsPage StartupOptionsPageLeave

; Instfiles page
!insertmacro MUI_PAGE_INSTFILES

; Finish page
!define MUI_FINISHPAGE_TITLE "安装完成"
!define MUI_FINISHPAGE_TEXT "${PRODUCT_NAME} 已成功安装到您的计算机。$\r$\n$\r$\n点击 完成 关闭此向导。"
!define MUI_FINISHPAGE_RUN "$INSTDIR\Dynamic Wallpaper Manager.exe"
!define MUI_FINISHPAGE_RUN_TEXT "运行 ${PRODUCT_NAME}"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_TEXT "在桌面创建快捷方式"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION CreateDesktopShortcut
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_NOREBOOTSUPPORT
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Language
!insertmacro MUI_LANGUAGE "SimpChinese"

; Variables
Var StartupCheckbox
Var StartupCheckboxState

; Startup options page
Function StartupOptionsPage
    nsDialogs::Create 1018
    Pop $0

    ${NSD_CreateLabel} 0 0 100% 24u "选择附加选项："
    Pop $0

    ${NSD_CreateCheckbox} 10 30u 100% 12u "开机自动启动 ${PRODUCT_NAME}"
    Pop $StartupCheckbox
    ${NSD_Check} $StartupCheckbox

    nsDialogs::Show
FunctionEnd

Function StartupOptionsPageLeave
    ${NSD_GetState} $StartupCheckbox $StartupCheckboxState
FunctionEnd

; Initialization function - kill process before installation
Function .onInit
    !insertmacro KillProcess
FunctionEnd

; Installation section
Section "主程序" SecMain
    SectionIn RO
    SetOutPath "$INSTDIR"
    File /r "bin\Release\net8.0-windows\*.*"

    ; Write registry keys for uninstaller
    WriteRegStr HKCU "Software\${PRODUCT_NAME}" "InstallDir" "$INSTDIR"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "UninstallString" "$INSTDIR\Uninstall.exe"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}" "DisplayIcon" "$INSTDIR\Dynamic Wallpaper Manager.exe"
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Create Start Menu shortcuts
    CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
    CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\Dynamic Wallpaper Manager.exe"
    CreateShortcut "$SMPROGRAMS\${PRODUCT_NAME}\卸载.lnk" "$INSTDIR\Uninstall.exe"

    ; Create startup item if checkbox is checked
    ${If} $StartupCheckboxState == ${BST_CHECKED}
        WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${PRODUCT_NAME}" "$INSTDIR\Dynamic Wallpaper Manager.exe"
    ${EndIf}
SectionEnd

; Desktop shortcut function
Function CreateDesktopShortcut
    CreateShortcut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\Dynamic Wallpaper Manager.exe"
FunctionEnd

; Uninstaller section
Section "Uninstall"
    ; Kill running process before uninstallation
    !insertmacro KillProcess

    ; Remove files
    Delete "$INSTDIR\Dynamic Wallpaper Manager.exe"
    Delete "$INSTDIR\*.dll"
    Delete "$INSTDIR\preview.jpg"
    Delete "$INSTDIR\project.json"
    Delete "$INSTDIR\assets\*.*"
    RMDir "$INSTDIR\assets"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"

    ; Remove shortcuts
    Delete "$DESKTOP\${PRODUCT_NAME}.lnk"
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\卸载.lnk"
    RMDir "$SMPROGRAMS\${PRODUCT_NAME}"

    ; Remove registry keys
    DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
    DeleteRegKey HKCU "Software\${PRODUCT_NAME}"
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${PRODUCT_NAME}"
SectionEnd