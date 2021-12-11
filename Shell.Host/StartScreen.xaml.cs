﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Toolkit.Wpf.UI.XamlHost;

namespace Shell.Host {
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : Window {
        public StartScreen() {
            this.InitializeComponent();

            this.Height = Functions.STARTSCREEN_HEIGHT;
            this.Left = 0;
            this.Top = Functions.STATUSBAR_HEIGHT;

            // var frame = (Windows.UI.Xaml.Controls.Frame)this.MainFrame.Child;
            // frame.Navigate(typeof(Shell.SplashPage), null);
        }

        private void Window_Loaded(Object sender, RoutedEventArgs e) {
            var wndHelper = new WindowInteropHelper(this);

            Int32 exStyle = (Int32)WinAPI.GetWindowLong(wndHelper.Handle, (Int32)WinAPI.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (Int32)WinAPI.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            WinAPI.SetWindowLong(wndHelper.Handle, (Int32)WinAPI.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private async void RootFrame_ChildChanged(Object sender, EventArgs e) {
            var windowsXamlHost = (WindowsXamlHost)sender;
            var rootFrame = (Windows.UI.Xaml.Controls.Frame)windowsXamlHost.Child;
            if (rootFrame == null) return;

            try {
                // rootFrame.Navigate(typeof(Shell.SplashPage));
            } catch { }
        }
    }
}