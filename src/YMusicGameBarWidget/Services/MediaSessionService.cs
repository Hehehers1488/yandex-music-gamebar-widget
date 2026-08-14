using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using YMusicGameBarWidget.Models;

namespace YMusicGameBarWidget.Services
{
    /// <summary>
    /// Reads the current Yandex Music track through the system media session (SMTC)
    /// and issues transport controls against it.
    /// </summary>
    public sealed class MediaSessionService
    {
        private static readonly MediaSessionService _instance = new MediaSessionService();
        public static MediaSessionService Instance => _instance;

        private GlobalSystemMediaTransportControlsSessionManager _manager;
        private GlobalSystemMediaTransportControlsSession _session;
        private bool _started;
        private CoreDispatcher _dispatcher;
        private bool _pollingNowPlaying;
        private TimeSpan _lastPosition;
        private DateTime _lastTick;
        private TimeSpan _lastSynced = TimeSpan.MinValue;
        private DateTime _anchorSuppressedUntil;

        /// <summary>Raised on the UI thread whenever playback state changed.</summary>
        public event EventHandler<NowPlaying> Updated;

        public NowPlaying Current { get; } = new NowPlaying();

        public async void Initialize()
        {
            if (_started) return;
            _started = true;
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _dispatcher = Window.Current?.Dispatcher;
                DebugLog.Write("SMTC manager ok, sessions=" + _manager.GetSessions().Count());
                _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                _manager.SessionsChanged += OnSessionsChanged;
                SelectBestSession();
            }
            catch (Exception ex)
            {
                DebugLog.Write("SMTC init failed: " + ex.Message);
            }
        }

