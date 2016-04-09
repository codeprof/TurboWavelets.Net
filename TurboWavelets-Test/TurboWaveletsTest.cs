using System;
using TurboWavelets;

namespace TurboWavelets
{
	public class TurboWaveletsTest
	{

		public static void Main (string[] args)
		{

			float[,] src = new float[9, 12];
			float[,] dst = new float[9, 12];

			for (int x = 0; x < 9; x++) {
				for (int y=0; y < 12; y++) {
					src [x, y] = x * y;
				}
			}
			OrderWavelet2D sorter = new OrderWavelet2D(9, 12);
			Biorthogonal53Wavelet2D wavelet = new Biorthogonal53Wavelet2D (9, 12);
			wavelet.TransformIsotropic2D (src);
			sorter.BacktransformIsotropic2D(src);
			sorter.CropCoefficients(src, 1, 8);
			sorter.TransformIsotropic2D(src);
			wavelet.BacktransformIsotropic2D(src);
		}
	}
}

