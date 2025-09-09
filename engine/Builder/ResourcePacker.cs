using EmeralEngine.Core;
using NAudio.Wave;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using ZstdNet;

namespace EmeralEngine.Builder
{
    internal class ResourcePacker
    {
        private const int MAX_SIZE = 1073741824;
        private const int SAMPLE_RATE = 48000;
        private int FileCount;
        private Dictionary<int, List<string>> Groups;
        public ResourcePacker(string[] resources)
        {
            // 良い感じに仕分け
            Groups = new();
            FileCount = 0;
            var d = new SortedList<long, string>();
            foreach (var f in resources)
            {
                d.Add((new FileInfo(f)).Length, f);
                FileCount++;
            }
            var sizes = new List<long>();
            var num = 1;
            var file = "data1.dat";
            var group = new List<string>();
            Groups.Add(num, group);
            foreach (var f in d.Reverse())
            {
                group = Groups[num];
                if (sizes.Sum() + f.Key <= MAX_SIZE || group.Count == 0)
                {
                    group.Add(f.Value);
                    sizes.Add(f.Key);
                }
                else
                {
                    num++;
                    file = $"data{num}.dat";
                    group = new();
                    group.Add(f.Value);
                    sizes.Clear();
                    sizes.Add(f.Key);
                    Groups.Add(num, group);
                }
            }
        }

        public Dictionary<string, string> Pack(string dest, FilePackingData data)
        {
            var table = new Dictionary<string, string>();
            var json = new Dictionary<string, object>();
            data.Length = FileCount;
            data.FinishedCount = 0;
            foreach (var g in Groups)
            {
                var start = 0;
                using (var f = new FileStream(Path.Combine(dest, $"data{g.Key}.dat"), FileMode.Create, FileAccess.Write))
                {
                    foreach (var p in g.Value)
                    {
                        var random = new byte[2];
                        data.FileName = Path.GetFileName(p);
                        byte[] b;
                        if (Utils.IsAudio(p))
                        {
                            b = LoadAudio(p);
                        }
                        else
                        {
                            b = File.ReadAllBytes(p);
                        }
                        using (var r = RandomNumberGenerator.Create())
                        using (var z = new Compressor())
                        {
                            var c = z.Wrap(b);
                            r.GetBytes(random);
                            c[0] = random[0];
                            c[1] = random[1];
                            f.Write(c);
                            var h = HashHelper.GetHash(b);
                            table.Add(MainWindow.pmanager.RelToResourcePath(p), h);
                            json.Add(h, new object[] { c.Length, start, g.Key });
                            start += c.Length;
                        }
                        data.FinishedCount++;
                    }
                }
            }
            File.WriteAllBytes(Path.Combine(dest, "data.dat"), Reverse(JsonSerializer.Serialize(json)));
            return table;
        }

        private byte[] LoadAudio(string file)
        {
            using (var reader = new MediaFoundationReader(file))
            {
                if (reader.WaveFormat.SampleRate == SAMPLE_RATE)
                {
                    using (var ms = new MemoryStream())
                    {
                        WaveFileWriter.WriteWavFileToStream(ms, reader);
                        return ms.ToArray();
                    }
                }
                var fmt = new WaveFormat(SAMPLE_RATE, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels);
                using (var resampler = new MediaFoundationResampler(reader, fmt))
                using (var ms = new MemoryStream())
                {
                    WaveFileWriter.WriteWavFileToStream(ms, resampler);
                    return ms.ToArray();
                }
            }
        }

        private static byte[] Reverse(string str)
        {
            var res = new List<byte>();
            foreach (var s in str)
            {
                res.Add((byte)~s);
            }
            return res.ToArray();
        }
    }

    struct ResourceInfo
    {
        public string Hash;
    }

    class HashHelper
    {
        public static string GetHash(byte[] b)
        {
            using (var sha256 = SHA256.Create())
            {
                return BitConverter.ToString(sha256.ComputeHash(b)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
