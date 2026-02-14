namespace WallpaperEngine.Services {
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// 窗口查找工具类，通过 Windows API (EnumWindows) 根据进程 ID 查找窗口句柄
    /// </summary>
    public static class WindowFinder {
        // 声明所需的Windows API
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // 定义回调委托
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// 根据进程 ID 枚举所有顶层窗口，返回匹配的第一个窗口句柄
        /// </summary>
        /// <param name="processId">目标进程的 ID</param>
        /// <returns>找到的窗口句柄，未找到时返回 IntPtr.Zero</returns>
        public static IntPtr GetMainWindowHandle(int processId)
        {
            IntPtr foundHwnd = IntPtr.Zero;
            // 定义回调方法
            EnumWindowsProc callback = (hWnd, lParam) => {
                GetWindowThreadProcessId(hWnd, out int windowPid);
                if (windowPid == processId) {
                    foundHwnd = hWnd;
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            };

            EnumWindows(callback, IntPtr.Zero);
            return foundHwnd;
        }
    }
}
