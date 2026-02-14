namespace WallpaperEngine.Services {
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

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
