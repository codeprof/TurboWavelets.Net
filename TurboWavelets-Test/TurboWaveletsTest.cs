// 
// TurboWaveletsTest.cs
//  
// Author:
//       Stefan Moebius
// Date:
//       2016-04-09
// 
// Copyright (c) 2016 Stefan Moebius
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using TurboWavelets;
using NUnit.Framework;

namespace TurboWaveletsTests
{
	[TestFixture]
	public class TurboWaveletsTest
	{
		Wavelet2D wavelet;
		float[,] test;
		float[,] val; 
		int width, height;
		Wavelet2D.ProgressDelegate del;

		public bool logProgress (float progress)
		{
			Console.WriteLine(progress.ToString());
			return false;
		}

		public void init(Wavelet2D wavelet)
		{
			del = new Wavelet2D.ProgressDelegate(logProgress);
			this.wavelet = wavelet;
			this.width = wavelet.Width;
			this.height = wavelet.Height;
			test = new float[width, height];
			val = new float[width, height];
			for (int x = 0; x < width; x++) {
				for (int y=0; y < height; y++) {
					test [x, y] = x * y - 4 * x + 7 * y + 5.5f;
					val  [x, y] = test [x, y];
				}
			}
		}

		public bool compare()
		{
			bool isSame = true;
			for (int x = 0; x < this.width; x++) {
				for (int y=0; y < this.height; y++) {
					if (Math.Abs (test [x, y] - val [x, y]) > 0.001f)
						isSame = false;
				}
			}
			return isSame;
		}

		[SetUp] public void Init() 
		{
		}


		[Test]
		public void testBiorthogonal53Wavelet2D_5x8()
		{
			init(new Biorthogonal53Wavelet2D (5, 8));
			wavelet.TransformIsotropic2D (test, del);
			Assert.IsFalse(compare());
			wavelet.BacktransformIsotropic2D(test);
			Assert.IsTrue(compare());
		}

		[Test]
		public void testBiorthogonal53Wavelet2D_68x111 ()
		{
			init (new Biorthogonal53Wavelet2D (1687, 1871));
			wavelet.TransformIsotropic2D (test, del);
			Assert.IsFalse(compare());
			wavelet.BacktransformIsotropic2D(test, del);
			Assert.IsTrue(compare());
		}


		/*
		public static void Main (string[] args)
		{
			TurboWaveletsTest o = new TurboWaveletsTest();
			o.testBiorthogonal53Wavelet2D_5x8();
			o.testBiorthogonal53Wavelet2D_68x111();
			
			Assert.Pass("Passed");
		}
*/
	}
}


















