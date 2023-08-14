using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Media;

namespace PastelChimeCDDAEmuWinForms
{
    public partial class Form1 : Form
    {
        private int lines = 0;
        private int currentTrack = 0;
        private String gamePath = "L:\\CLEANS\\PC-98\\Pastel Chime\\";
        private String path = "L:\\CLEANS\\PC-98\\xsystem35\\xsystem35.exe";
        private SoundPlayer player = new SoundPlayer();

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
        private void playCDTrack(string tracknum)
        {
            int track = int.Parse(tracknum);
            if (track == 0)
            {
                if (track != currentTrack)
                {
                    currentTrack = 0;
                    logInfo("Got Track 0, stopping CD.");
                    player.Stop();
                }
            }
            else if (track > 17 || track < 0) { return; }//safety only, not possible in theory
            else
            {
                if (track != currentTrack)
                {
                    logInfo("Got CD Track: " + track);
                    player.Stop();
                    if (track < 10)
                    {
                        player.SoundLocation = gamePath + "BGM\\" + "Track" + "0" + track + "-_128kbit_AAC_.wav";
                    }
                    else { player.SoundLocation = gamePath + "BGM\\" + "Track" + track + "-_128kbit_AAC_.wav"; }
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
                Arguments = "/C start " + path,
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

            Process[] psi = Process.GetProcessesByName("xsystem35");
            if (psi.Length == 0)
            {
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
                    //  -be called from the game directory
                }
                startXSystem35(path);
                logInfo("Giving xSystem35 enough time to start up...");
                Thread.Sleep(4000);
                loop();
            }
            else
            {
                logInfo("Found Running XSystem35 process: ");
                logInfo(psi.First().MainWindowTitle);
                if (!psi.First().MainWindowTitle.Contains("Pastel Chime"))
                {
                    logInfo("Please close it and then press the start button on top right or start Pastel Chime manually.");
                }
                else {
                    loop();
                }
            }
        }

        private void loop() {

            logInfo("Play like normal");
            logInfo("The emulator will periodically read xSystem35's window title to get the CD Track and play it for you");
            logInfo("Enjoy!");
            while (true)
            {
                Thread.Sleep(250);
                Process[] pss = Process.GetProcessesByName("xsystem35");
                String cdtrackRaw = pss.Where(o => o.MainWindowTitle.Contains("Pastel Chime - CDTrack:")).FirstOrDefault().MainWindowTitle;
                String cdtrack = cdtrackRaw.Split(' ').Last();
                playCDTrack(cdtrack);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(logic).Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            player.Stop();
            Process[] pss = Process.GetProcessesByName("PastelChimeCDDAEmuWinForms");
            pss.First().Kill();
        }
    }
}
