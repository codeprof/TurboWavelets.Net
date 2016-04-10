// 
// Biorthogonal53Wavelet2D.cs
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
using System.Threading.Tasks;

namespace TurboWavelets
{
	public class Biorthogonal53Wavelet2D : Wavelet2D
	{
		protected const int   MINSIZE    = 3;
		protected const float SCALE      = 2.0f;
		protected const float SCALE_INV  = 0.5f;
		protected const float MEAN       = 0.5f;
		protected const float MEAN_INV   = 2.0f;
		protected const float SMOOTH     = 0.25f;
		protected const float SMOOTH_INV = 4.0f;
		/// <summary>
		/// A fast implementation of a two-dimensional biorthogonal 5/3 wavelet transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// using the lifting scheme. The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
		public Biorthogonal53Wavelet2D (int width, int height)
            : base(MINSIZE, MINSIZE, width, height)
		{   
		}

		/// <summary>
		/// Initalizes a two dimensional wavelet transformation
		/// </summary>
		public Biorthogonal53Wavelet2D (int width, int height, int minSize)
            : base(minSize, MINSIZE, width, height)
		{
		}

		override protected void TransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= MINSIZE) {
				int half = length >> 1;
				//if the length is even then subtract 1 from "half"
				//as there is the same number of low and high frequency values
				//(Note that "num_lf_values" is equal to "half+1") 
				//For a odd length there is one additional low frequency value (so do not subtract 1)
				//"half" is one less than "num_lf_values" as we cannot directly
				//calculate the last value in the for-loop (array bounds)
				if ((length & 1) == 0)
					half--;
				int offsrc = 0;
				// starting offset for high frequency values (= number of low frequency values)
				int offdst = half + 1; 
				int num_lf_values = offdst;
	
				float last_hf = 0.0f;
				for (int i = 0; i < half; i++) {
					//calculate the high frequency value by
					//subtracting the mean of the immediate neighbors for every second value
					float hf = src [offsrc + 1, y] - (src [offsrc, y] + src [offsrc + 2, y]) * MEAN;
					//smoothing the low frequency value, scale by factor 2 
					//(instead of scaling low frequencies by factor sqrt(2) and
					//shrinking high frequencies by factor sqrt(2)
					//and reposition to have all low frequencies on the left side
					dst [i, y] = SCALE * (src [offsrc, y] + (last_hf + hf) * SMOOTH);
					dst [offdst++, y] = hf;
					last_hf = hf;
					offsrc += 2; 
				} 
				if ((length & 1) == 0) {
					//the secound last value in the array is our last low frequency value
					dst [num_lf_values - 1, y] = SCALE * src [length - 2, y]; 
					//the last value is a high frequency value
					//however here we just subtract the previos value (so not really a
					//biorthogonal 5/3 transformation)
					//This is done because the 5/3 wavelet cannot be calculated at the boundary
					dst [length - 1, y] = src [length - 1, y] - src [length - 2, y];
				} else {
					dst [num_lf_values - 1, y] = SCALE * src [length - 1, y]; 
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];
			}
			
		}

		override protected void TransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= MINSIZE) {
				int half = length >> 1;
				//if the length is even then subtract 1 from "half"
				//as there is the same number of low and high frequency values
				//(Note that "num_lf_values" is equal to "half+1") 
				//For a odd length there is one additional low frequency value (so do not subtract 1)
				//"half" is one less than "num_lf_values" as we cannot directly
				//calculate the last value in the for-loop (array bounds)
				if ((length & 1) == 0)
					half--;
				int offsrc = 0;
				// starting offset for high frequency values (= number of low frequency values)
				int offdst = half + 1; 
				int num_lf_values = offdst;
	
				float last_hf = 0.0f;
				for (int i = 0; i < half; i++) {
					//calculate the high frequency value by
					//subtracting the mean of the immediate neighbors for every second value
					float hf = src [x, offsrc + 1] - (src [x, offsrc] + src [x, offsrc + 2]) * MEAN;
					//smoothing the low frequency value, scale by factor 2 
					//(instead of scaling low frequencies by factor sqrt(2) and
					//shrinking high frequencies by factor sqrt(2)
					//and reposition to have all low frequencies on the left side
					dst [x, i] = SCALE * (src [x, offsrc] + (last_hf + hf) * SMOOTH);
					dst [x, offdst++] = hf;
					last_hf = hf;
					offsrc += 2; 
				} 
				if ((length & 1) == 0) {
					//the secound last value in the array is our last low frequency value
					dst [x, num_lf_values - 1] = src [x, length - 2] * SCALE; 
					//the last value is a high frequency value
					//however here we just subtract the previos value (so not really a
					//biorthogonal 5/3 transformation)
					//This is done because the 5/3 wavelet cannot be calculated at the boundary
					dst [x, length - 1] = src [x, length - 1] - src [x, length - 2];
				} else {
					dst [x, num_lf_values - 1] = src [x, length - 1] * SCALE; 
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];
			}
		}

		override protected void InvTransformRow (float[,] src, float[,] dst, int y, int length)
		{
			if (length >= MINSIZE) {
				int half = length >> 1;
				//if the length is even then subtract 1 from "half"
				//as there is the same number of low and high frequency values
				//(Note that "num_lf_values" is equal to "half+1") 
				//For a odd length there is one additional low frequency value (so do not subtract 1)
				//"half" is one less than "num_lf_values" as we cannot directly
				//calculate the last value in the for-loop (array bounds)
				if ((length & 1) == 0)
					half--;
				// number of low frequency values
				int num_lf_values = half + 1;
	
				float last_lf = SCALE_INV * src [0, y] - src [num_lf_values, y] * SMOOTH;
				float last_hf = src [num_lf_values, y];
				//Calculate the first two values outside the for loop (array bounds)
				dst [0, y] = last_lf;
				dst [1, y] = last_hf + last_lf * SCALE_INV;
				for (int i = 1; i < half; i++) {
					float hf = src [num_lf_values + i, y];
					float lf = SCALE_INV * src [i, y];
					//reconstruct the original value by removing the "smoothing" 
					float lf_reconst = lf - (hf + last_hf) * SMOOTH;
					dst [2 * i, y] = lf_reconst;
					//add reconstructed low frequency value (left side) and high frequency value
					dst [2 * i + 1, y] = lf_reconst * MEAN + hf;
					//add other low frequency value (right side)
					//This must be done one iteration later, as the
					//reconstructed values is not known earlier
					dst [2 * i - 1, y] += lf_reconst * MEAN;
					last_hf = hf;
					last_lf = lf_reconst;
				}
	
				if ((length & 1) == 0) {
					//restore the last 3 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [length - 3, y] += src [num_lf_values - 1, y] * MEAN * SCALE_INV;
					//copy the last low frequency value
					dst [length - 2, y] = src [num_lf_values - 1, y] * SCALE_INV;
					//restore the last value by adding last low frequency value
					dst [length - 1, y] = src [length - 1, y] + src [num_lf_values - 1, y] * SCALE_INV; 
				} else {
					//restore the last 2 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [length - 2, y] += src [num_lf_values - 1, y] * MEAN * SCALE_INV;
					//copy the last low frequency value
					dst [length - 1, y] = src [num_lf_values - 1, y] * SCALE_INV;
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [i, y] = src [i, y];				
			}
		}

		override protected void InvTransformCol (float[,] src, float[,] dst, int x, int length)
		{
			if (length >= MINSIZE) {
				int half = length >> 1;
				//if the length is even then subtract 1 from "half"
				//as there is the same number of low and high frequency values
				//(Note that "num_lf_values" is equal to "half+1") 
				//For a odd length there is one additional low frequency value (so do not subtract 1)
				//"half" is one less than "num_lf_values" as we cannot directly
				//calculate the last value in the for-loop (array bounds)
				if ((length & 1) == 0)
					half--;
				// number of low frequency values
				int num_lf_values = half + 1;
	
				float last_lf = SCALE_INV * src [x, 0] - src [x, num_lf_values] * SMOOTH;
				float last_hf = src [x, num_lf_values];
				//Calculate the first two values outside the for loop (array bounds)
				dst [x, 0] = last_lf;
				dst [x, 1] = last_hf + last_lf * SCALE_INV;
				for (int i = 1; i < half; i++) {
					float hf = src [x, num_lf_values + i];
					float lf = SCALE_INV * src [x, i];
					//reconstruct the original value by removing the "smoothing" 
					float lf_reconst = lf - (hf + last_hf) * SMOOTH;
					dst [x, 2 * i] = lf_reconst;
					//add reconstructed low frequency value (left side) and high frequency value
					dst [x, 2 * i + 1] = lf_reconst * MEAN + hf;
					//add other low frequency value (right side)
					//This must be done one iteration later, as the
					//reconstructed values is not known earlier
					dst [x, 2 * i - 1] += lf_reconst * MEAN;
					last_hf = hf;
					last_lf = lf_reconst;
				}
	
				if ((length & 1) == 0) {
					//restore the last 3 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [x, length - 3] += src [x, num_lf_values - 1] * MEAN * SCALE_INV;
					//copy the last low frequency value
					dst [x, length - 2] = src [x, num_lf_values - 1] * SCALE_INV;
					//restore the last value by adding last low frequency value
					dst [x, length - 1] = src [x, length - 1] + src [x, num_lf_values - 1] * SCALE_INV; 
				} else {
					//restore the last 2 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [x, length - 2] += src [x, num_lf_values - 1] * MEAN * SCALE_INV;
					//copy the last low frequency value
					dst [x, length - 1] = src [x, num_lf_values - 1] * SCALE_INV;
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [x, i] = src [x, i];				
			}
		}
	}
}
