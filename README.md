# IdeaPunctFix 

**JetBrains IDE 智能标点转换工具** — 当 IntelliJ IDEA / WebStorm / PyCharm 等 IDE 在前台时，自动将中文标点符号转换为英文标点。**像 HBuilder 一样方便写代码，再也不用在中文/英文输入法之间切来切去。**

## 效果对比

| 场景 | 输入法状态 | 按下 `,` 键 | 按下 `.` 键 | 按下 `;` 键 | 按下 `:` (Shift+;) |
|------|-----------|------------|------------|------------|-------------------|
| 普通软件 / 无工具 | 中文模式 | `，` | `。` | `；` | `：` |
| **IDEA + IdeaPunctFix** | 中文模式 | **`,`**  | **`.`**  | **`;`**  | **`:`**  |
| 普通软件 / 无工具 | 英文模式 | `,` | `.` | `;` | `:` |

## 工作原理

使用 Windows 低层键盘钩子 (`WH_KEYBOARD_LL`)，当检测到：

1.  前台窗口是 **JetBrains IDE**（IDEA、WebStorm、PyCharm 等）
2.  输入法处于 **中文模式**
3.  按下了 **标点符号键**（`,`, `.`, `;`, `'`, `[`, 等）

→ 自动 **拦截中文标点**，向编辑器发送 **英文标点**。其他应用完全不受影响。

##  快速开始

### 方式一：C# 版（推荐，独立 exe）

**前提：** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
cd src/IdeaPunctFix
dotnet run -c Release
```

**构建独立 exe（无需 .NET 运行时）：**

```bash
cd src/IdeaPunctFix
dotnet publish -c Release
```

`bin/Release/net8.0-windows/win-x64/publish/IdeaPunctFix.exe` 即为绿色单文件程序。

运行后，系统托盘会出现一个图标：

```
 IdeaPunctFix — JetBrains 智能标点
```

右键菜单：

- **禁用/启用转换** — 临时开关
- **关于** — 版本信息
- **退出** — 退出程序

### 方式二：AutoHotkey 版（需要 AHK v2）

**前提：** 安装 [AutoHotkey v2](https://www.autohotkey.com/)

```bash
# 直接双击运行
ahk/idea-punct-fix.ahk
```

功能与 C# 版相同，系统托盘出现 `H` 图标。

##  支持的 IDE

| IDE | 进程名 | 全称 |
|-----|--------|------|
| IntelliJ IDEA | `idea64.exe` / `idea.exe` | Java/Kotlin IDE |
| WebStorm | `webstorm64.exe` | JavaScript/TypeScript IDE |
| PyCharm | `pycharm64.exe` | Python IDE |
| GoLand | `goland64.exe` | Go IDE |
| CLion | `clion64.exe` | C/C++ IDE |
| Rider | `rider64.exe` | .NET IDE |
| DataGrip | `datagrip64.exe` | 数据库 IDE |
| RubyMine | `rubymine64.exe` | Ruby IDE |
| PhpStorm | `phpstorm64.exe` | PHP IDE |

> 其他 JetBrains IDE 也可通过修改代码中的 `TargetProcesses` 列表添加。

##  标点映射表

| 按键 | 中文输出（拦截） | 英文输出（转换后） | Shift 中文（拦截） | Shift 英文（转换后） |
|------|----------------|------------------|------------------|-------------------|
| `,` | `，` | **`,`** | `《` | **`<`** |
| `.` | `。` | **`.`** | `》` | **`>`** |
| `;` | `；` | **`;`** | `：` | **`:`** |
| `'` | `'` | **`'`** | `"` | **`"`** |
| `[` | `【` | **`[`** | `〖` | **`{`** |
| `]` | `】` | **`]`** | `〗` | **`}`** |
| `\` | `、` | **`\`** | `¦` | **`\|`** |
| `` ` `` | `·` | **`` ` ``** | `～` | **`~`** |
| `/` | `/` | **`/`** | `？` | **`?`** |
| `-` | `—`(部分输入法) | **`-`** | `——` | **`_`** |
| `=` | `＝` | **`=`** | `＋` | **`+`** |

> 不同中文输入法（微软拼音、搜狗、百度等）的具体输出字符可能有差异，但转换逻辑一致。

##  设计思路

### 为什么不用 AHK 的 `#IfWinActive` + `SendInput`？

AHK 的 `#IfWinActive` 指令无法在 IME 处理按键**之前**拦截标点符号。当 IME 处于中文模式时，标点键被 IME 截获，AHK 收不到按键事件。必须使用 **低层键盘钩子**（`SetWindowsHookEx WH_KEYBOARD_LL`）在 IME 处理之前拦截。

### 为什么用 `PostMessage(WM_CHAR)` 而不是 `SendInput`？

1. **避免递归**：`SendInput` 发送的按键会被同一个钩子再次捕获，需要用防递归标志
2. **更直接**：`WM_CHAR` 直接向编辑器窗口发送字符，Swing/AWT 的事件队列正确接收

### C# vs AHK？

| 特性 | C# 版 | AHK 版 |
|------|-------|--------|
| 是否需安装运行时 | 可构建独立 exe | 需 AutoHotkey v2 |
| 文件大小 | ~15MB (独立发布) | ~5KB |
| 内存占用 | ~10MB | ~2MB |
| 可靠性 | 高（类型安全） | 中（AHK 回调可能有延迟） |
| 配置扩展 | 改代码+重新编译 | 改脚本即生效 |

##  项目结构

```
IdeaPunctFix/
├── README.md                     # 本文件
├── .gitignore
├── src/
│   └── IdeaPunctFix/             # C# 主程序
│       ├── IdeaPunctFix.csproj   # 项目文件 (.NET 8)
│       ├── Program.cs            # 入口
│       ├── TrayApp.cs            # 托盘图标 + 菜单
│       ├── KeyboardHook.cs       # 低层键盘钩子
│       ├── PunctuationFilter.cs  # 标点映射表
│       ├── ImeHelper.cs          # IME 状态检测
│       └── WindowHelper.cs       # 窗口检测
└── ahk/
    └── idea-punct-fix.ahk        # AutoHotkey 版本
```

##  注意事项

- **管理员权限**：低层键盘钩子 (`WH_KEYBOARD_LL`) 在某些安全策略严格的系统上可能需要管理员权限。如果程序启动时提示"无法安装键盘钩子"，请右键 → **以管理员身份运行**。
- **杀软误报**：键盘钩子程序可能被某些杀毒软件误报为"键盘记录器"。本项目代码完全开源，可自行审查编译。
- **输入法兼容性**：支持主流中文输入法（微软拼音、搜狗、百度、QQ 拼音等）。如有兼容问题，欢迎提 Issue。

##  License

MIT License — 随便用，随便改。
