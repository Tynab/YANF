using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Script;
using YANF.Script.Service;
using static System.Math;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Script.YANConstant;
using static YANF.Script.YANConstant.MsgBoxLang;

namespace YANF.Demo
{
    public partial class MainFrm : Form
    {
        #region Fields
        // Simulated work: 100 steps (one per percent) of STEP_MS milliseconds, and the size of the simulated download
        private const int STEP_MS = 30;
        private const double TOTAL_MB = 137;
        #endregion

        #region Constructors
        public MainFrm() => InitializeComponent();
        #endregion

        #region Events
        // Show update screen
        private async void BtnUpdScr_Click(object sender, EventArgs e)
        {
            try
            {
                if (tgLegacy.Checked)
                {
                    IYANDlvScrService service = new YANUpdScrService();
                    RunLegacy(service, percent => service.PublishValue(percent, Capacity(percent), (int)Ceiling(percent * W_UPDATE_SCR / 100d)));
                    return;
                }
                // YANLoader.Show: a scope around awaited work on the UI thread; the form takes no input until it is disposed
                using var scope = YANLoader.Show(this, new YANLoaderOptions
                {
                    Kind = YANLoaderKind.Update
                });
                for (var percent = 1; percent <= 100; percent++)
                {
                    await Task.Delay(STEP_MS);
                    scope.SetProgress(percent, Capacity(percent));
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // Show wait screen
        private async void BtnWaitScr_Click(object sender, EventArgs e)
        {
            try
            {
                if (tgLegacy.Checked)
                {
                    RunLegacy(new YANWaitScrService(), null);
                    return;
                }
                // RunWithLoaderAsync: any task will do, the Wait screen shows no progress
                await this.RunWithLoaderAsync((progress, ct) => Task.Delay(100 * STEP_MS, ct), new YANLoaderOptions
                {
                    Kind = YANLoaderKind.Wait
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // Show load screen
        private async void BtnLoadScr_Click(object sender, EventArgs e)
        {
            try
            {
                if (tgLegacy.Checked)
                {
                    IYANDlvScrService service = new YANLoadScrService();
                    RunLegacy(service, percent => service.PublishValue(percent, null, 0));
                    return;
                }
                // RunWithLoaderAsync: the work runs on the thread pool and reports its progress from there
                await this.RunWithLoaderAsync((progress, ct) => Task.Run(() => Work(progress, ct), ct));
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        // Show demo 1 screen
        private void BtnDemo1_Click(object sender, EventArgs e) => new Demo1().Show();

        // Show demo 2 screen
        private void BtnDemo2_Click(object sender, EventArgs e) => new Demo2().Show();

        // lbl Legacy click: switch tg Legacy too
        private void LblLegacy_Click(object sender, EventArgs e) => tgLegacy.Checked = !tgLegacy.Checked;
        #endregion

        #region Methods
        // Simulated work on a worker thread (IProgress<int> can be used from any thread)
        private static void Work(IProgress<int> progress, CancellationToken ct)
        {
            for (var percent = 1; percent <= 100; percent++)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(STEP_MS);
                progress.Report(percent);
            }
        }

        // 1.0 services, kept for work that blocks the UI thread: the screen runs on a thread of its own, so it keeps moving meanwhile
        // (publish is null for the Wait screen; PublishValue can be called from any thread, without Invoke)
        private void RunLegacy(IYANSrcService service, Action<int> publish)
        {
            service.OnLoader(this);
            try
            {
                for (var percent = 1; percent <= 100; percent++)
                {
                    // blocking work, for example a synchronous database call
                    Thread.Sleep(STEP_MS);
                    publish?.Invoke(percent);
                }
            }
            finally
            {
                service.OffLoader();
            }
        }

        // Detail text of the update screen
        private static string Capacity(int percent) => $"{percent * TOTAL_MB / 100:0.##} MB / {TOTAL_MB:0.##} MB";

        // Show what failed (an async void handler must not let an exception escape)
        private void ShowError(Exception ex) => YANMessageBox.Show(this, new YANMessageBoxOptions
        {
            Caption = "ERROR",
            Text = ex.Message,
            Buttons = OK,
            Icon = Error,
            Language = ENG
        });
        #endregion
    }
}
