using System;
using System.IO;
using System.Media;
using System.Windows;
using NAudio.Wave;
using Pie.Models;

namespace Pie.Services
{
    public class SoundService
    {
        private readonly SettingsService _settingsService;
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioReader;

        public SoundService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void PlayActivateSound()
        {
            if (!_settingsService.Settings.SoundsEnabled) return;
            PlaySystemSound(SystemSounds.Asterisk);
        }

        public void PlayHoverSound()
        {
            if (!_settingsService.Settings.SoundsEnabled) return;
            // Use a subtle click sound
            PlaySystemSound(SystemSounds.Hand);
        }

        public void PlaySelectSound()
        {
            if (!_settingsService.Settings.SoundsEnabled) return;
            PlaySystemSound(SystemSounds.Exclamation);
        }

        public void PlayCloseSound()
        {
            if (!_settingsService.Settings.SoundsEnabled) return;
            // Subtle close sound
        }

        private void PlaySystemSound(SystemSound sound)
        {
            try
            {
                sound.Play();
            }
            catch { }
        }

        public void PlayCustomSound(string soundPath)
        {
            if (!_settingsService.Settings.SoundsEnabled) return;
            if (!File.Exists(soundPath)) return;

            try
            {
                StopCurrentSound();

                _audioReader = new AudioFileReader(soundPath)
                {
                    Volume = (float)_settingsService.Settings.SoundVolume
                };
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioReader);
                _waveOut.Play();
            }
            catch { }
        }

        private void StopCurrentSound()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioReader?.Dispose();
            _waveOut = null;
            _audioReader = null;
        }

        public void Dispose()
        {
            StopCurrentSound();
        }
    }
}
