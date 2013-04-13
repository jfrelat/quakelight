PNG Wrapper
===========

This is the implementation of a fast bitmap renderer for Silverlight 2.
Since there was no bitmap API available in Silverlight 2 (WriteableBitmap was only introduced in Silverlight 3), other strategies were required.

How it works
------------
For fast updates of the bitmap, a PNG format is built once in memory (PNG Wrapper).
Instead of encoding a PNG format for each bitmap, we only update pixels in memory.
Another trick used is to ignore the PNG CRC value (using 0).
This removes the overhead of computing a valid CRC for each update (adler32).

Restrictions
------------
* Only bitmaps with 256 colors palette are supported. This allows for palette effects without updating the bitmap.
For information, Quake is using 256 colors too.
* Silverlight is ignoring invalid CRC (the 0 value) but Moonlight does not work (because of the underlying Cairo library).
