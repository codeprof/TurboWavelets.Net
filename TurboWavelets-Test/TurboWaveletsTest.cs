// 
// TurboWaveletsTest.cs
//  
// Author:
//       Stefan Moebius
// Date:
//       2016-04-16
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
		float[,] valOrg;
		float[,] valTrans;
		int width, height;
		float progressValue;

		private bool logProgress (float progress)
		{
			progressValue = progress;
			Console.WriteLine (progress.ToString ());
			return false;
		}

		private void initTestData (Wavelet2D wavelet, bool simpleTestData = false)
		{
			Random rnd = new Random (1);
			this.wavelet = wavelet;
			this.width = wavelet.Width;
			this.height = wavelet.Height;
			test = new float[width, height];
			valOrg = new float[width, height];
			for (int x = 0; x < width; x++) {
				for (int y=0; y < height; y++) {
					if (simpleTestData) {
						test [x, y] = (float)(x + y);
					} else {
						test [x, y] = (float)(rnd.NextDouble () * 10.0 - 5.0);
					}
					valOrg [x, y] = test [x, y];
				}
			}
		}

		private bool isAllZero ()
		{
			bool isZero = true;
			for (int x = 0; x < this.width; x++) {
				for (int y = 0; y < this.height; y++) {
					if (Math.Abs (test [x, y]) > 0.00001f)
						isZero = false;
				}
			}
			return isZero;
		}

		private bool compareSource ()
		{
			bool isSame = true;
			for (int x = 0; x < this.width; x++) {
				for (int y = 0; y < this.height; y++) {
					if (Math.Abs (test [x, y] - valOrg [x, y]) > 0.001f)
						isSame = false;
				}
			}
			return isSame;
		}

		private bool compareTransformed ()
		{
			bool isSame = true;
			for (int x = 0; x < this.width; x++) {
				for (int y = 0; y < this.height; y++) {
					if (Math.Abs (test [x, y] - valTrans [x, y]) > 0.001f)
						isSame = false;
				}
			}
			return isSame;
		}

		private void performForwardAndBackwardTest (Wavelet2D wavelet, bool parallel, bool cached, bool simpleTestData = false, bool checkTransformationResults = false)
		{
			initTestData (wavelet, simpleTestData);
			wavelet.EnableParallel = parallel;
			wavelet.EnableCaching = cached;
			wavelet.TransformIsotropic2D (test);
			//Note: This test is wrong, if transformation does not change
			//the values in the array! For a array with all zeros this is the case
			if (!isAllZero ()) {
				Assert.IsFalse (compareSource ());
			}
			if (checkTransformationResults) {
				Assert.IsTrue (compareTransformed ());	
			}
			wavelet.BacktransformIsotropic2D (test);
			Assert.IsTrue (compareSource ());
		}
		
		private void performForwardAndBackwardTestCombined (Wavelet2D wavelet, bool simpleTestData = false, bool checkTransformationResults = false)
		{
			performForwardAndBackwardTest(wavelet, false, false, simpleTestData, checkTransformationResults);
			performForwardAndBackwardTest(wavelet, true,  false, simpleTestData, checkTransformationResults);
			performForwardAndBackwardTest(wavelet, false, true , simpleTestData, checkTransformationResults);
			performForwardAndBackwardTest(wavelet, true , true , simpleTestData, checkTransformationResults);
		}


		[SetUp]
		public void Setup ()
		{
		}

		[Test]
		public void testBiorthogonal53Wavelet2D_5x8 ()
		{
			performForwardAndBackwardTestCombined (new Biorthogonal53Wavelet2D (5, 8));
		}

		[Test]
		public void testBiorthogonal53Wavelet2D_68x111 ()
		{
			performForwardAndBackwardTestCombined (new Biorthogonal53Wavelet2D (68, 111));
		}

		[Test]
		public void testOrderWavelet2D_5x8 ()
		{
			performForwardAndBackwardTestCombined (new OrderWavelet2D (5, 8));
		}

		[Test]
		public void testOrderWavelet2D_68x111 ()
		{
			performForwardAndBackwardTestCombined (new OrderWavelet2D (68, 111));
		}

		[Test]
		public void testHaarWavelet2D_2x2 ()
		{
			valTrans = new float[2, 2];
			valTrans [0, 0] = 4;
			valTrans [1, 0] = 1;
			valTrans [0, 1] = 1;
			valTrans [1, 1] = 0;
			performForwardAndBackwardTestCombined (new HaarWavelet2D (2, 2), true, true);
		}

		[Test]
		public void testHaarWavelet2D_5x8 ()
		{
			performForwardAndBackwardTestCombined (new HaarWavelet2D (5, 8));
		}

		[Test]
		public void testHaarWavelet2D_68x111 ()
		{
			performForwardAndBackwardTestCombined (new HaarWavelet2D (68, 111));
		}

		[Test]
		public void testProgress ()
		{
			Wavelet2D.ProgressDelegate del = new Wavelet2D.ProgressDelegate (logProgress);
			initTestData (new Biorthogonal53Wavelet2D (64, 64));
			wavelet.TransformIsotropic2D (test, del);
			Assert.IsTrue (Math.Abs (100.0f - progressValue) < 0.00001f);
			wavelet.BacktransformIsotropic2D (test, del);
			Assert.IsTrue (Math.Abs (100.0f - progressValue) < 0.00001f);

			initTestData (new Biorthogonal53Wavelet2D (791, 324));
			wavelet.TransformIsotropic2D (test, del);
			Assert.IsTrue (Math.Abs (100.0f - progressValue) < 0.00001f);
			wavelet.BacktransformIsotropic2D (test, del);
			Assert.IsTrue (Math.Abs (100.0f - progressValue) < 0.00001f);
		}

		[Test]
		public void testRange ()
		{
			Wavelet2D wavelet = new OrderWavelet2D (512, 512);
			float[,] test = new float[512, 512];
			for (int y = 0; y < 512; y++) {
				for (int x = 0; x < 512; x++) {
					test[x,y] = 50.0f;
				}
			}
			float min;
			float max;
			wavelet.EnableParallel = false;
			wavelet.getCoefficientsRange(test, out min, out max);
			Assert.AreEqual(min, 50.0f);
			Assert.AreEqual(max, 50.0f);
			test[5,5] = 100.0f;
			test[10,100] = - 10.0f;
			wavelet.getCoefficientsRange(test, out min, out max);
			Assert.AreEqual(min, 10.0f);
			Assert.AreEqual(max, 100.0f);
			wavelet.EnableParallel = true;
			wavelet.getCoefficientsRange(test, out min, out max);
			Assert.AreEqual(min, 10.0f);
			Assert.AreEqual(max, 100.0f);
		}

		[Test]
		public void testCrop ()
		{
			Wavelet2D wavelet = new OrderWavelet2D (4, 4);
			float[,] test = new float[4, 4];
			for (int y = 0; y < 4; y++) {
				for (int x = 0; x < 4; x++) {
					test[x,y] = 50.0f;
				}
			}
			float min;
			float max;
			test[1,0] = 10.0f;
			test[0,1] = - 10.0f;
			wavelet.CropCoefficients(test, 10.1f);
			wavelet.EnableParallel = false;
			wavelet.getCoefficientsRange(test, out min, out max);
			Assert.AreEqual(0.0f, min);
			Assert.AreEqual(50.0f, max);
			test[1,0] = 10.0f;
			test[0,1] = - 10.0f;
			wavelet.CropCoefficients(test, 9.9f);
			wavelet.EnableParallel = true;
			wavelet.getCoefficientsRange(test, out min, out max);
			Assert.AreEqual(10.0f, min);
			Assert.AreEqual(50.0f, max);
		}

	}
}


















