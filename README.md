<div align="center">

# 🖥️ DeskSwitcher

**Компактная панель для быстрого переключения виртуальных рабочих столов Windows**

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white)

</div>

---

## О проекте

DeskSwitcher — небольшое WPF-приложение с панелью кнопок. Каждая кнопка соответствует одному виртуальному рабочему столу Windows: нажали — перешли на нужный стол, без цепочки `Win + Ctrl + ←/→`.

Окно закрепляется на всех рабочих столах, поэтому панель всегда под рукой, на каком бы столе вы ни находились.

## Возможности

- 🔘 **Кнопка на каждый рабочий стол** — переход одним нажатием.
- 🔤 **Названия столов** — если вы переименовали стол в Windows, кнопка покажет это имя, иначе «Стол N».
- ✨ **Подсветка текущего стола** — активный стол выделен жирным шрифтом.
- 🔄 **Автообновление** — панель сама перестраивается, когда вы создаёте, удаляете или переименовываете столы, а также когда переключаетесь клавиатурой или жестами.
- 📌 **Видна на всех столах** — окно закрепляется через API виртуальных рабочих столов.

## Требования

| Что | Версия |
|---|---|
| ОС | Windows 10 (сборка 19041 и новее) или Windows 11 |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| Пакет | [`Slions.VirtualDesktop`](https://www.nuget.org/packages/Slions.VirtualDesktop) |

> ⚠️ Приложение использует недокументированные интерфейсы Windows через библиотеку `Slions.VirtualDesktop`. Список поддерживаемых сборок Windows приведён в описании пакета. Сверьтесь со своей сборкой (`winver`).

## Быстрый старт

```bash
git clone <адрес-вашего-репозитория>
cd DeskSwitcher
dotnet run --project DeskSwitcher
```

Или откройте решение в Visual Studio и запустите проект (`F5`).

### Настройка проекта

В `DeskSwitcher.csproj` должна быть указана платформа Windows 10.0.19041 или выше (иначе пакет не подключится):

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Slions.VirtualDesktop" Version="6.9.2" />
</ItemGroup>
```

## Структура проекта

```
DeskSwitcher/
├── DesktopController.cs   # вся работа с рабочими столами (обёртка над библиотекой)
├── MainWindow.xaml        # разметка окна: контейнер для кнопок
├── MainWindow.xaml.cs     # построение кнопок, закрепление окна
└── DeskSwitcher.csproj
```

## Как это устроено

Вся логика работы с виртуальными столами изолирована в классе `DesktopController`. Окно ничего не знает о библиотеке и пользуется простым интерфейсом:

```csharp
var desktops = new DesktopController();

desktops.Count;                 // сколько столов
desktops.CurrentIndex;          // индекс текущего стола (с нуля)
desktops.GetName(i);            // название стола

desktops.SwitchTo(2);           // перейти на третий стол
desktops.SwitchLeft();          // на стол слева
desktops.SwitchRight();         // на стол справа

desktops.Create(switchTo: true);  // создать стол
desktops.RemoveAt(1);             // удалить стол

desktops.PinWindow(hwnd);       // показывать окно на всех столах

desktops.DesktopsChanged += ...; // список столов изменился
desktops.CurrentChanged  += ...; // текущий стол сменился
```

События библиотеки могут приходить из другого потока, поэтому контроллер возвращает их в UI-поток. Создавайте `DesktopController` в UI-потоке (например, в конструкторе окна).

Благодаря такой изоляции библиотеку можно заменить, не трогая интерфейс.

## Известные ограничения

- Работа зависит от сборки Windows: после крупного обновления системы библиотеке может понадобиться новая версия.
- Первый вызов API после запуска может занять заметное время: библиотека подбирает COM-интерфейсы под вашу сборку Windows.
- Последний оставшийся рабочий стол удалить нельзя.

## Планы

- [ ] Горячие клавиши для переключения по номеру стола
- [ ] Значок в области уведомлений
- [ ] Запуск вместе с Windows
- [ ] Сохранение положения и размера окна

## Благодарности

- [Slions/VirtualDesktop](https://github.com/Slions/VirtualDesktop) — форк, поддерживающий новые сборки Windows.
- [Grabacr07/VirtualDesktop](https://github.com/Grabacr07/VirtualDesktop) — исходная библиотека.

## Лицензия

Укажите лицензию проекта (например, MIT) и добавьте файл `LICENSE`.
