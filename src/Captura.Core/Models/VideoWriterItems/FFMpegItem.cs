﻿using System;
using System.Collections.Generic;
using Screna;
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace Captura.Models
{
    public class FFMpegItem : IVideoWriterItem
    {
        // MP4 (x264, AAC)
        public static FFMpegItem x264 { get; } = new FFMpegItem("Mp4 (x264 | AAC)", () => ".mp4", VideoQuality =>
        {
            var settings = ServiceProvider.Get<Settings>().FFMpeg.X264;

            // quality: 51 (lowest) to 0 (highest)
            var crf = (51 * (100 - VideoQuality)) / 99;

            return $"-vcodec libx264 -crf {crf} -pix_fmt {settings.PixelFormat} -preset {settings.Preset}";
        }, FFMpegAudioItem.Aac);

        // Avi (Xvid, Mp3)
        public static FFMpegItem Avi { get; } = new FFMpegItem("Avi (Xvid | Mp3)", () => ".avi", VideoQuality =>
        {
            // quality: 31 (lowest) to 1 (highest)
            var qscale = 31 - ((VideoQuality - 1) * 30) / 99;

            return $"-vcodec libxvid -qscale:v {qscale}";
        }, FFMpegAudioItem.Mp3);

        // Gif (No Audio)
        public static FFMpegItem Gif { get; } = new FFMpegItem("Gif (No Audio)", () => ".gif", VideoQuality => "", FFMpegAudioItem.Mp3);

        // MP4 (HEVC Intel QSV, AAC)
        public static FFMpegItem HEVC_QSV { get; } = new FFMpegItem("Mp4 (HEVC Intel QSV | AAC) (Skylake or above)", () => ".mp4",
            VideoQuality => "-vcodec hevc_qsv -load_plugin hevc_hw -q 2 -preset:v veryfast", FFMpegAudioItem.Aac);

        // MP4 (H.264 NVENC, AAC)
        public static FFMpegItem H264_NVENC { get; } = new FFMpegItem("Mp4 (H.264 NVENC | AAC) (Alpha)", () => ".mp4",
            VideoQuality => "-c:v h264_nvenc -profile high444p -pixel_format yuv444p -preset fast", FFMpegAudioItem.Aac);

        // MP4 (HEVC NVENC, AAC)
        public static FFMpegItem HEVC_NVENC { get; } = new FFMpegItem("Mp4 (HEVC NVENC | AAC) (Alpha)", () => ".mp4",
            VideoQuality => "-c:v hevc_nvenc -profile high444p -pixel_format yuv444p -preset slow", FFMpegAudioItem.Aac);

        public static IEnumerable<FFMpegItem> Items { get; } = new[]
        {
            x264,
            Avi,
            Gif,
            HEVC_QSV,
            H264_NVENC,
            HEVC_NVENC
        };
        
        FFMpegItem(string Name, Func<string> Extension, FFMpegVideoArgsProvider VideoArgsProvider, FFMpegAudioArgsProvider AudioArgsProvider)
            : this(Name, Extension)
        {
            _videoArgsProvider = VideoArgsProvider;
            _audioArgsProvider = AudioArgsProvider;
        }

        protected FFMpegItem(string Name, Func<string> Extension)
        {
            _name = Name;
            _extension = Extension;
        }

        public FFMpegItem(CustomFFMpegCodec CustomCodec) : this(CustomCodec.Name,
            () => CustomCodec.Extension)
        {
            _videoArgsProvider = VideoQuality => CustomCodec.Args;

            _audioArgsProvider = FFMpegAudioItem.Mp3;

            foreach (var audioItem in FFMpegAudioItem.Items)
            {
                if (audioItem.Name.Split(' ')[0] == CustomCodec.AudioFormat)
                {
                    _audioArgsProvider = audioItem.AudioArgsProvider;
                    break;
                }
            }
        }

        readonly Func<string> _extension;

        public string Extension => _extension?.Invoke();

        readonly string _name;
        readonly FFMpegVideoArgsProvider _videoArgsProvider;
        readonly FFMpegAudioArgsProvider _audioArgsProvider;
        public override string ToString() => _name;

        public virtual IVideoFileWriter GetVideoFileWriter(VideoWriterArgs Args)
        {
            return new FFMpegWriter(FFMpegVideoWriterArgs.FromVideoWriterArgs(Args, _videoArgsProvider, _audioArgsProvider));
        }

        public IVideoFileWriter GetVideoFileWriter(VideoWriterArgs Args, string OutputArgs)
        {
            var args = FFMpegVideoWriterArgs.FromVideoWriterArgs(Args, _videoArgsProvider, _audioArgsProvider);
            args.OutputArgs = OutputArgs;

            return new FFMpegWriter(args);
        }
    }
}
