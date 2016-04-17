// 
// OrderWavelet2D.cs
//  
// Author:
//       Stefan Moebius
// Date:
//       2016-04-17
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

namespace TurboWavelets
{
	public class OrderWavelet2D : Wavelet2D
	{
		/// <summary>
		/// A fast implementation of a two-dimensional ordering transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// This does not perform a complete wavelet transformation. It
		/// just does the cascade ordering of the values.
		/// The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
		public OrderWavelet2D (int width, int height)
            : base(2, 2, width, height)
		{   
		}

		/// <summary>
		/// A fast implementation of a two-dimensional ordering transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// This does not perform a complete wavelet transformation. It
		/// just does the cascade ordering of the values.
		/// The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
		/// <param name="minSize">Minimum width/height up to the transformation should be applied</param>
		public OrderWavelet2D (int width, int height, int minSize)
            : base(minSize, 2, width, height)
		{
		}

		override protected void TransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= allowedMinSize) {
				int half = length >> 1;
				int offSrc = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					dst [i, y] = src [offSrc, y];
					dst [i + numLFValues, y] = src [offSrc + 1, y];
					offSrc += 2;
				}							
				if ((length & 1) != 0)
					dst [numLFValues - 1, y] = src [length - 1, y];
			} else {
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];
			}
		}

		override protected void TransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= allowedMinSize) {
				int half = length >> 1;
				int offSrc = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					dst [x, i] = src [x, offSrc];
					dst [x, i + numLFValues] = src [x, offSrc + 1];
					offSrc += 2;
				}							
				if ((length & 1) != 0)
					dst [x, numLFValues - 1] = src [x, length - 1];
			} else {
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];
			}
		}

		override protected void InvTransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= allowedMinSize) {
				int half = length >> 1;
				int offDst = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					dst [offDst, y] = src [i, y];
					dst [offDst + 1, y] = src [i + numLFValues, y];
					offDst += 2;
				}							
				if ((length & 1) != 0)
					dst [length - 1, y] = src [numLFValues - 1, y]; 
			} else {
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];
			}
		}

		override protected void InvTransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= allowedMinSize) {
				int half = length >> 1;
				int offDst = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					dst [x, offDst] = src [x, i];
					dst [x, offDst + 1] = src [x, i + numLFValues];
					offDst += 2;
				}							
				if ((length & 1) != 0)
					dst [x, length - 1] = src [x, numLFValues - 1]; 
			} else {
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];
			}
		}
	}
}