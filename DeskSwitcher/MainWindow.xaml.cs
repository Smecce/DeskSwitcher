using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Interop;

namespace DeskSwitcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private readonly DesktopController _desktops = new();
	private TrayIcon? _tray;
	private HotkeyManager? _hotkeys;
	private bool _exiting;

	private OsdWindow? _osd; // всплывашка :)

	public MainWindow()
	{
		InitializeComponent();

		_desktops.DesktopsChanged += (_, _) => Rebuild();
		_desktops.CurrentChanged += (_, _) =>
		{
			Rebuild();
			if (!IsVisible) ShowDesktopOsd();   // если окно видно, подсказка не нужна
		};

		// при выходе из Windows не мешаем завершению сеанса
		Application.Current.SessionEnding += (_, _) => _exiting = true;
	}


	private void ShowOsd(string title, string? subtitle = null)
	{
		_osd?.Dismiss();                     // убираем предыдущую всплывашку
		_osd = new OsdWindow(title, subtitle);
		_osd.Show();
	}

	private void ShowDesktopOsd()
	{
		int cur = _desktops.CurrentIndex;
		if (cur < 0) return;
		ShowOsd(_desktops.GetName(cur), $"{cur + 1} из {_desktops.Count}");
	}

	// ---------- запуск: трей и горячие клавиши ----------

	private void OnSourceInitialized(object? sender, EventArgs e)
	{
		var hwnd = new WindowInteropHelper(this).Handle;

		_tray = new TrayIcon("DeskSwitcher");
		_tray.ShowRequested += (_, _) => ShowFromTray();
		_tray.ExitRequested += (_, _) => ExitApp();

		_hotkeys = new HotkeyManager(hwnd);
		RegisterHotkeys();
	}

	private void RegisterHotkeys()
	{
		var failed = new List<string>();
		const HotkeyModifiers mod = HotkeyModifiers.Control | HotkeyModifiers.Alt;

		void Reg(string label, Key key, Action action)
		{
			if (!_hotkeys!.Register(mod, key, action)) failed.Add($"Ctrl+Alt+{label}");
		}

		for (int n = 1; n <= 9; n++)
		{
			int index = n - 1;                       // копия для замыкания
			Reg(n.ToString(), Key.D0 + n, () => _desktops.SwitchTo(index));
			Reg("Num" + n, Key.NumPad0 + n, () => _desktops.SwitchTo(index));
		}
		Reg("←", Key.Left, () => _desktops.SwitchLeft());
		Reg("→", Key.Right, () => _desktops.SwitchRight());

		if (failed.Count > 0) _tray?.Notify("Горячие клавиши", "Не удалось зарегистрировать: " + string.Join(", ", failed) + ". Возможно, они заняты другой программой.");
	}

	private void PinToAllDesktops() => _desktops.PinWindow(new WindowInteropHelper(this).Handle);

	// ---------- трей ----------

	private void HideToTray()
	{
		Hide();
		WindowState = WindowState.Normal;   // чтобы окно не вернулось свёрнутым
	}

	private void ShowFromTray()
	{
		Show();
		WindowState = WindowState.Normal;
		Activate();
	}

	private void ExitApp()
	{
		_exiting = true;
		Close();
	}

	// Крестик прячет окно в трей. Реальный выход — через меню трея.
	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (_exiting) return;
		e.Cancel = true;
		HideToTray();
	}

	private void OnClosed(object? sender, EventArgs e)
	{
		_hotkeys?.Dispose();
		_tray?.Dispose();
		_desktops.Dispose();

		_osd?.Dismiss();
	}

	// ---------- кнопки ----------

	private void Rebuild()
	{
		Panel.Children.Clear();
		int current = _desktops.CurrentIndex;

		for (int i = 0; i < _desktops.Count; i++)
		{
			int index = i;
			var btn = new Button
			{
				Content = _desktops.GetName(i),
				Margin = new Thickness(2),
				Padding = new Thickness(8, 2, 8, 2),
				FontWeight = i == current ? FontWeights.Bold : FontWeights.Normal
			};
			btn.Click += (_, _) => _desktops.SwitchTo(index);
			Panel.Children.Add(btn);
		}
	}
	private void On_Loaded(object sender, RoutedEventArgs e)
	{
		PinToAllDesktops();
		Rebuild();

		HideToTray();
		ShowOsd("DeskSwitcher запущен", "Значок в трее · Ctrl+Alt+1…9 — переключение столов");
	}
	private void On_StateChanged(object sender, EventArgs e)
	{
		if (WindowState == WindowState.Minimized) HideToTray();
	}
}