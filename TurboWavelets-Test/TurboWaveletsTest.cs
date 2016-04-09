using System;
using TurboWavelets;

namespace TurboWavelets
{
	public class TurboWaveletsTest
	{

		public static void Main (string[] args)
		{

			float[,] test = new float[9, 12];
			float[,] validate = new float[9, 12];

			for (int x = 0; x < 9; x++) {
				for (int y=0; y < 12; y++) {
					test [x, y] = x * y;
					validate [x, y] = test [x, y];
				}
			}

			OrderWavelet2D sorter = new OrderWavelet2D (9, 12);
			Biorthogonal53Wavelet2D wavelet = new Biorthogonal53Wavelet2D (9, 12);
			wavelet.TransformIsotropic2D (test);
			sorter.BacktransformIsotropic2D (test);
			//sorter.CropCoefficients(test, 4, 8);
			sorter.TransformIsotropic2D (test);
			wavelet.BacktransformIsotropic2D (test);
			
			bool correct = true;
			for (int x = 0; x < 9; x++) {
				for (int y=0; y < 12; y++) {
					if (Math.Abs (test [x, y] - validate [x, y]) > 0.01f)
						correct = false;
				}
			}
			if (correct)
				Console.WriteLine ("results are correct");
			else
				Console.WriteLine ("results are INCORRECT!");
		}
	}
}

