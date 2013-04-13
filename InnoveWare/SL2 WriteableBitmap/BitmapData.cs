using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace InnoveWare
{
    public class BitmapData
    {
        private int _width = 0;
        private int _height = 0;
        private PngWrapper _wrapper;

        public BitmapData(byte[] palette, byte[] buffer, int width, int height)
        {
            /* Initialize members. */
            _width = width;
            _height = height;

            /* Create a 8-bit PNG wrapper. */
            _wrapper = new PngWrapper(palette, buffer, width, height);
        }

        public void UpdatePalette()
        {
            _wrapper.UpdatePalette();
        }

        public void UpdateBitmap()
        {
            _wrapper.UpdateBitmap();
        }

        public void UpdateBitmap(int x, int y, int width, int height)
        {
            _wrapper.UpdateBitmap(x, y, width, height);
        }

        public void Blit(BitmapImage bitmap)
        {
            /* Update the bitmap source with updated palette and/or bitmap. */
            bitmap.SetSource(_wrapper.GetStream());
        }
    }
}
