using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using XcpClient.Windows;
using XcpClient.Modules;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace XcpClient.Controls
{
    internal class MediaControl : Control
    {
        private sealed class VlcPlayer : IDisposable
        {
            private static class VlcApi
            {
                public static IntPtr libvlc_new(string[] arguments)
                {
                    return libvlc_new(arguments.Length, arguments);
                }

                public static IntPtr libvlc_media_new_path(IntPtr libvlc_instance, string path)
                {
                    return libvlc_media_new_path(libvlc_instance, Encoding.UTF8.GetBytes(path));
                }

                // ----------------------------------------------------------------------------------------
                // 以下是libvlc.dll导出函数

                // 创建一个libvlc实例，它是引用计数的
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr libvlc_new(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv);

                // 释放libvlc实例
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_release(IntPtr libvlc_instance_t);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr libvlc_get_version();

                // 从视频来源(例如Url)构建一个libvlc_meida
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr libvlc_media_new_location(IntPtr p_instance, [MarshalAs(UnmanagedType.LPArray)] byte[] psz_mrl);

                // 从本地文件路径构建一个libvlc_media
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr libvlc_media_new_path(IntPtr p_instance, [MarshalAs(UnmanagedType.LPArray)] byte[] psz_mrl);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_release(IntPtr libvlc_media_inst);

                // 创建libvlc_media_player(播放核心)
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern IntPtr libvlc_media_player_new(IntPtr p_libvlc_instance);

                // 将视频(libvlc_media)绑定到播放器上
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_set_media(IntPtr libvlc_media_player_t, IntPtr libvlc_media_t);

                // 设置图像输出的窗口
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_set_hwnd(IntPtr libvlc_mediaplayer, IntPtr libvlc_drawable);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_play(IntPtr libvlc_mediaplayer);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_pause(IntPtr libvlc_mediaplayer);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_stop(IntPtr libvlc_mediaplayer);

                // 解析视频资源的媒体信息(如时长等)
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_parse(IntPtr media);

                // 返回视频的时长(必须先调用libvlc_media_parse之后，该函数才会生效)
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern Int64 libvlc_media_get_duration(IntPtr p_md);

                // 当前播放的时间
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern Int64 libvlc_media_player_get_time(IntPtr libvlc_mediaplayer);

                // 设置播放位置(拖动)
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_set_time(IntPtr libvlc_mediaplayer, Int64 time);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_media_player_release(IntPtr libvlc_mediaplayer);

                // 获取和设置音量
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern int libvlc_audio_get_volume(IntPtr p_mi);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern int libvlc_audio_set_volume(IntPtr p_mi, int volume);

                // 设置全屏
                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern void libvlc_set_fullscreen(IntPtr p_mi, bool b_fullscreen);

                [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl)]
                public static extern bool libvlc_get_fullscreen(IntPtr p_mi);
            }

            private bool disposed;
            private IntPtr _instance;
            private IntPtr _player;
            private double duration_;

            public VlcPlayer(string pluginPath)
            {
                disposed = false;

                string plugin_arg = "--plugin-path=" + pluginPath;
                string[] arguments = { "-I", "dummy", "--ignore-config", "--no-video-title", plugin_arg };
                _instance = VlcApi.libvlc_new(arguments);
                _player = VlcApi.libvlc_media_player_new(_instance);
            }
            
            public double PlayTime
            {
                get { return VlcApi.libvlc_media_player_get_time(_player) / 1000.0; }
                set { VlcApi.libvlc_media_player_set_time(_player, (long)(value * 1000)); }
            }
            public int Volume
            {
                get { return VlcApi.libvlc_audio_get_volume(_player); }
                set { VlcApi.libvlc_audio_set_volume(_player, value); }
            }
            public bool FullScreen
            {
                get { return VlcApi.libvlc_get_fullscreen(_player); }
                set { VlcApi.libvlc_set_fullscreen(_player, value); }
            }
            public double Duration
            {
                get { return duration_; }
            }
            public string Version
            {
                get { return Marshal.PtrToStringAnsi(VlcApi.libvlc_get_version()); }
            }

            public void SetRenderWindow(IntPtr wndHandle)
            {
                if (_instance != IntPtr.Zero && wndHandle != IntPtr.Zero)
                {
                    VlcApi.libvlc_media_player_set_hwnd(_player, wndHandle);
                }
            }
            public void PlayFile(string filePath)
            {
                IntPtr libvlc_media = VlcApi.libvlc_media_new_path(_instance, filePath);
                if (libvlc_media != IntPtr.Zero)
                {
                    VlcApi.libvlc_media_parse(libvlc_media);
                    duration_ = VlcApi.libvlc_media_get_duration(libvlc_media) / 1000.0;

                    VlcApi.libvlc_media_player_set_media(_player, libvlc_media);
                    VlcApi.libvlc_media_release(libvlc_media);

                    VlcApi.libvlc_media_player_play(_player);
                }
            }
            public void Pause()
            {
                if (_player != IntPtr.Zero)
                {
                    VlcApi.libvlc_media_player_pause(_player);
                }
            }
            public void Stop()
            {
                if (_player != IntPtr.Zero)
                {
                    VlcApi.libvlc_media_player_stop(_player);
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (_player != IntPtr.Zero)
                    {
                        VlcApi.libvlc_media_player_release(_player);
                        _player = IntPtr.Zero;
                    }
                    if (_instance != IntPtr.Zero)
                    {
                        VlcApi.libvlc_release(_instance);
                        _instance = IntPtr.Zero;
                    }
                    disposed = true;
                }
            }
            ~VlcPlayer()
            {
                Dispose(false);
            }
        }

        private VlcPlayer _player;
        private List<Media> _medias;
        private int _index;

        public MediaControl()
        {
            _medias = new List<Media>();
            _index = -1;
            base.BackColor = Color.Black;
            base.BackgroundImageLayout = ImageLayout.Zoom;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { }
        }
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override ImageLayout BackgroundImageLayout
        {
            get { return base.BackgroundImageLayout; }
            set { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
            {
                _player = new VlcPlayer(string.Concat('"', Path.Combine(Environment.CurrentDirectory, "plugins"), '\\', '"'));
                _player.SetRenderWindow(Handle);
                _player.Volume = 100;
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (_player != null)
            {
                _player.Dispose();
                _player = null;
            }
            base.Dispose(disposing);
        }

        public async Task LoadConfig(string url)
        {
            _index = -1;
            _medias.Clear();
            try { Directory.Delete(FileSystem.GetPath("ad"), true); }
            catch (Exception) { }

#if (DEBUG)
            await LoadMedia(MediaType.Video, "G:\\Downloads\\麦DW和我妈妈.720p.HD国语中字【6v电影www.6vhao.net】.mp4", -1, 0);
            await LoadMedia(MediaType.Image, "http://pic.4j4j.cn/upload/pic/20130704/fcdf38dcf7.jpg", 3, 1);
            await LoadMedia(MediaType.Image, "http://image.tianjimedia.com/uploadImages/2013/070/KYH2Q1WV83C9.jpg", 3, 2);
            await LoadMedia(MediaType.Image, "http://pic4.bbzhi.com/mingxingbizhi/gaoqingqingliangmeinvsheyingbizhi/gaoqingqingliangmeinvsheyingbizhi_492993_10.jpg", 3, 3);
#endif
        }
        private async Task LoadMedia(MediaType type, string url, int time, int index)
        {
            try
            {
                if (url.StartsWith("http"))
                {
                    FileInfo file = new FileInfo(Path.Combine(FileSystem.GetPath("ad"), index.ToString()));
                    if (!file.Directory.Exists)
                        file.Directory.Create();
                    WebClient web = new WebClient();
                    byte[] data = await web.DownloadDataTaskAsync(url);
                    using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
                        await fs.WriteAsync(data, 0, data.Length);
                    url = file.FullName;
                }
                _medias.Add(new Media(type, url, time));
                if (_index == -1)
                    _index = 0;
            }
            catch (Exception) { }
        }
        public void Stop()
        {
            _player.Stop();
            if (base.BackgroundImage != null)
            {
                Image img = base.BackgroundImage;
                base.BackgroundImage = null;
                img.Dispose();
            }
        }
        public bool ShowNext(out DateTime time)
        {
            Stop();
            try
            {
                if (_index > -1)
                {
                    if (_index >= _medias.Count)
                        _index = 0;
                    Media media = _medias[_index++];
                    int value = media.Time;
                    switch (media.Type)
                    {
                        case MediaType.Image:
                            {
                                using (StreamReader reader = new StreamReader(media.Url))
                                    base.BackgroundImage = Image.FromStream(reader.BaseStream);
                                if (value <= 1)
                                    value = 10;
                                time = DateTime.Now.AddSeconds(value);
                                return true;
                            }
                        case MediaType.Video:
                            {
                                _player.PlayFile(media.Url);
                                if (value <= 1)
                                    value = (int)_player.Duration;
                                time = DateTime.Now.AddSeconds(value);
                                return true;
                            }
                    }
                }
            }
            catch (Exception) { }
            time = DateTime.MinValue;
            return false;
        }
    }
}
