// 
// Biorthogonal53Wavelet1DStatic.cs
//  
// Author:
//       Stefan Moebius
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
	public class Biorthogonal53Wavelet1DStatic
	{
		/// <summary>
		/// A fast implementation of a 1 dimensional biorthogonal 5/3 wavelet transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// using the lifting scheme.
		/// </summary>
		/// <param name="src">The source values which should be transformed</param>
		/// <param name="dst">The resulting values after the transformation</param>
		/// <returns>None</returns>
		public static void wavelet53_1d (float[] src, float[] dst, int length)
		{
			if (length >= 3) {
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
					float hf = src [offsrc + 1] - (src [offsrc] + src [offsrc + 2]) * 0.5f;
					//smoothing the low frequency value, scale by factor 2 
					//(instead of scaling low frequencies by factor sqrt(2) and
					//shrinking high frequencies by factor sqrt(2)
					//and reposition to have all low frequencies on the left side
					dst [i] = 2.0f * (src [offsrc] + (last_hf + hf) * 0.25f);
					dst [offdst++] = hf;
					last_hf = hf;
					offsrc += 2; 
				} 
				if ((length & 1) == 0) {
					//the secound last value in the array is our last low frequency value
					dst [num_lf_values - 1] = src [length - 2]; 
					//the last value is a high frequency value
					//however here we just subtract the previos value (so not really a
					//biorthogonal 5/3 transformation)
					//This is done because the 5/3 wavelet cannot be calculated at the boundary
					dst [length - 1] = src [length - 1] - src [length - 2];
				} else {
					dst [num_lf_values - 1] = src [length - 1]; 
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [i] = src [i];
			}
		}

		/// <summary>
		/// A fast implementation of a 1 dimensional biorthogonal 5/3 wavelet back-transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// using the lifting scheme.
		/// </summary>
		/// <param name="src">The source values which should be back-transformed</param>
		/// <param name="dst">The resulting values after the back-transformation</param>
		/// <returns>None</returns>
		public static void wavelet53_1d_inverse (float[] src, float[] dst, int length)
		{
			if (length >= 3) {
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

				float last_lf = 0.5f * src [0] - src [num_lf_values] * 0.25f;
				float last_hf = src [num_lf_values];
				//Calculate the first two values outside the for loop (array bounds)
				dst [0] = last_lf;
				dst [1] = last_hf + last_lf * 0.5f;
				for (int i = 1; i < half; i++) {
					float hf = src [num_lf_values + i];
					float lf = 0.5f * src [i];
					//reconstruct the original value by removing the "smoothing" 
					float lf_reconst = lf - (hf + last_hf) * 0.25f;
					dst [2 * i] = lf_reconst;
					//add reconstructed low frequency value (left side) and high frequency value
					dst [2 * i + 1] = lf_reconst * 0.5f + hf;
					//add other low frequency value (right side)
					//This must be done one iteration later, as the
					//reconstructed values is not known earlier
					dst [2 * i - 1] += lf_reconst * 0.5f;
					last_hf = hf;
					last_lf = lf_reconst;
				}

				if ((length & 1) == 0) {
					//restore the last 3 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [length - 3] += src [num_lf_values - 1] * 0.5f;
					//copy the last low frequency value
					dst [length - 2] = src [num_lf_values - 1];
					//restore the last value by adding last low frequency value
					dst [length - 1] = src [length - 1] + src [num_lf_values - 1]; 
				} else {
					//restore the last 2 values outside the for loop
					//adding the missing low frequency value (right side)
					dst [length - 2] += src [num_lf_values - 1] * 0.5f;
					//copy the last low frequency value
					dst [length - 1] = src [num_lf_values - 1];
				}
			} else {
				//We cannot perform the biorthogonal 5/3 wavelet transformation
				//for lengths smaller than 3. We could do a simpler transformation...
				//Here however, we just copy the values from the source to the destination array  
				for (int i = 0; i < length; i++)
					dst [i] = src [i];				
			}
		}
	}
}

