using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pie.Services
{
    public class MediaService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;
        private const byte VK_MEDIA_STOP = 0xB2;
        private const byte VK_VOLUME_UP = 0xAF;
        private const byte VK_VOLUME_DOWN = 0xAE;
        private const byte VK_VOLUME_MUTE = 0xAD;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public void PlayPause()
        {
            SendMediaKey(VK_MEDIA_PLAY_PAUSE);
        }

        public void NextTrack()
        {
            SendMediaKey(VK_MEDIA_NEXT_TRACK);
        }

        public void PreviousTrack()
        {
            SendMediaKey(VK_MEDIA_PREV_TRACK);
        }

        public void Stop()
        {
            SendMediaKey(VK_MEDIA_STOP);
        }

        public void VolumeUp()
        {
            SendMediaKey(VK_VOLUME_UP);
        }

        public void VolumeDown()
        {
            SendMediaKey(VK_VOLUME_DOWN);
        }

        public void ToggleMute()
        {
            SendMediaKey(VK_VOLUME_MUTE);
        }

        private void SendMediaKey(byte vk)
        {
            keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