        private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            _ = DispatcherRunAsync(SelectBestSession);
        }

        private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        {
            _ = DispatcherRunAsync(SelectBestSession);
        }

        private async Task DispatcherRunAsync(Action action)
        {
            var d = _dispatcher;
            if (d == null)
            {
                try { d = Window.Current?.Dispatcher; }
                catch { return; }
            }
            if (d == null) return;
            try
            {
                await d.RunAsync(CoreDispatcherPriority.Normal, () => action());
            }
            catch { }
        }

        private static bool IsYandexMusicSession(GlobalSystemMediaTransportControlsSession s)
        {
            var id = s?.SourceAppUserModelId ?? string.Empty;
            bool isYandex = id.IndexOf("yandex", StringComparison.OrdinalIgnoreCase) >= 0
                         || id.IndexOf("яндекс", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isMusic = id.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0
                        || id.IndexOf("музыка", StringComparison.OrdinalIgnoreCase) >= 0;
            return isYandex && isMusic;
        }

        private void SelectBestSession()
        {
            if (_manager == null) return;
            try
            {
                GlobalSystemMediaTransportControlsSession session = null;
                foreach (var s in _manager.GetSessions())
                {
                    if (IsYandexMusicSession(s))
                    {
                        session = s;
                        break;
                    }
                }
                if (session == null && IsYandexMusicSession(_manager.GetCurrentSession()))
                {
                    session = _manager.GetCurrentSession();
                }
                SetSession(session);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SelectBestSession failed: " + ex.Message);
            }
        }

        private void SetSession(GlobalSystemMediaTransportControlsSession session)
        {
            if (_session == session) return;
            if (_session != null)
            {
                _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _session.TimelinePropertiesChanged -= OnTimelineChanged;
            }
            _session = session;
            if (_session != null)
            {
                _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _session.TimelinePropertiesChanged += OnTimelineChanged;
            }
            RefreshState();
            _ = RefreshNowPlayingAsync();
        }

        private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
            => _ = DispatcherRunAsync(() => _ = RefreshNowPlayingAsync());

        private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
            => _ = DispatcherRunAsync(RefreshState);

        private void OnTimelineChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
            => _ = DispatcherRunAsync(RefreshState);

        public void RefreshState()
        {
            TimeSpan duration = Current.Duration;
            TimeSpan synced = TimeSpan.Zero;
            bool haveSync = false;

            try
            {
                var tl = _session?.GetTimelineProperties();
                if (tl != null)
                {
                    if (tl.EndTime <= TimeSpan.FromDays(1))
                    {
                        duration = tl.EndTime - tl.StartTime;
                        if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
                        if (duration > TimeSpan.FromDays(1)) duration = TimeSpan.Zero;
                    }
                    else
                    {
                        duration = TimeSpan.Zero;
                    }
                    Current.Duration = duration;

                    // Yandex Music reports TimeSpan.MaxValue for unknown/unsupported positions.
                    if (tl.Position >= TimeSpan.Zero && tl.Position <= TimeSpan.FromDays(1))
                    {
                        synced = tl.Position;
                        haveSync = true;
                    }
                }
            }
            catch { }

            try
            {
                var info = _session?.GetPlaybackInfo();
                if (info == null)
                {
                    Current.IsPlaying = false;
                    Current.CanPlayPause = Current.CanNext = Current.CanPrevious = false;
                }
                else
                {
                    Current.IsPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                    var controls = info.Controls;
                    Current.CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true;
                    Current.CanNext = controls?.IsNextEnabled == true;
                    Current.CanPrevious = controls?.IsPreviousEnabled == true;
                }
            }
            catch { }

            var now = DateTime.Now;

            // Yandex Music only syncs the timeline at state changes (track, seek, pause),
            // so adopt a fresh anchor only when it actually moved. Backward moves mean a
            // track restart or seek-back and must always be adopted, even within 2 seconds.
            // After a track change the reported position can still be the previous track's,
            // so adoption is suppressed for a short window.
            if (haveSync && now >= _anchorSuppressedUntil)
            {
                bool movedBackward = synced.TotalSeconds < _lastSynced.TotalSeconds - 0.3;
                bool movedForward = synced.TotalSeconds > _lastSynced.TotalSeconds + 2;
                if (_lastSynced == TimeSpan.MinValue || movedBackward || movedForward)
                {
                    _lastSynced = synced;
                    _lastPosition = synced;
                    _lastTick = now;
                }
            }

            if (Current.IsPlaying && duration > TimeSpan.Zero)
            {
                var elapsed = now - _lastTick;
                if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
                if (elapsed > TimeSpan.FromSeconds(5)) elapsed = TimeSpan.FromSeconds(5);
                if (_lastPosition > duration) _lastPosition = duration;
                _lastPosition += elapsed;
                _lastTick = now;
                if (_lastPosition < TimeSpan.Zero) _lastPosition = TimeSpan.Zero;
                if (_lastPosition > duration) _lastPosition = duration;
                Current.Position = _lastPosition;
            }
            else if (haveSync)
            {
                Current.Position = synced;
            }

            RaiseUpdated();
        }

        public async Task RefreshNowPlayingAsync()
        {
            if (_pollingNowPlaying) return;
            _pollingNowPlaying = true;
            try
            {
                if (_session == null)
                {
                    SetNoSession();
                    return;
                }
                var props = await _session.TryGetMediaPropertiesAsync();
                if (props == null)
                {
                    SetNoSession();
                    return;
                }

                var title = string.IsNullOrEmpty(props.Title) ? "Без названия" : props.Title;
                var artist = props.Artist ?? string.Empty;
                var album = props.AlbumTitle ?? string.Empty;
                bool trackChanged = title != Current.Title || artist != Current.Artist || album != Current.Album;

                if (trackChanged)
                {
                    DebugLog.Write("NowPlaying -> [" + title + "] by [" + artist + "]");
                    Current.Title = title;
                    Current.Artist = artist;
                    Current.Album = album;
                    Current.HasSession = true;
                    ResetPlaybackAnchor();
                    try { RefreshState(); }
                    catch { }
                }

                // Yandex Music populates the thumbnail a moment after the track changes,
                // so keep retrying until artwork is available.
                if (trackChanged || !Current.HasArtwork)
                {
                    try
                    {
                        if (props.Thumbnail != null)
                        {
                            using (var stream = await props.Thumbnail.OpenReadAsync())
                            {
                                var bmp = new BitmapImage();
                                bmp.DecodePixelType = DecodePixelType.Logical;
                                bmp.DecodePixelWidth = 120;
                                await bmp.SetSourceAsync(stream);
                                Current.AlbumArt = bmp;
                                Current.HasArtwork = true;
                                DebugLog.Write("Artwork set");
                            }
                        }
                        else if (trackChanged)
                        {
                            DebugLog.Write("Artwork: no thumbnail yet, will retry");
                            Current.HasArtwork = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write("Artwork failed: " + ex.Message);
                        Current.HasArtwork = false;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("RefreshNowPlaying failed: " + ex.Message);
                SetNoSession();
            }
            finally
            {
                _pollingNowPlaying = false;
            }
            RaiseUpdated();
        }

        public void ResetPlaybackAnchor()
        {
            _lastPosition = TimeSpan.Zero;
            _lastTick = DateTime.Now;
            _lastSynced = TimeSpan.Zero;
            _anchorSuppressedUntil = DateTime.Now + TimeSpan.FromSeconds(3);
        }

        private void SetNoSession()
        {
            Current.Title = "Нет трека";
            Current.Artist = "Включите музыку в Яндекс Музыке";
            Current.Album = string.Empty;
            Current.HasSession = false;
            Current.HasArtwork = false;
            Current.IsPlaying = false;
            Current.CanPlayPause = Current.CanNext = Current.CanPrevious = false;
            Current.Position = TimeSpan.Zero;
            Current.Duration = TimeSpan.Zero;
            _lastSynced = TimeSpan.MinValue;
            _lastPosition = TimeSpan.Zero;
            _anchorSuppressedUntil = DateTime.Now;
        }

        private void RaiseUpdated()
        {
            Updated?.Invoke(this, Current);
        }

        public async Task<bool> SeekAsync(TimeSpan position)
        {
            if (_session == null) return false;
            if (position < TimeSpan.Zero) position = TimeSpan.Zero;
            if (Current.Duration > TimeSpan.Zero && position > Current.Duration) position = Current.Duration;
            try
            {
                bool ok = await _session.TryChangePlaybackPositionAsync(position.Ticks);
                if (ok)
                {
                    _lastPosition = position;
                    _lastTick = DateTime.Now;
                    _lastSynced = TimeSpan.Zero;
                    _anchorSuppressedUntil = DateTime.Now + TimeSpan.FromSeconds(3);
                    RefreshState();
                }
                return ok;
            }
            catch { return false; }
        }

        public async Task<bool> TogglePlayPauseAsync()
        {
            if (_session == null) return false;
            try
            {
                var info = _session.GetPlaybackInfo();
                if (info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    return await _session.TryPauseAsync();
                return await _session.TryPlayAsync();
            }
            catch { return false; }
        }

        public async Task<bool> NextAsync()
        {
            if (_session == null) return false;
            try { return await _session.TrySkipNextAsync(); }
            catch { return false; }
        }

        public async Task<bool> PreviousAsync()
        {
            if (_session == null) return false;
            try { return await _session.TrySkipPreviousAsync(); }
            catch { return false; }
        }
    }
}
