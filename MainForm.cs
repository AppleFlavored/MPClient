using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Melanchall.DryWetMidi.Interaction;
using Timer = System.Windows.Forms.Timer;
using MPClient.Properties;
using NAudio.Wave;
using System.Net.Http.Headers;
using System.IO;

namespace MPClient
{
    public partial class MainForm : Form
    {
        private OutputDevice device;
        private Playback currentPlayback;
        private Timer playbackTimer;
        private MidiFile currentFile;
        private bool shouldLoop = false;
        private string midiPath = "";

        public MainForm()
        {
            InitializeComponent();

            playbackTimer = new Timer();
            playbackTimer.Interval = 1000;
            playbackTimer.Tick += PlaybackTimer_Tick;

            foreach (OutputDevice device in OutputDevice.GetAll())
            {
                deviceSelection.Items.Add(device.Name);
            }

            device = OutputDevice.GetById(0);
            deviceSelection.SelectedIndex = 0;
        }

        private void outputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (device.Id != deviceSelection.SelectedIndex)
                device = OutputDevice.GetById(deviceSelection.SelectedIndex);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (midiPath.Length == 0)
            {
                MessageBox.Show("Select a midi file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SelectMidi();
                return;
            }

            if (currentPlayback == null || !currentPlayback.IsRunning)
            {
                currentFile = MidiFile.Read(midiPath);
                currentPlayback = currentFile.GetPlayback(device);
                currentPlayback.Loop = shouldLoop;

                currentPlayback.Started += CurrentPlayback_Started;
                currentPlayback.Finished += CurrentPlayback_Stopped;
                currentPlayback.Stopped += CurrentPlayback_Stopped;

                try
                {
                    currentPlayback.Start();
                }
                catch (MidiDeviceException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnPlay.Image = Resources.control_stop;
            }
            else
            {
                currentPlayback.Stop();
                device.TurnAllNotesOff();
            }
        }

        private delegate void NeedsUpdate();

        private void UpdateComponents()
        {
            deviceSelection.Enabled = true;
            statusLabel.Text = "Not Playing";
            statusStrip1.BackColor = Color.DarkMagenta;
            btnPlay.Image = Resources.control_play;
            btnPlay.ToolTipText = "Play";
            btnSelectSong.Enabled = true;
            playbackTimer.Stop();
        }

        private void CurrentPlayback_Stopped(object sender, EventArgs e)
        {
            if (InvokeRequired)
                Invoke(new NeedsUpdate(UpdateComponents));
            else
                UpdateComponents();
        }

        private void CurrentPlayback_Started(object sender, EventArgs e)
        {
            deviceSelection.Enabled = false;
            playbackTimer.Start();
            btnPlay.ToolTipText = "Stop";
            statusStrip1.BackColor = Color.DodgerBlue;
            statusLabel.Text = "Loading";
            btnSelectSong.Enabled = false;
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            MetricTimeSpan current = currentPlayback.GetCurrentTime<MetricTimeSpan>();
            MetricTimeSpan duration = currentPlayback.GetDuration<MetricTimeSpan>();

            statusLabel.Text = $"Playing {Path.GetFileName(midiPath)} [{ToTimeString(current)} - {ToTimeString(duration)}]";
        }

        private string ToTimeString(MetricTimeSpan time)
        {
            string seconds = time.Seconds.ToString();

            if (time.Seconds < 10)
                seconds = seconds.Insert(0, "0");

            return $"{time.Minutes}:{seconds}";
        }

        private void btnRepeat_Click(object sender, EventArgs e)
        {
            shouldLoop = !shouldLoop;

            if (currentPlayback != null)
                currentPlayback.Loop = shouldLoop;

            if (shouldLoop)
                btnRepeat.Image = Resources.control_repeat_blue;
            else
                btnRepeat.Image = Resources.control_repeat;
        }

        private void btnSelectSong_Click(object sender, EventArgs e)
        {
            SelectMidi();
        }

        private void SelectMidi()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Midi Files|*.mid";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                midiPath = dialog.FileName;
            }
        }
    }
}
