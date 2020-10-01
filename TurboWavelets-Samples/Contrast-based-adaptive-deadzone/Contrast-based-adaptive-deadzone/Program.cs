using System;
using System.Drawing;
using System.Drawing.Imaging;
using TurboWavelets;

namespace TurbowaveletsSamples
{
	class ContrastBasedAdaptiveDeadzoneClass
	{
		
		private static void applyAdaptiveDeadzone (float[,] array, int numCoeffs)
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
			//Use numCoeffs cofficient out of 64 (8x8) -> for e.g. numCoeffs = 5 this
			//means a compression to 7,8% of the original size
			waveletOrder.CropCoefficients (array, 7, 8);
			waveletOrder.TransformIsotropic2D (array);
			wavelet53.BacktransformIsotropic2D (array);
		}

		public static void Main (string[] args)
		{
			Bitmap bmp = new Bitmap ("../../../sample.png");
			float[,] yArray = new float[bmp.Width, bmp.Height];
			float[,] cbArray = new float[bmp.Width, bmp.Height];
			float[,] crArray = new float[bmp.Width, bmp.Height];
			float[,] aArray = new float[bmp.Width, bmp.Height];

			ImageArrayConverter.BitmapToAYCbCrArrays (bmp, aArray, yArray, cbArray, crArray); 
			//setting ~95% of luminance coefficients to zero
			applyAdaptiveDeadzone (yArray, 3);
			//compress chroma even more (98,4%)
			applyAdaptiveDeadzone (cbArray, 1);
			applyAdaptiveDeadzone (crArray, 1);
			ImageArrayConverter.AYCbCrArraysToBitmap (aArray, yArray, cbArray, crArray, bmp);
			bmp.Save ("test.png", ImageFormat.Png);
		}
	}
}
