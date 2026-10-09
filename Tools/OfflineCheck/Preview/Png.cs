using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/// <summary>Winziger PNG-Encoder (RGBA, ohne Abhängigkeiten).</summary>
static class Png
{
    public static void Save(string path, Color[] px, int w, int h, bool flipY = true)
    {
        var raw = new byte[(w * 4 + 1) * h];
        int k = 0;
        for (int row = 0; row < h; row++)
        {
            int y = flipY ? h - 1 - row : row;
            raw[k++] = 0;
            for (int x = 0; x < w; x++)
            {
                var c = px[y * w + x];
                raw[k++] = B(c.r); raw[k++] = B(c.g); raw[k++] = B(c.b); raw[k++] = B(c.a);
            }
        }
        using var f = File.Create(path);
        f.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, w); BE(ihdr, 4, h); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(f, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, true)) z.Write(raw);
            Chunk(f, "IDAT", ms.ToArray());
        }
        Chunk(f, "IEND", Array.Empty<byte>());
    }

    static byte B(float v) => (byte)(Mathf.Clamp01(v) * 255f + 0.5f);

    static void BE(byte[] b, int o, int v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }

    static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, data.Length); s.Write(len);
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        s.Write(t); s.Write(data);
        uint crc = Crc(t, 0xFFFFFFFFu); crc = Crc(data, crc) ^ 0xFFFFFFFFu;
        var c = new byte[4]; BE(c, 0, (int)crc); s.Write(c);
    }

    static uint[] _table;
    static uint Crc(byte[] d, uint crc)
    {
        if (_table == null)
        {
            _table = new uint[256];
            for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1; _table[n] = c; }
        }
        foreach (var b in d) crc = _table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }
}
