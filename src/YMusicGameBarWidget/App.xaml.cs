using Microsoft.Gaming.XboxGameBar;
using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using YMusicGameBarWidget.Services;
using YMusicGameBarWidget.Views;

namespace YMusicGameBarWidget
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private XboxGameBarWidget widget;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.UnhandledException += (s, e) =>
            {
                DebugLog.Write("UnhandledException: " + e.Exception);
                e.Handled = true;
            };
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            try
            {
                DebugLog.Write("OnActivated Kind=" + args.Kind);
                XboxGameBarWidgetActivatedEventArgs widgetArgs = null;
                if (args.Kind == ActivationKind.Protocol)
                {
                    var protocolArgs = args as IProtocolActivatedEventArgs;
                    if (protocolArgs != null && protocolArgs.Uri != null)
                    {
                        DebugLog.Write("  scheme=" + protocolArgs.Uri.Scheme + " uri=" + protocolArgs.Uri.AbsoluteUri);
                        if (protocolArgs.Uri.Scheme.Equals("ms-gamebarwidget"))
                        {
                            widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                        }
                    }
                }
                DebugLog.Write("  widgetArgs=" + (widgetArgs != null ? "yes" : "null"));

                if (widgetArgs != null)
                {
                    DebugLog.Write("  IsLaunchActivation=" + widgetArgs.IsLaunchActivation
                        + " AppExtensionId=[" + widgetArgs.AppExtensionId + "]");

                    if (widgetArgs.IsLaunchActivation)
                    {
                        var rootFrame = new Frame();
                        rootFrame.NavigationFailed += OnNavigationFailed;
                        Window.Current.Content = rootFrame;

                        DebugLog.Write("  creating XboxGameBarWidget...");
                        widget = new XboxGameBarWidget(
                            widgetArgs,
                            Window.Current.CoreWindow,
                            rootFrame);
                        DebugLog.Write("  XboxGameBarWidget created.");

                        rootFrame.Navigate(typeof(PlayerView), widget);
                        DebugLog.Write("  navigated to PlayerView.");

                        Window.Current.Closed += OnWidgetWindowClosed;
                        Window.Current.Activate();
                        DebugLog.Write("  Window activated.");
                    }
                    else
                    {
                        DebugLog.Write("  repeat activation, re-navigating PlayerView");
                        Frame rootFrame = Window.Current.Content as Frame;
                        if (rootFrame == null)
                        {
                            rootFrame = new Frame();
                            rootFrame.NavigationFailed += OnNavigationFailed;
                            Window.Current.Content = rootFrame;
                        }
                        rootFrame.Navigate(typeof(PlayerView), widgetArgs.Uri);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("OnActivated FAILED: " + ex);
            }
        }

        private void OnWidgetWindowClosed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
        {
            widget = null;
            Window.Current.Closed -= OnWidgetWindowClosed;
            DebugLog.Write("Widget window closed.");
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                DebugLog.Write("OnLaunched");
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    Window.Current.Content = rootFrame;
                }
                if (e.PrelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                    Window.Current.Activate();
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("OnLaunched FAILED: " + ex);
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            DebugLog.Write("NavigationFailed to " + e.SourcePageType.FullName + ": " + e.Exception);
        }

        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            widget = null;
            deferral.Complete();
        }
    }
}
