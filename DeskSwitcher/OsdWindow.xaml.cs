using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DeskSwitcher;

public partial class OsdWindow : Window
{
	private const int GWL_EXSTYLE = -20;
	private const int WS_EX_TRANSPARENT = 0x00000020;   // клики проходят сквозь окно
	private const int WS_EX_TOOLWINDOW = 0x00000080;   // нет в Alt+Tab
	private const int WS_EX_NOACTIVATE = 0x08000000;   // не забирает фокус

	private readonly DispatcherTimer _timer;
	private bool _closed;

	public OsdWindow(string title, string? subtitle = null)
	{
		InitializeComponent();

		TitleText.Text = title;
		if (string.IsNullOrEmpty(subtitle)) SubtitleText.Visibility = Visibility.Collapsed;
		else SubtitleText.Text = subtitle;

		// Ставим окно на место до показа, чтобы оно не мелькнуло в углу.
		Card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		var wa = SystemParameters.WorkArea;
		//Left = wa.Left + (wa.Width - Card.DesiredSize.Width) / 2;
		Left = wa.Left + wa.Width - Card.DesiredSize.Width - 30;
		Top = wa.Bottom - Card.DesiredSize.Height - 30;

		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
		_timer.Tick += (_, _) => { _timer.Stop(); FadeOutAndClose(); };

		SourceInitialized += (_, _) => MakeClickThrough();
		Loaded += (_, _) => _timer.Start();
		Closed += (_, _) => { _closed = true; _timer.Stop(); };
	}

	/// <summary>Безопасно закрыть, даже если окно уже закрылось само.</summary>
	public void Dismiss()
	{
		if (!_closed) Close();
	}

	private void FadeOutAndClose()
	{
		var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(250));
		fade.Completed += (_, _) => Dismiss();
		BeginAnimation(OpacityProperty, fade);
	}

	private void MakeClickThrough()
	{
		var hwnd = new WindowInteropHelper(this).Handle;
		int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
		SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
	}

	[DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
	[DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}