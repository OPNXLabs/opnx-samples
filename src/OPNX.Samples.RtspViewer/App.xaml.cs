using System.Windows;
namespace OPNX.Samples.RtspViewer;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        SampleTheme.Apply("OPNX.V Dark", "Blue");
        base.OnStartup(e);
    }
}
