using System.Runtime.InteropServices;

namespace DeskSwitcher;

public static class FocusHelper
{
	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_TOOLWINDOW = 0x00000080;
	private const int WS_EX_APPWINDOW = 0x00040000;
	private const int WS_EX_NOACTIVATE = 0x08000000;
	private const uint GW_OWNER = 4;
	private const int DWMWA_CLOAKED = 14;

	/// <summary>Передаёт фокус верхнему окну текущего стола (но не окнам нашей программы).</summary>
	public static void FocusTopWindowOnCurrentDesktop()
	{
		uint ourPid = (uint)Environment.ProcessId;
		IntPtr target = IntPtr.Zero;

		// EnumWindows перечисляет окна сверху вниз по порядку наложения
		EnumWindows((hwnd, _) =>
		{
			if (!IsCandidate(hwnd, ourPid)) return true;   // ищем дальше
			target = hwnd;
			return false;                                    // нашли
		}, IntPtr.Zero);

		if (target == IntPtr.Zero) return;

		// Windows разрешает менять активное окно только «активному» потоку, поэтому присоединяемся к нему.
		IntPtr fg = GetForegroundWindow();
		uint fgThread = GetWindowThreadProcessId(fg, out _);
		uint ourThread = GetCurrentThreadId();
		bool attached = fgThread != 0 && fgThread != ourThread && AttachThreadInput(ourThread, fgThread, true);

		SetForegroundWindow(target);

		if (attached) AttachThreadInput(ourThread, fgThread, false);
	}

	private static bool IsCandidate(IntPtr hwnd, uint ourPid)
	{
		if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return false;

		// окна других виртуальных столов «скрыты» оболочкой
		if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
			return false;

		int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
		bool appWindow = (ex & WS_EX_APPWINDOW) != 0;

		if ((ex & WS_EX_TOOLWINDOW) != 0 && !appWindow) return false;
		if ((ex & WS_EX_NOACTIVATE) != 0) return false;
		if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero && !appWindow) return false;
		if (GetWindowTextLength(hwnd) == 0) return false;      // служебные окна без заголовка

		GetWindowThreadProcessId(hwnd, out uint pid);
		return pid != ourPid;
	}

	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	[DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
	[DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
	[DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLength(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
	[DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
	[DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
	[DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
	[DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
	[DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int size);
}