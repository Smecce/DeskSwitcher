using System;
using Microsoft.Win32;

namespace DeskSwitcher;

public static class AutoStart
{
	private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string AppName = "DeskSwitcher";

	public static bool IsEnabled()
	{
		using var key = Registry.CurrentUser.OpenSubKey(RunKey);
		return key?.GetValue(AppName) is string;
	}

	public static void Set(bool enable)
	{
		using var key = Registry.CurrentUser.CreateSubKey(RunKey);   // откроет или создаст
		if (enable)
			key.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
		else
			key.DeleteValue(AppName, throwOnMissingValue: false);
	}
}