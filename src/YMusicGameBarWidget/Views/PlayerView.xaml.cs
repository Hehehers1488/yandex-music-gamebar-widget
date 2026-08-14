using Microsoft.Gaming.XboxGameBar;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using YMusicGameBarWidget.Models;
using YMusicGameBarWidget.Services;

namespace YMusicGameBarWidget.Views
{
    /// <summary>
    /// Widget page: shows the current Yandex Music track and transport controls.
    /// </summary>
    public sealed partial class PlayerView : Page
    {
        private XboxGameBarWidget _widget;
        private DispatcherTimer _ticker;

        public NowPlaying ViewModel { get; } = MediaSessionService.Instance.Current;

        public PlayerView()
        {
            DebugLog.Write("PlayerView ctor start");
            try
            {
                this.InitializeComponent();
                DebugLog.Write("PlayerView InitializeComponent ok");
                MediaSessionService.Instance.Updated += OnUpdated;
                MediaSessionService.Instance.Initialize();
                ApplyPlayPauseGlyph();

                _ticker = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _ticker.Tick += (s, e) =>
                {
                    try
                    {
                        MediaSessionService.Instance.RefreshState();
                        _ = MediaSessionService.Instance.RefreshNowPlayingAsync();
                    }
                    catch { }
                };
                _ticker.Start();
                DebugLog.Write("PlayerView ctor done");
            }
            catch (Exception ex)
            {
                DebugLog.Write("PlayerView ctor FAILED: " + ex);
                throw;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _widget = e.Parameter as XboxGameBarWidget;
            DebugLog.Write("PlayerView OnNavigatedTo, paramType=" + (e.Parameter?.GetType().Name ?? "null"));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            MediaSessionService.Instance.Updated -= OnUpdated;
            _ticker?.Stop();
        }

        private void OnUpdated(object sender, NowPlaying nowPlaying)
        {
            if (Dispatcher.HasThreadAccess)
            {
                ApplyPlayPauseGlyph();
            }
            else
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, ApplyPlayPauseGlyph);
            }
        }

        private void ApplyPlayPauseGlyph()
        {
            if (PlayGlyph == null) return;
            PlayGlyph.Glyph = ViewModel.IsPlaying ? "\uE769" : "\uE768";
        }

        private async void OnProgressTapped(object sender, TappedRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null || grid.ActualWidth <= 0) return;
            var duration = ViewModel.Duration;
            if (duration <= TimeSpan.Zero) return;

            var ratio = e.GetPosition(grid).X / grid.ActualWidth;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            var target = TimeSpan.FromSeconds(duration.TotalSeconds * ratio);
            await MediaSessionService.Instance.SeekAsync(target);
        }

        private async void OnSeekBack5(object sender, RoutedEventArgs e)
        {
            await MediaSessionService.Instance.SeekAsync(ViewModel.Position - TimeSpan.FromSeconds(5));
        }

        private async void OnSeekFwd5(object sender, RoutedEventArgs e)
        {
            await MediaSessionService.Instance.SeekAsync(ViewModel.Position + TimeSpan.FromSeconds(5));
        }

        private void OnProgressPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null || grid.ActualWidth <= 0 || ViewModel.Duration <= TimeSpan.Zero) return;

            var x = e.GetCurrentPoint(grid).Position.X;
            var ratio = x / grid.ActualWidth;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            var target = TimeSpan.FromSeconds(ViewModel.Duration.TotalSeconds * ratio);
            SeekHintText.Text = FormatTime(target);
            SeekHint.Visibility = Visibility.Visible;

            SeekHint.UpdateLayout();
            var half = SeekHint.ActualWidth / 2;
            var marginLeft = x - half;
            if (marginLeft < 0) marginLeft = 0;
            var maxLeft = grid.ActualWidth - SeekHint.ActualWidth;
            if (marginLeft > maxLeft) marginLeft = maxLeft;
            SeekHint.Margin = new Thickness(marginLeft, 0, 0, 0);
        }

        private void OnProgressPointerExited(object sender, PointerRoutedEventArgs e)
        {
            SeekHint.Visibility = Visibility.Collapsed;
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t <= TimeSpan.Zero) return "0:00";
            var total = (long)t.TotalSeconds;
            var m = total / 60;
            var s = total % 60;
            return m >= 60
                ? string.Format("{0}:{1:00}:{2:00}", m / 60, m % 60, s)
                : string.Format("{0}:{1:00}", m, s);
        }

        private async void OnPlayPause(object sender, RoutedEventArgs e)
        {
            if (!await MediaSessionService.Instance.TogglePlayPauseAsync())
            {
                YandexWindowControl.Send(MediaCommand.PlayPause);
            }
            MediaSessionService.Instance.RefreshState();
        }

        private async void OnNext(object sender, RoutedEventArgs e)
        {
            if (!await MediaSessionService.Instance.NextAsync())
            {
                YandexWindowControl.Send(MediaCommand.NextTrack);
            }
            MediaSessionService.Instance.RefreshState();
        }

        private async void OnPrev(object sender, RoutedEventArgs e)
        {
            if (!await MediaSessionService.Instance.PreviousAsync())
            {
                YandexWindowControl.Send(MediaCommand.PreviousTrack);
            }
            MediaSessionService.Instance.ResetPlaybackAnchor();
            MediaSessionService.Instance.RefreshState();
        }
    }
}
