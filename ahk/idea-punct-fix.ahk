; ==============================================================================
; IdeaPunctFix — AutoHotkey 版本
; 
; 功能：当 JetBrains IDE (IntelliJ IDEA, WebStorm, PyCharm 等) 窗口在前台时，
;       自动将中文标点符号转换为英文标点。
; 
; 使用：安装 AutoHotkey v2 后双击运行本脚本，系统托盘会出现 H 图标。
;       右键菜单可暂停/恢复/退出。
; 
; 对比 C# 版本：AHK 版本功能相同，但需要安装 AutoHotkey v2。
; ==============================================================================
#Requires AutoHotkey v2.0
#SingleInstance Force
Persistent

; ===== 配置 =====
; 目标进程名列表（不含 .exe）
TargetProcesses := [
    "idea", "idea64",       ; IntelliJ IDEA
    "rider64",              ; JetBrains Rider
    "webstorm64",           ; WebStorm
    "pycharm64",            ; PyCharm
    "goland64",             ; GoLand
    "clion64",              ; CLion
    "datagrip64",           ; DataGrip
    "rubymine64",           ; RubyMine
    "phpstorm64",           ; PhpStorm
]

; 是否启用转换
Enabled := true

; ===== 标点映射表 =====
; VirtualKey -> [无Shift时的ASCII, 有Shift时的ASCII]
PunctMap := Map(
    0xBA, [Ord(";"), Ord(":")],   ; ;:
    0xBB, [Ord("="), Ord("+")],   ; =+
    0xBC, [Ord(","), Ord("<")],   ; ,<
    0xBD, [Ord("-"), Ord("_")],   ; -_
    0xBE, [Ord("."), Ord(">")],   ; .>
    0xBF, [Ord("/"), Ord("?")],   ; /?
    0xC0, [Ord("``"), Ord("~")],  ; `~
    0xDB, [Ord("["), Ord("{")],   ; [{
    0xDC, [Ord("\"), Ord("|")],   ; \|
    0xDD, [Ord("]"), Ord("}")],   ; ]}
    0xDE, [Ord("'"), Ord('"')],   ; '"
)

; ===== 低层键盘钩子 =====
; 注册低层键盘钩子回调
HookProc := CallbackCreate(KeyboardProc, , 4)
HookId := DllCall("SetWindowsHookEx"
    , "Int", 13                    ; WH_KEYBOARD_LL
    , "Ptr", HookProc
    , "Ptr", DllCall("GetModuleHandle", "Ptr", 0, "Ptr")
    , "UInt", 0, "Ptr")

; 获取前台窗口进程名
GetForegroundProcessName() {
    hWnd := DllCall("GetForegroundWindow", "Ptr")
    if !hWnd
        return ""
    PID := 0
    DllCall("GetWindowThreadProcessId", "Ptr", hWnd, "UInt*", &PID)
    if !PID
        return ""
    try {
        return ProcessGetName(PID)
    } catch {
        return ""
    }
}

; 检查 IME 是否在中文模式
IsChineseIme(hWnd) {
    hImc := DllCall("imm32\ImmGetContext", "Ptr", hWnd, "Ptr")
    if !hImc
        return false
    
    Conversion := 0, Sentence := 0
    DllCall("imm32\ImmGetConversionStatus", "Ptr", hImc, "Int*", &Conversion, "Int*", &Sentence)
    DllCall("imm32\ImmReleaseContext", "Ptr", hWnd, "Ptr", hImc)
    
    ; IME_CMODE_NATIVE = 0x0001
    return (Conversion & 0x0001) != 0
}

; 检查 Shift 键状态
IsShiftDown() {
    return (GetKeyState("Shift") & 0x80) != 0
}

; 键盘钩子回调
KeyboardProc(nCode, wParam, lParam) {
    Critical
    
    if !Enabled
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    if nCode < 0 || (wParam != 0x0100 && wParam != 0x0104)  ; 只处理 WM_KEYDOWN / WM_SYSKEYDOWN
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    ; 读取 KBDLLHOOKSTRUCT.vkCode
    vkCode := NumGet(lParam, 0, "UInt")
    
    ; 检查是否是需要处理的标点键
    if !PunctMap.Has(vkCode)
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    ; 检查前台窗口
    procName := GetForegroundProcessName()
    if procName = ""
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    isTarget := false
    for name in TargetProcesses {
        if StrCompare(procName, name, false) = 0 {
            isTarget := true
            break
        }
    }
    if !isTarget
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    ; 检查 IME 状态
    hWnd := DllCall("GetForegroundWindow", "Ptr")
    if !IsChineseIme(hWnd)
        return DllCall("CallNextHookEx", "Ptr", 0, "Int", nCode, "Ptr", wParam, "Ptr", lParam)
    
    ; 获取对应的英文 ASCII 码
    shiftDown := IsShiftDown()
    asciiCode := shiftDown ? PunctMap[vkCode][2] : PunctMap[vkCode][1]
    
    ; 发送 WM_CHAR 到前台窗口
    ; 0x0102 = WM_CHAR
    DllCall("PostMessage", "Ptr", hWnd, "UInt", 0x0102, "Ptr", asciiCode, "Ptr", 0)
    
    ; 返回 1 阻止原始按键
    return 1
}

; ===== 托盘菜单 =====
Tray := A_TrayMenu
Tray.Delete()  ; 清空默认菜单

Tray.Add("状态: 运行中 (JetBrains)", (*) => {})
Tray.Disable("状态: 运行中 (JetBrains)")
Tray.Add()

Tray.Add("禁用转换", ToggleHook)
Tray.Add()

Tray.Add("关于", ShowAbout)
Tray.Add("退出", (*) => ExitApp())

UpdateTrayTip() {
    if Enabled {
        Tray.Rename("禁用转换", "禁用转换")
        Tray.Uncheck("禁用转换")
        Tray.Rename("状态: 运行中 (JetBrains)", "✓ 运行中 (JetBrains)")
        A_IconTip := "IdeaPunctFix — JetBrains 智能标点"
    } else {
        Tray.Rename("禁用转换", "启用转换")
        Tray.Rename("状态: 运行中 (JetBrains)", "⏸ 已暂停")
        A_IconTip := "IdeaPunctFix — 已暂停"
    }
}

ToggleHook(*) {
    global Enabled := !Enabled
    UpdateTrayTip()
}

ShowAbout(*) {
    MsgBox(
        "IdeaPunctFix v1.0 (AutoHotkey)`n`n"
        . "当 JetBrains IDE 窗口在前台时，`n"
        . "自动将中文标点符号转为英文标点。`n`n"
        . "支持的 IDE:`n"
        . "  IntelliJ IDEA, WebStorm, PyCharm, GoLand,`n"
        . "  CLion, Rider, DataGrip, RubyMine, PhpStorm`n`n"
        . "右键托盘图标可暂停/恢复。",
        "关于 IdeaPunctFix",
        "OK Iconi"
    )
}

; 初始化托盘提示
UpdateTrayTip()

; 在退出时清理钩子
OnExit((*) => DllCall("UnhookWindowsHookEx", "Ptr", HookId))
