using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media.Imaging;

namespace YMusicGameBarWidget.Models
{
    public sealed class NowPlaying : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void Raise([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; Raise(); }
        }

        private string _artist;
        public string Artist
        {
            get => _artist;
            set { _artist = value; Raise(); }
        }

        private string _album;
        public string Album
        {
            get => _album;
            set { _album = value; Raise(); }
        }

        private BitmapImage _albumArt;
        public BitmapImage AlbumArt
        {
            get => _albumArt;
            set { _albumArt = value; Raise(); }
        }

        private bool _hasArtwork;
        public bool HasArtwork
        {
            get => _hasArtwork;
            set { _hasArtwork = value; Raise(); }
        }

        private bool _hasSession;
        public bool HasSession
        {
            get => _hasSession;
            set { _hasSession = value; Raise(); }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; Raise(); }
        }

        private bool _canPlayPause;
        public bool CanPlayPause
        {
            get => _canPlayPause;
            set { _canPlayPause = value; Raise(); }
        }

        private bool _canNext;
        public bool CanNext
        {
            get => _canNext;
            set { _canNext = value; Raise(); }
        }

        private bool _canPrevious;
        public bool CanPrevious
        {
            get => _canPrevious;
            set { _canPrevious = value; Raise(); }
        }

        private TimeSpan _position;
        public TimeSpan Position
        {
            get => _position;
            set
            {
                _position = value;
                Raise();
                Raise(nameof(PositionText));
                Raise(nameof(Progress));
            }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                Raise();
                Raise(nameof(DurationText));
                Raise(nameof(Progress));
            }
        }

        public string PositionText => Format(_position);

        public string DurationText => Format(_duration);

        public double Progress
        {
            get
            {
                if (_duration <= TimeSpan.Zero || _position <= TimeSpan.Zero) return 0;
                var value = _position.TotalSeconds / _duration.TotalSeconds;
                return value < 0 ? 0 : (value > 1 ? 1 : value);
            }
        }

        private static string Format(TimeSpan t)
        {
            if (t <= TimeSpan.Zero) return "0:00";
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
            return $"{(int)t.TotalMinutes}:{t.Seconds:D2}";
        }
    }
}
