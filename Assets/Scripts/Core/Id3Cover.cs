using System;
using System.IO;
using System.Text;

namespace MuseoAutista
{
    /// <summary>Extrae la portada (frame APIC) de la etiqueta ID3v2.3/2.4 de un MP3.</summary>
    public static class Id3Cover
    {
        public static byte[] FromFile(string path)
        {
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var header = new byte[10];
                    if (fs.Read(header, 0, 10) < 10 || header[0] != 'I' || header[1] != 'D' || header[2] != '3') return null;
                    int size = SyncSafe(header, 6);
                    var data = new byte[10 + size];
                    Array.Copy(header, data, 10);
                    fs.Read(data, 10, size);
                    return FromBytes(data);
                }
            }
            catch { return null; }
        }

        public static byte[] FromBytes(byte[] d)
        {
            if (d == null || d.Length < 10 || d[0] != 'I' || d[1] != 'D' || d[2] != '3') return null;
            int ver = d[3];
            if (ver < 3) return null;
            int end = Math.Min(d.Length, 10 + SyncSafe(d, 6));
            int pos = 10;
            if ((d[5] & 0x40) != 0) pos += ver == 4 ? SyncSafe(d, 10) : BigEndian(d, 10) + 4;

            while (pos + 10 <= end)
            {
                if (d[pos] == 0) break;
                string id = Encoding.ASCII.GetString(d, pos, 4);
                int fsize = ver == 4 ? SyncSafe(d, pos + 4) : BigEndian(d, pos + 4);
                int start = pos + 10;
                if (fsize <= 0 || start + fsize > d.Length) break;
                if (id == "APIC")
                {
                    int p = start;
                    byte enc = d[p++];
                    while (p < start + fsize && d[p] != 0) p++; // tipo MIME
                    p += 2;                                      // fin MIME + tipo de imagen
                    if (enc == 0 || enc == 3) { while (p < start + fsize && d[p] != 0) p++; p++; }
                    else { while (p + 1 < start + fsize && !(d[p] == 0 && d[p + 1] == 0)) p += 2; p += 2; }
                    int len = start + fsize - p;
                    if (len <= 0) return null;
                    var img = new byte[len];
                    Array.Copy(d, p, img, 0, len);
                    return img;
                }
                pos = start + fsize;
            }
            return null;
        }

        static int SyncSafe(byte[] b, int i) => (b[i] << 21) | (b[i + 1] << 14) | (b[i + 2] << 7) | b[i + 3];
        static int BigEndian(byte[] b, int i) => (b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3];
    }
}
