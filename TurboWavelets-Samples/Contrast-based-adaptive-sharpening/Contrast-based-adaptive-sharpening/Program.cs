using System;
using System.Drawing;
using System.Drawing.Imaging;
using TurboWavelets;

namespace TurbowaveletsSamples
{
	class ContrastBasedAdaptiveShapeningClass
	{
		
		private static void applyAdaptiveShapening (float[,] array, float position)
		{
			int width = array.GetLength (0);
			int height = array.GetLength (1);
			Biorthogonal53Wavelet2D wavelet53 = new Biorthogonal53Wavelet2D (width, height);
			OrderWavelet2D waveletOrder = new OrderWavelet2D (width, height);
            
			wavelet53.EnableCaching = true;
			waveletOrder.EnableCaching = true;
			wavelet53.TransformIsotropic2D (array);
			//Reverse the ordering of the coefficients
			waveletOrder.BacktransformIsotropic2D (array);
			float[] scale = new float[8 * 8];

			for (int x=0; x < 8*8; x++) {
				scale [x] = 1.0f + 2.0f / ( (position - x) * (position- x) + 1.0f);
			}
			waveletOrder.ScaleCoefficients(array, scale, 8);
			waveletOrder.TransformIsotropic2D (array);
			wavelet53.BacktransformIsotropic2D (array);
		}

		public static void Main (string[] args)
		{
			Bitmap bmp = new Bitmap ("../../sample.png");
			float[,] yArray = new float[bmp.Width, bmp.Height];
			float[,] cbArray = new float[bmp.Width, bmp.Height];
			float[,] crArray = new float[bmp.Width, bmp.Height];
			float[,] aArray = new float[bmp.Width, bmp.Height];

			ImageArrayConverter.BitmapToAYCbCrArrays (bmp, aArray, yArray, cbArray, crArray); 
			applyAdaptiveShapening (yArray, 5.0f);
			ImageArrayConverter.AYCbCrArraysToBitmap (aArray, yArray, cbArray, crArray, bmp);
			bmp.Save ("test.png", ImageFormat.Png);
		}
	}
}
