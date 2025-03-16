﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Configuration;
using GestureSign.Common.Input;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.Common.UI;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Properties;

namespace GestureSign.Daemon
{
    public class TrayManager : ILoadable, ITrayManager
    {

        #region Private Variables

        // Create variable to hold the only allowed instance of this class
        static readonly TrayManager _Instance = new TrayManager();

        #endregion

        #region Controls Initialization

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _disableGesturesMenuItem;
        private ToolStripMenuItem _controlPanelMenuItem;
        private ToolStripMenuItem _exitGestureSignMenuItem;

        #endregion

        #region Private Methods

        private void SetupTrayIconAndTrayMenu()
        {
            _trayIcon = new NotifyIcon();
            _trayMenu = new ContextMenuStrip();
            _disableGesturesMenuItem = new ToolStripMenuItem();
            _controlPanelMenuItem = new ToolStripMenuItem();
            _exitGestureSignMenuItem = new ToolStripMenuItem();

            // Tray Icon
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Text = "GestureSign";
            _trayIcon.DoubleClick += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            _trayIcon.Click += (o, e) => { TrayIcon_Click(o, (MouseEventArgs)e); };
            _trayIcon.Icon = Resources.normal_daemon;

            // Tray Menu
            _trayMenu.Items.AddRange(new ToolStripMenuItem[] { _disableGesturesMenuItem, new ToolStripMenuItem("-"), _controlPanelMenuItem, new ToolStripMenuItem("-"), _exitGestureSignMenuItem });
            _trayMenu.Name = "TrayMenu";
            //TrayMenu.Size = new Size(194, 82);
            //TrayMenu.Opened += (o, e) => { Input.TouchCapture.Instance.DisableTouchCapture(); };
            //TrayMenu.Closed += (o, e) => { Input.TouchCapture.Instance.EnableTouchCapture(); };

            // Disable Gestures Menu Item
            _disableGesturesMenuItem.Checked = false;
            //miDisableGestures.CheckOnClick = true;
            _disableGesturesMenuItem.Name = "DisableGesturesMenuItem";
            //miDisableGestures.Size = new Size(193, 22);
            _disableGesturesMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Disable");
            _disableGesturesMenuItem.Click += (o, e) => { ToggleDisableGestures(); };


            // Control Panel Menu Item
            _controlPanelMenuItem.Name = "ControlPanel";
            //_controlPanelMenuItem.Size = new Size(193, 22);
            _controlPanelMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.ControlPanel");
            _controlPanelMenuItem.Click += (o, e) =>
            {
                StartControlPanel();
            };

            _exitGestureSignMenuItem.Name = "ExitGestureSign";
            //miExitGestureSign.Size = new Size(193, 22);
            _exitGestureSignMenuItem.Text = LocalizationProvider.Instance.GetTextValue("TrayMenu.Exit");
            _exitGestureSignMenuItem.Click += async (o, e) =>
            {
                await NamedPipe.SendMessageAsync(IpcCommands.Exit, Constants.ControlPanel, wait: false);
                // try to fix exception 0xc0020001
                Application.DoEvents();
                Application.Exit();
            };
        }

        private void TrayIcon_Click(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (e.Clicks == 2 && PointCapture.Instance.Mode != CaptureMode.Training)
                        ToggleDisableGestures();
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    ToggleDisableGestures();
                    break;
            }
        }

        #endregion

        #region Constructors

        protected TrayManager()
        {
            PointCapture.Instance.ModeChanged += CaptureMode_Changed;
            Application.ApplicationExit += Application_ApplicationExit;
        }
        #endregion


        #region Public Properties

        public static TrayManager Instance
        {
            get { return _Instance; }
        }

        public bool TrayIconVisible
        {
            get { return _trayIcon.Visible; }
            set { _trayIcon.Visible = value; }
        }

        public void Load()
        {
            SetupTrayIconAndTrayMenu();
            _trayIcon.Visible = AppConfig.ShowTrayIcon;

            AppConfig.ConfigChanged += (o, ea) =>
            {
                _trayIcon.Visible = AppConfig.ShowTrayIcon;
            };
        }

        public static void StartControlPanel()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ControlPanelFileName);
            if (File.Exists(path))
            {
                using (Process controlPanel = new Process())
                {
                    try
                    {
                        controlPanel.StartInfo.FileName = path;
                        //daemon.StartInfo.UseShellExecute = false;
                        controlPanel.Start();
                    }
                    catch (Exception exception)
                    {
                        Logging.LogException(exception);
                        MessageBox.Show(exception.ToString(),
                            LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show(string.Format(LocalizationProvider.Instance.GetTextValue("Messages.ComponentNotFoundMessage"), path),
                    LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Events

        void Application_ApplicationExit(object sender, EventArgs e)
        {
            if (_trayIcon != null) _trayIcon.Visible = false;
        }


        protected void CaptureMode_Changed(object sender, ModeChangedEventArgs e)
        {
            // Update tray icon based on new state
            if (e.Mode == CaptureMode.UserDisabled)
            {
                _disableGesturesMenuItem.Checked = true;
                _trayIcon.Icon = Resources.stop;
            }
            else
            {
                _disableGesturesMenuItem.Checked = false;
                // Consider state of Training Mode and load according icon
                _trayIcon.Icon = e.Mode == CaptureMode.Training ? Resources.add : Resources.normal_daemon;
            }
        }

        #endregion

        #region Public Methods

        public void ToggleDisableGestures()
        {
            PointCapture.Instance.ToggleUserDisablePointCapture();
        }

        #endregion

    }
}
