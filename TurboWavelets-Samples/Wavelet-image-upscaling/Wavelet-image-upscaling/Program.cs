using System;
using System.Drawing;
using System.Drawing.Imaging;
using TurboWavelets;

namespace TurbowaveletsSamples
{
	class WaveletImageUpscalingClass
	{
		
		public static void Main (string[] args)
		{
			Bitmap bmp = new Bitmap ("../../../sample.png");

			Bitmap bmp2 = new Bitmap (2 * bmp.Width, 2 * bmp.Height, PixelFormat.Format32bppArgb);
			float[,] yArray2 = new float[bmp2.Width, bmp2.Height];
			float[,] cbArray2 = new float[bmp2.Width, bmp2.Height];
			float[,] crArray2 = new float[bmp2.Width, bmp2.Height];
			float[,] aArray2 = new float[bmp2.Width, bmp2.Height];

			ImageArrayConverter.BitmapToAYCbCrArrays (bmp, aArray2, yArray2, cbArray2, crArray2); 

			Biorthogonal53Wavelet2D wavelet = new Biorthogonal53Wavelet2D (bmp.Width, bmp.Height);
			Biorthogonal53Wavelet2D wavelet2 = new Biorthogonal53Wavelet2D (bmp2.Width, bmp2.Height);
			
			wavelet.TransformIsotropic2D (aArray2);
			wavelet.TransformIsotropic2D (yArray2);
			wavelet.TransformIsotropic2D (cbArray2);
			wavelet.TransformIsotropic2D (crArray2);

			wavelet2.BacktransformIsotropic2D (aArray2);
			wavelet2.BacktransformIsotropic2D (yArray2);
			wavelet2.BacktransformIsotropic2D (cbArray2);
			wavelet2.BacktransformIsotropic2D (crArray2);
			for (int y=0; y < bmp2.Height; y++) {
				for (int x=0; x < bmp2.Width; x++) {
					aArray2[x,y] *= 4.0f;
					yArray2[x,y] *= 4.0f;
					cbArray2[x,y] *= 4.0f;
					crArray2[x,y] *= 4.0f;
				}
			}
			ImageArrayConverter.AYCbCrArraysToBitmap (aArray2, yArray2, cbArray2, crArray2, bmp2);
			bmp2.Save ("test.png", ImageFormat.Png);
		}
	}
}
