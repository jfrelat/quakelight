using System;
using System.IO;

namespace InnoveWare
{
    public class PngWrapper
    {
        private static byte[] _HEADER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static byte[] _IHDR = { (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
        private static byte[] _GAMA = { (byte)'g', (byte)'A', (byte)'M', (byte)'A' };
        private static byte[] _PLTE = { (byte)'P', (byte)'L', (byte)'T', (byte)'E' };
        private static byte[] _IDAT = { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };
        private static byte[] _IEND = { (byte)'I', (byte)'E', (byte)'N', (byte)'D' };
        private static byte[] _ZLIB = { 0x78, 0xDA };
        private static byte[] _PNG8 = { 8, 3, 0, 0, 0 };
        private MemoryStream _ms;
        private int _width;
        private int _height;
        private byte[] _data;
        private int _data_offset;
        private byte[] _palette;
        private int _palette_offset;

        private static int LittleEndian(int data)
        {
            return (int)(((data & 0xFF) << 24) | ((data & 0xFF00) << 8) | ((data & 0xFF0000) >> 8) | ((data & 0xFF000000) >> 24));
        }

        public PngWrapper(byte[] palette, byte[] data, int width, int height)
        {
            /* Set parameters. */
            _width = width;
            _height = height;
            _data = data;
            _palette = palette;

            /* Initialize Memory Stream. */
            _ms = new MemoryStream();

            /* Write PNG header. */
            _ms.Write(_HEADER, 0, _HEADER.Length);

            // Write IHDR chunk
            _ms.Write(BitConverter.GetBytes(LittleEndian(13)), 0, 4);
            _ms.Write(_IHDR, 0, _IHDR.Length);
            _ms.Write(BitConverter.GetBytes(LittleEndian(_width)), 0, 4);
            _ms.Write(BitConverter.GetBytes(LittleEndian(_height)), 0, 4);
            _ms.Write(_PNG8, 0, _PNG8.Length);
            _ms.Write(BitConverter.GetBytes(0), 0, 4);

            // Write gAMA chunk
            /*_ms.Write(BitConverter.GetBytes(LittleEndian(4)), 0, 4);
            _ms.Write(_GAMA, 0, _GAMA.Length);
            _ms.Write(BitConverter.GetBytes(1 * 100000), 0, 4);
            _ms.Write(BitConverter.GetBytes(0), 0, 4);*/

            // Write PLTE chunk
            _ms.Write(BitConverter.GetBytes(LittleEndian(_palette.Length)), 0, 4);
            _ms.Write(_PLTE, 0, _PLTE.Length);
            _palette_offset = (int)_ms.Position;    // Remember position for palette
            _ms.Write(_palette, 0, _palette.Length);
            _ms.Write(BitConverter.GetBytes(0), 0, 4);

            // Write IDAT chunk
            _ms.Write(BitConverter.GetBytes(LittleEndian(2 + (6 + _width) * _height)), 0, 4);
            _ms.Write(_IDAT, 0, _IDAT.Length);
            _ms.Write(_ZLIB, 0, _ZLIB.Length);

            // Remember position for data
            _data_offset = (int)_ms.Position + 6;

            /* Loop on all blocks. */
            int rowSize = _width + 1;
            int ofs = 0;
            for (int y = 0; y < _height; y++)
            {
                /* Write flag. */
                if (y == _height - 1)
                    _ms.WriteByte(0x01);
                else
                    _ms.WriteByte(0x00);

                /* Write length. */
                _ms.Write(BitConverter.GetBytes(rowSize), 0, 2);
                _ms.Write(BitConverter.GetBytes(~rowSize), 0, 2);

                /* Write filter bit. */
                _ms.WriteByte(0x00);

                /* Write data blocks. */
                _ms.Write(_data, ofs, _width);
                ofs += _width;
            }

            // Write fake CRC
            _ms.Write(BitConverter.GetBytes(0), 0, 4);

            // Write IEND chunk
            _ms.Write(BitConverter.GetBytes(LittleEndian(0)), 0, 4);
            _ms.Write(_IEND, 0, _IEND.Length);
            _ms.Write(BitConverter.GetBytes(0), 0, 4);
        }

        public void UpdatePalette()
        {
            /* Write PLTE chunk. */
            _ms.Seek(_palette_offset, SeekOrigin.Begin);
            _ms.Write(_palette, 0, _palette.Length);
        }

        public void UpdateBitmap()
        {
            /* Loop on all rows. */
            int ofs = 0;
            int seek_ofs = _data_offset;
            for (int y = 0; y < _height; y++)
            {
                /* Skip to the writing position. */
                _ms.Seek(seek_ofs, SeekOrigin.Begin);
                seek_ofs += _width + 6;
                
                /* Write data block. */
                _ms.Write(_data, ofs, _width);
                ofs += _width;
            }
        }

        public void UpdateBitmap(int x, int y, int width, int height)
        {
            /* Loop on all rows. */
            int ofs = _width * y + x;
            int seek_ofs = _data_offset + (_width + 6) * y + x;
            for (int r = 0; r < height; r++)
            {
                /* Skip to the writing position. */
                _ms.Seek(seek_ofs, SeekOrigin.Begin);
                seek_ofs += _width + 6;

                /* Write data block. */
                _ms.Write(_data, ofs, width);
                ofs += _width;
            }
        }

        public Stream GetStream()
        {
            /* Return current stream. */
            return _ms;
        }
    }
}
