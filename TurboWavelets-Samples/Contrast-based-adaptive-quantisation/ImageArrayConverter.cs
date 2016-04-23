using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace TurbowaveletsSamples
{
	public class ImageArrayConverter
	{
        static unsafe public void BitmapToAYCbCrArrays (Bitmap bmp, float[,] outA, float[,] outY, float[,] outCb, float[,] outCr)
        {
            if (bmp == null) {
                throw new ArgumentException ("bmp cannot be null!");
            }
            if (outA == null) {
                throw new ArgumentException ("outA cannot be null!");
            }
            if (outY == null) {
                throw new ArgumentException ("outY cannot be null!");
            }
            if (outCb == null) {
                throw new ArgumentException ("outCb cannot be null!");
            }
            if (outCr == null) {
                throw new ArgumentException ("outCr cannot be null!");
            }
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            for (int y = 0; y < bmp.Height; y++) {
				int* srcPtr = (int*)IntPtr.Add(data.Scan0, data.Stride * y);
                for (int x = 0; x < bmp.Width; x++) {
                    int colB = (*srcPtr) & 255;
                    int colG = (*srcPtr >> 8) & 255;
                    int colR = (*srcPtr >> 16) & 255;
                    int colA = (*srcPtr >> 24) & 255; 
					
                    outA [x, y]  = colA; 
                    outY [x, y]  = ( 0.2990f * colR + 0.5870f * colG + 0.1140f * colB + 0.5f);
                    outCb [x, y] = (-0.1687f * colR - 0.3313f * colG + 0.5000f * colB + 127.5f + 0.5f);
                    outCr [x, y] = ( 0.5000f * colR - 0.4187f * colG - 0.0813f * colB + 127.5f + 0.5f);
                    srcPtr++;
                }
            }
			bmp.UnlockBits(data);
        }

       static unsafe public void AYCbCrArraysToBitmap(float[,] inA, float[,] inY, float[,] inCb, float[,] inCr, Bitmap bmp)
        {
            if (inA == null) {
                throw new ArgumentException ("inA cannot be null!");
            }
            if (inY == null) {
                throw new ArgumentException ("inY cannot be null!");
            }
            if (inCb == null) {
                throw new ArgumentException ("inCb cannot be null!");
            }
            if (inCr == null) {
                throw new ArgumentException ("inCr cannot be null!");
            }
            if (bmp == null) {
                throw new ArgumentException ("bmp cannot be null!");
            }
			Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
			BitmapData data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
 
            for (int posY = 0; posY < bmp.Height; posY++) {
				int* dstPtr = (int*)IntPtr.Add(data.Scan0, data.Stride * posY);
                for (int posX = 0; posX < bmp.Width; posX++) {
                    float a = inA [posX, posY]; 
                    float y = inY [posX, posY];
                    float cb = inCb [posX, posY] - 127.5f;
                    float cr = inCr [posX, posY] - 127.5f;
                    
                    int aInt = (int)(a);
                    int rInt = (int)(y + 1.40200f * cr + 0.5f);
                    int gInt = (int)(y - 0.34414f * cb - 0.71417f * cr + 0.5f);
                    int bInt = (int)(y + 1.77200f * cb + 0.5f);

                    if (aInt < 0) {
                        aInt = 0;
                    } else if (aInt > 255) {
                        aInt = 255;
                    }
                    if (rInt < 0) {
                        rInt = 0;
                    } else if (rInt > 255) {
                        rInt = 255;
                    }
                    if (gInt < 0) {
                        gInt = 0;
                    } else if (gInt > 255) {
                        gInt = 255;
                    }
                    if (bInt < 0) {
                        bInt = 0;
                    } else if (bInt > 255) {
                        bInt = aInt;
                    }
                    *dstPtr = (int)(bInt + (gInt << 8) + (rInt << 16) + (aInt << 24));
                    dstPtr++;
                }
            }
			bmp.UnlockBits(data);
        }

	}
}

