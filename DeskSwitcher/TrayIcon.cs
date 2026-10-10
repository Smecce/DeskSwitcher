using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeskSwitcher;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public TrayIcon(string tooltip)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Показать", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _icon = new NotifyIcon
        {
            // значок exe-файла (задаётся ApplicationIcon), иначе стандартный
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application,
            Text = tooltip,                     // не длиннее 63 символов
            ContextMenuStrip = menu,
            Visible = true
        };

        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    public void Notify(string title, string text) =>
        _icon.ShowBalloonTip(4000, title, text, ToolTipIcon.Warning);

    public void Dispose()
    {
        _icon.Visible = false;   // иначе значок «зависает» в трее до наведения мыши
        _icon.Dispose();
    }
}