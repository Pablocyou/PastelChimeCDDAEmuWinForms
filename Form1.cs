using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;
using WindowsInput;
using System.Runtime.InteropServices;
using System.Text;
using System.Media;

namespace PastelChimeCDDAEmuWinForms
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder ss, int count);

        private int lines = 0;
        private int currentTrack = 0;
        private string ActiveWindowTitle()
        {
            //Create the variable
            const int nChar = 256;
            StringBuilder ss = new StringBuilder(nChar);

            //Run GetForeGroundWindows and get active window informations
            //assign them into handle pointer variable
            IntPtr handle = IntPtr.Zero;
            handle = GetForegroundWindow();

            if (GetWindowText(handle, ss, nChar) > 0) return ss.ToString();
            else return "";
        }
        private void logInfo(String text, bool? refresh = false)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string, bool?>(logInfo), new object[] { text, refresh });
                return;
            }
            if (refresh == true) { textBox1.Text = ""; }
            textBox1.Text += text + Environment.NewLine;
            lines++;
            if (lines > 18)
            {
                textBox1.Text = textBox1.Text.Split(Environment.NewLine.ToCharArray(), 2).Skip(1).FirstOrDefault();
            }
        }
        public Form1()
        {
            InitializeComponent();
            this.Text = "CD-DA Emulator for Pastel Chime";
            textBox2.Text = "L:\\CLEANS\\PC-98\\Pastel Chime\\";
            textBox3.Text = "L:\\CLEANS\\PC-98\\xsystem35\\xsystem35.exe";
        }
        private string getCDTrack()
        {
            // at this point either you have clipboard data or an exception
            ClipboardAsync Clipboard2 = new ClipboardAsync();

            string clip = Clipboard2.GetText();
            // Do whatever you need to do with clipboardText
            string[] wip = clip.Split(Environment.NewLine.ToCharArray());
            var wip2 = wip.Reverse();
            var wip3 = wip2.Skip(2);
            var wip4 = wip3.First();
            return wip4;
        }
        private void playCDTrack(string tracknum, SoundPlayer player, string gamepath)
        {
            /* TODO:
                si recibes 0 --> stop y clear la track activa
                si recibes una track (1º vez) --> play esa track y te la guardas como track activa
                si recibes una track y ya está reproduciendo algo
	                -si son iguales, nada
	                -si son diferentes, cambias
                tmb hay que hacer que se reproduca en bucle
             */

            int track = int.Parse(tracknum);
            if (track == 0)
            {
                currentTrack = 0;
                player.Stop();
            }
            else if (track > 17 || track < 0) { return; }
            else
            {
                if (track != currentTrack)
                {
                    player.Stop();
                    if (track < 10)
                    {
                        player.SoundLocation = gamepath + "BGM\\" + "Track" + "0" + track + "-_128kbit_AAC_.wav";
                    }
                    else { player.SoundLocation = gamepath + "BGM\\" + "Track" + track + "-_128kbit_AAC_.wav"; }
                    currentTrack = track;
                    player.PlayLooping();
                }
            }
        }

        private void startXSystem35(String path)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C start " + path + " -debug",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            var p = new Process { StartInfo = startInfo };
            p.Start();
        }

        public void logic()
        {
            logInfo("", true);
            logInfo("CD-DA Emulator for Pastel Chime");

            Process[] pss = Process.GetProcessesByName("xsystem35");
            if (pss.Length == 0)
            {
                String gamePath = "L:\\CLEANS\\PC-98\\Pastel Chime\\";
                String path = "L:\\CLEANS\\PC-98\\xsystem35\\xsystem35.exe";

                if (textBox2.Text != "") { gamePath = @textBox2.Text; }
                if (textBox3.Text != "") { path = @textBox3.Text; }
                logInfo("Cannot find a running instance of xsystem35");
                logInfo("Creating a new instance...");
                //Check if we are being run from inside the game folder
                if (!File.Exists("PastelSA.ALD"))
                {
                    logInfo("Cannot find the game in current directory");
                    logInfo("Changing to specified path " + gamePath);
                    Directory.SetCurrentDirectory(gamePath);
                    //xSystem35 needs to either:
                    //  -have the game alongside the executable
                    //  OR
                    //  -be called from a the game directory
                }

                startXSystem35(path);

                logInfo("Giving xSystem35 enough time to start up...");
                InputSimulator isim = new InputSimulator();
                logInfo("Please click on the game window, then on the debug console window");
                logInfo("Wait until the game screen updates, then after clickling on the game window you'll be able to play");
                logInfo("The emulator will periodically do Alt+Tabs to the debug window to query the CD Track");
                logInfo("If it stops querying, click on the game window, then on the debug window, and then continue playing");

                Thread.Sleep(4000);
                logInfo("Woken up after 4s.");
                while (!ActiveWindowTitle().Contains(path))
                {
                    //check every 5 secs to send c + enter until user finally selects it
                    Thread.Sleep(5000);
                }
                logInfo("Sending c + ENTER");
                isim.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                isim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                String cdtrack;
                System.Media.SoundPlayer player = new System.Media.SoundPlayer();
                while (true)
                {
                    Thread.Sleep(8000);
                    //loginfo(ActiveWindowTitle());
                    if (ActiveWindowTitle().Contains("Version 2.9"))
                    {
                        //Alt+Tab into the console window
                        isim.Keyboard.KeyDown(VirtualKeyCode.MENU);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyUp(VirtualKeyCode.MENU);
                    }

                    //Send a ^C which stops execution
                    isim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_C);

                    //p VAR115 (Tell the debugger to read the value of VAR115, which contains the current CD Track)
                    //This is possible thanks to a modification to the code I made just for Pastel Chime, not necessary on other games.
                    //TODO: esto lo tenemos que sustituir por otra variable que creemos para usar con cada comando SS en los ADV
                    isim.Keyboard.TextEntry("p VAR115");
                    isim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    //Send Ctrl+A, Ctrl+C (Copy all contents from the debug console)
                    isim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                    Thread.Sleep(100);
                    isim.Keyboard.KeyPress(VirtualKeyCode.VK_A);
                    Thread.Sleep(100);
                    isim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                    Thread.Sleep(100);

                    isim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                    Thread.Sleep(100);
                    isim.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                    Thread.Sleep(100);
                    isim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                    Thread.Sleep(100);

                    //Drop 'c' then Enter into the debugger which means to continue execution
                    isim.Keyboard.KeyPress(VirtualKeyCode.VK_C);
                    isim.Keyboard.KeyPress(VirtualKeyCode.RETURN);

                    if (!ActiveWindowTitle().Contains("Version 2.9"))
                    {
                        //Alt+Tab back into the game
                        isim.Keyboard.KeyDown(VirtualKeyCode.MENU);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                        Thread.Sleep(100);
                        isim.Keyboard.KeyUp(VirtualKeyCode.MENU);
                    }
                    cdtrack = getCDTrack();
                    logInfo("Got CD Track: " + cdtrack);
                    playCDTrack(cdtrack, player, gamePath);
                }
            }
            else
            {
                logInfo("Found Running XSystem35 process: ");
                logInfo(pss.First().MainWindowTitle);
                logInfo("Please close it and then press start button on top right.");
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(logic).Start();
        }
    }
}



class ClipboardAsync
{
    private string _GetText;
    private void _thGetText(object format)
    {
        try
        {
            if (format == null)
            {
                _GetText = Clipboard.GetText();
            }
            else
            {
                _GetText = Clipboard.GetText((TextDataFormat)format);
            }
        }
        catch (Exception)
        {
            _GetText = string.Empty;
        }
    }
    public string GetText()
    {
        ClipboardAsync instance = new ClipboardAsync();
        Thread staThread = new Thread(instance._thGetText);
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        return instance._GetText;
    }
}
