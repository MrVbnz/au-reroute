using System.Drawing;
using System.Windows;
using System.Windows.Media.Animation;

namespace AuReroute;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_AddRoute_Click(object sender, RoutedEventArgs e)
    {
        var route = new RouteComponent();
        var fadeIn = (Storyboard)FindResource("FadeInStoryboard");
        fadeIn.Begin(route);
        Panel_Routes.Children.Add(route);
        route.RemoveRequested += () =>
        {
            var fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
            fadeOut.Completed += (completedSender, completedArgs) => Panel_Routes.Children.Remove(route);
            fadeOut.Begin(route);
        };
    }

    private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }
}