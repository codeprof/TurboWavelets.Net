// 
// HaarWavelet2D.cs
//  
// Author:
//       Stefan Moebius
// Date:
//       2016-04-24
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

namespace TurboWavelets
{
	/// <summary>
	/// Implements the two dimensional haar wavelet transformation for arbitrary sizes
	/// </summary>
	public class HaarWavelet2D : Wavelet2D
	{
		/// <summary>
		/// The allowed minimum transformation (limitation of the algorithmn implementation)
		/// </supmmary>
		protected const int   AllowedMinSize    = 2;
        /// <summary>
        /// scale factor
        /// </summary>
		protected const float Scale             = 2.0f;
        /// <summary>
        /// inverse scale factor
        /// </summary>
		protected const float InvScale          = 0.5f;
        /// <summary>
        /// factor for the mean of two values
        /// </summary>
		protected const float Mean              = 0.5f;
        /// <summary>
        /// inverse factor for the mean of two values
        /// </summary>
		protected const float InvMean           = 2.0f;

		/// <summary>
		/// A fast implementation of a two-dimensional haar transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
		public HaarWavelet2D (int width, int height)
            : base(AllowedMinSize, AllowedMinSize, width, height)
		{   
		}

		/// <summary>
		/// A fast implementation of a two-dimensional haar transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
		/// <param name="minSize">Minimum width/height up to the transformation should be applied</param>
		public HaarWavelet2D (int width, int height, int minSize)
            : base(minSize, AllowedMinSize, width, height)
		{
		}

        #pragma warning disable 1591 // do not show compiler warnings of the missing descriptions
		override protected void TransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= AllowedMinSize) {
				int half = length >> 1;
				int offSrc = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					float a = src [offSrc    , y];
					float b = src [offSrc + 1, y];
					//calculate the mean of a and b and scale with factor 2
					//So no multiplication needed at all
					dst [i              , y] = (a + b);
					dst [i + numLFValues, y] = (b - a) * Mean;
					offSrc += 2;
				}							
				if ((length & 1) != 0) {
					dst [numLFValues - 1, y] = src [length - 1, y] * Scale;
				}		
			} else {
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];
			}
		}

		override protected void TransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= AllowedMinSize) {
				int half = length >> 1;
				int offSrc = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					float a = src [x, offSrc    ];
					float b = src [x, offSrc + 1];
					//calculate the mean of a and b and scale with factor 2
					//So no multiplication needed at all
					dst [x, i              ] = (a + b);
					dst [x, i + numLFValues] = (b - a) * Mean;
					offSrc += 2;
				}							
				if ((length & 1) != 0) {
					dst [x, numLFValues - 1] = src [x, length - 1] * Scale;
				}	
			} else {
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];
			}
		}

		override protected void InvTransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= AllowedMinSize) {
				int half = length >> 1;
				int offDst = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					float a = src [i, y              ] * InvScale;
					float b = src [i + numLFValues, y];
					dst [offDst,     y] = a - b;
					dst [offDst + 1, y] = a + b;
					offDst += 2;
				}							
				if ((length & 1) != 0) {
					dst [length - 1, y] = src [numLFValues - 1, y] * InvScale; 
				}
			} else {
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];
			}
		}

		override protected void InvTransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= AllowedMinSize) {
				int half = length >> 1;
				int offDst = 0;
				// number of low-pass values
				int numLFValues = half + (length & 1);

				for (int i = 0; i < half; i++) {
					float a = src [x, i              ] * InvScale;
					float b = src [x, i + numLFValues];
					dst [x,     offDst] = a - b;
					dst [x, offDst + 1] = a + b;
					offDst += 2;
				}							
				if ((length & 1) != 0) {
					dst [x, length - 1] = src [x, numLFValues - 1] * InvScale;
				} 
			} else {
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];
			}
		}
        #pragma warning restore 1591
	}
}