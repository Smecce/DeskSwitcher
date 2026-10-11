using System;
using System.Linq;
using System.Threading;
using WindowsDesktop;

namespace DeskSwitcher;

public sealed class DesktopController : IDisposable
{
	private readonly SynchronizationContext? _ui;
	private bool _disposed;

	/// <summary>Список столов изменился (создан, удалён, переименован).</summary>
	public event EventHandler? DesktopsChanged;

	/// <summary>Текущий стол сменился, в том числе не из нашей программы.</summary>
	public event EventHandler? CurrentChanged;

	public DesktopController()
	{
		// Создавайте контроллер в UI-потоке: события вернутся именно туда.
		_ui = SynchronizationContext.Current;

		VirtualDesktop.Created += OnListChanged;
		VirtualDesktop.Destroyed += OnListChanged;
		VirtualDesktop.Renamed += OnListChanged;   // если не компилируется, удалите
		VirtualDesktop.CurrentChanged += OnCurrentChanged;
	}

	// ---------- Состояние ----------

	private static VirtualDesktop[] GetAll() => VirtualDesktop.GetDesktops().ToArray();

	public int Count => GetAll().Length;

	/// <summary>Индекс текущего стола (с нуля), -1 если не найден.</summary>
	public int CurrentIndex
	{
		get
		{
			var id = VirtualDesktop.Current.Id;
			return Array.FindIndex(GetAll(), d => d.Id == id);
		}
	}

	public string GetName(int index)
	{
		var all = GetAll();
		if (index < 0 || index >= all.Length) return string.Empty;

		var name = all[index].Name;
		return string.IsNullOrWhiteSpace(name) ? $"Стол {index + 1}" : name;
	}

	// ---------- Переключение ----------

	public bool SwitchTo(int index)
	{
		var all = GetAll();
		if (index < 0 || index >= all.Length) return false;

		all[index].Switch();
		return true;
	}

	public bool SwitchLeft()
	{
		var target = VirtualDesktop.Current.GetLeft();
		if (target is null) // крайний левый стол
		{
			var all = GetAll();
			all[all.Length - 1].Switch();
			return true;
		}
		target.Switch();
		return true;
	}

	public bool SwitchRight()
	{
		var target = VirtualDesktop.Current.GetRight();
		if (target is null) // крайний правый стол
		{
			var all = GetAll();
			all[0].Switch();
			return true;
		}
		target.Switch();
		return true;
	}

	// ---------- Управление столами ----------

	/// <summary>Создаёт новый стол и возвращает его индекс.</summary>
	public int Create(bool switchTo = false)
	{
		var desktop = VirtualDesktop.Create();
		if (switchTo) desktop.Switch();
		return Array.FindIndex(GetAll(), d => d.Id == desktop.Id);
	}

	public bool RemoveAt(int index)
	{
		var all = GetAll();
		if (all.Length <= 1 || index < 0 || index >= all.Length)
			return false;                      // последний стол удалять нельзя

		all[index].Remove();
		return true;
	}

	// ---------- Закрепление окна на всех столах ----------

	public void PinWindow(IntPtr hwnd) => VirtualDesktop.PinWindow(hwnd);
	public void UnpinWindow(IntPtr hwnd) => VirtualDesktop.UnpinWindow(hwnd);

	// ---------- Служебное ----------

	private void OnListChanged(object? sender, object? e) => Raise(DesktopsChanged);
	private void OnCurrentChanged(object? sender, object? e) => Raise(CurrentChanged);

	// Библиотека может вызвать событие из другого потока, поэтому возвращаем его в UI.
	private void Raise(EventHandler? handler)
	{
		if (_disposed || handler is null) return;

		if (_ui is null) handler(this, EventArgs.Empty);
		else _ui.Post(_ => { if (!_disposed) handler(this, EventArgs.Empty); }, null);
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		VirtualDesktop.Created -= OnListChanged;
		VirtualDesktop.Destroyed -= OnListChanged;
		VirtualDesktop.Renamed -= OnListChanged;
		VirtualDesktop.CurrentChanged -= OnCurrentChanged;
	}
}