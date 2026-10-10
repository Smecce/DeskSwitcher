using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Interop;

using DesktopSwitcher;

namespace DeskSwitcher;


/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DesktopController _desktops = new();

    public MainWindow()
    {
        InitializeComponent();
        _desktops.DesktopsChanged += (_, _) => Rebuild();
        _desktops.CurrentChanged += (_, _) => Rebuild();

        Loaded += (_, _) =>
        {
            // закрепляем окно, чтобы оно было видно на всех столах
            _desktops.PinWindow(new WindowInteropHelper(this).Handle);
            Rebuild();
        };
        Closed += (_, _) => _desktops.Dispose();
    }

    private void Rebuild()
    {
        Panel.Children.Clear();
        int current = _desktops.CurrentIndex;

        for (int i = 0; i < _desktops.Count; i++)
        {
            int index = i; // копия для замыкания
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



}