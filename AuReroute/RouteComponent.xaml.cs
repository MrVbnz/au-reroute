using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace AuReroute
{
    public partial class RouteComponent : UserControl
    {
        public event Action? RemoveRequested;

        private AudioRouter? audioRouter;

        public RouteComponent()
        {
            InitializeComponent();
            ListDevices();
        }

        private void ListDevices()
        {
            Combo_From.Items.Clear();
            Combo_To.Items.Clear();
            var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                if (device == null)
                    continue;
                Combo_From.Items.Add(new FromDeviceInfo(device));
            }

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                Combo_To.Items.Add(new ToDeviceInfo(i, capabilities));
            }
        }

        private void SetStatus(string message)
        {
            var colorAnimation = new ColorAnimation
            {
                From = Colors.White,
                To = Colors.Purple,
                Duration = new Duration(TimeSpan.FromSeconds(0.1)),
                AutoReverse = true,
            };

            Label_Status.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            Label_Status.Content = message;
        }

        private void SetIsActive(bool active)
        {
            if (!active)
                ListDevices();

            Combo_From.IsEnabled = !active;
            Combo_To.IsEnabled = !active;
            Button_Start.IsEnabled = !active;

            Button_Stop.IsEnabled = active;
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (audioRouter != null)
                    audioRouter.Stop();
                var selectedFrom = Combo_From.SelectedValue as FromDeviceInfo;
                var selectedTo = Combo_To.SelectedValue as ToDeviceInfo;
                if (selectedFrom == null || selectedTo == null)
                {
                    SetStatus("Incorrect device");
                    return;
                }

                audioRouter = new AudioRouter(selectedFrom.Device.ID, selectedTo.Id);
                audioRouter.Start();

                SetStatus("Running");
                SetIsActive(true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (audioRouter != null)
                    audioRouter.Stop();
                SetStatus("Stopped");
                SetIsActive(false);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (audioRouter != null)
                    audioRouter.Stop();
                SetStatus("Stopped");
                SetIsActive(false);
                RemoveRequested?.Invoke();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }            
        }
    }

    internal sealed class FromDeviceInfo
    {
        public readonly MMDevice Device;
        private string toStringCache;

        public FromDeviceInfo(MMDevice device)
        {
            Device = device;
            toStringCache = device.FriendlyName;
        }

        public override string ToString() => toStringCache;
    }

    internal sealed class ToDeviceInfo
    {
        public readonly int Id;
        public readonly WaveOutCapabilities Capabilities;
        private string toStringCache;

        public ToDeviceInfo(int id, WaveOutCapabilities capabilities)
        {
            Id = id;
            Capabilities = capabilities;
            toStringCache = capabilities.ProductName;
        }

        public override string ToString() => toStringCache;
    }
}
