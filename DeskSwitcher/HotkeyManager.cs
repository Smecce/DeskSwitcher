using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace DeskSwitcher;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0, Alt = 0x1, Control = 0x2, Shift = 0x4, Win = 0x8
}

public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_NOREPEAT = 0x4000;   // не повторять при удержании

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 1;

    public HotkeyManager(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd)
                  ?? throw new InvalidOperationException("У окна нет HWND.");
        _source.AddHook(WndProc);
    }

    /// <summary>false — сочетание занято другой программой или системой.</summary>
    public bool Register(HotkeyModifiers modifiers, Key key, Action action)
    {
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        int id = _nextId++;

        if (!RegisterHotKey(_hwnd, id, (uint)modifiers | MOD_NOREPEAT, vk))
            return false;

        _actions[id] = action;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys)
            UnregisterHotKey(_hwnd, id);
        _actions.Clear();
        _source.RemoveHook(WndProc);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}