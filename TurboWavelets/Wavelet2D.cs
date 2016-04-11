// 
// Wavelet2D.cs
//  
// Author:
//       Stefan Moebius
// Date:
//       2016-04-11
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
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TurboWavelets
{
	public abstract class Wavelet2D
	{
		#region protected attributes
		protected int width;
		protected int height;
		protected int minSize;
		protected int allowedMinSize;
		protected bool enableParallel = true;
		protected bool enableCacheing = false;
		protected float[,] cachedArray = null;
		//Used to make all calls thread safe.
		//Note than using [MethodImpl(MethodImplOptions.Synchronized)] is not sufficient, as
		//the temporary and src array can be used by different methods at the same time
		protected object threadSync = new object ();
		#endregion
		/// <summary>
		/// Initalizes a two dimensional wavelet cascade transformation
		/// </summary>
		/// <param name="minSize">minimum size up to a transformation is applied (can be set arbitary)</param>
		/// <param name="allowedMinSize">minimum size up to a transformation can be applied (implementation depended)</param>
		/// <param name="width">starting width of the transformation</param>
		/// <param name="height">starting height of the transformation</param>				
		public Wavelet2D (int minSize, int allowedMinSize, int width, int height)
		{
			if (allowedMinSize < 0) {
				throw new ArgumentException ("allowedMinSize cannot be less than zero");
			}
			if (minSize < allowedMinSize) {
				throw new ArgumentException ("minSize cannot be smaller than " + allowedMinSize);
			}
			if (width < minSize || height < minSize) {
				throw new ArgumentException ("width and height must be greater or equal to " + minSize);
			}
			this.width = width;
			this.height = height;
			this.minSize = minSize;
			this.allowedMinSize = allowedMinSize;
		}

		/// <summary>
		/// returns the width for the two dimensional wavelet transformation
		/// </summary>
		public int Width {
			get {
				return width;
			}
		}

		/// <summary>
		/// returns the height for the two dimensional wavelet transformation
		/// </summary>
		public int Height {
			get {
				return height;
			}
		}

		/// <summary>
		/// enables or disables caching of memory (disabled by default)
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public bool EnableCaching {
			get {
				return enableCacheing;
			}
			set {
				enableCacheing = value;
				if (!value) {
					FlushCache ();
				}
			}
		}

		/// <summary>
		/// enables or disables parallel execution (enabled by default)
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public bool EnableParallel {
			get {
				return enableParallel;
			}
			set {
				enableParallel = value;
			}
		}

		/// <summary>
		/// Frees all cached memory
		/// </summary>
		public void FlushCache ()
		{
			lock (threadSync) {
				cachedArray = null;
			}
		}

		virtual protected void TransformRow (float[,] src, float[,] dst, int y, int length)
		{
			//will be overwritten by method of child class...
		}

		virtual protected void TransformCol (float[,] src, float[,] dst, int x, int length)
		{
			//will be overwritten by method of child class...
		}

		virtual protected void InvTransformRow (float[,] src, float[,] dst, int y, int length)
		{
			//will be overwritten by method of child class...
		}

		virtual protected void InvTransformCol (float[,] src, float[,] dst, int x, int length)
		{
			//will be overwritten by method of child class...
		}

		virtual protected void TransformRows (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, h, y => 
				{
					TransformRow (src, dst, y, w);
				});
			} else {
				for (int y = 0; y < h; y++) {
					TransformRow (src, dst, y, w);
				}
			}
		}

		virtual protected void TransformCols (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, w, x => 
				{
					TransformCol (src, dst, x, h);
				});
			} else {
				for (int x = 0; x < w; x++) {
					TransformCol (src, dst, x, h);
				}
			}
		}

		virtual protected void InvTransformRows (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, h, y => 
				{
					InvTransformRow (src, dst, y, w);
				});
			} else {
				for (int y = 0; y < h; y++) {
					InvTransformRow (src, dst, y, w);
				}
			}
		}

		virtual protected void InvTransformCols (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, w, x => 
				{
					InvTransformCol (src, dst, x, h);
				});
			} else {
				for (int x = 0; x < w; x++) {
					InvTransformCol (src, dst, x, h);
				}
			}
		}

		private void checkArrayArgument (float[,] src, string name)
		{
			if (src == null) {
				throw new ArgumentException (name + " cannot be null");
			}
			if (src.GetLength (0) < width) {
				throw new ArgumentException ("first dimension of src cannot be smaller than " + width);
			}
			if (src.GetLength (1) < height) {
				throw new ArgumentException ("second dimension of src cannot be smaller than " + height);
			}
		}

		private float[,] getTempArray ()
		{
			lock (threadSync) {
				float[,] tmp = cachedArray;
				if (tmp == null) {
					//Note: if we do transform the cols and rows in sequentally (not in parallel) we
					//do not need an temporary array of the same size as the source array.
					//Insead a one dimensional array (with the maximum of width and height as length
					//would be sufficient. However we do not use this fact here, as
					//different implementations of TransformCol(), TransformRow()... would be required
					tmp = new float[width, height];
					if (enableCacheing) {
						cachedArray = tmp;
					}
				}
			}
			return tmp;
		}

		/// <summary>
		/// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
		/// <param name="src">two dimensional float array to perform the the wavelet transformation on</param>	
		virtual public void TransformIsotropic2D (float[,] src)
		{
			checkArrayArgument (src, "src");
			lock (threadSync) {
				float[,] tmp = getTempArray ();
				int w = width, h = height;
				while ((w > minSize) && (h > minSize)) {
					TransformRows (src, tmp, w, h);
					TransformCols (tmp, src, w, h);
					w >>= 1;
					h >>= 1;
				}
			}
		}

		/// <summary>
		/// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
		virtual public void BacktransformIsotropic2D (float[,] src)
		{
			checkArrayArgument (src, "src");
			//Calculate the integral digits of log to the base two of the maximum of "width" and "height" 
			//The resulting number of "width | height" cannot have a greater log to the base 2 (integral digits)
			//than the greater of both values.
			int log2 = 1;
			int test = 1;
			while (test < (width | height)) {
				test <<= 1;
				log2++;
			}
			lock (threadSync) {
				float[,] tmp = getTempArray ();
				int i = 1;
				while (i <= log2) {
					int w = width >> (log2 - i);
					int h = height >> (log2 - i);
					if ((w > minSize) && (h > minSize)) {
						InvTransformCols (src, tmp, w, h);
						InvTransformRows (tmp, src, w, h);
					}
					i++;
				}
			}
		}

		virtual protected void ModifyCoefficients (float[,] src, int n, float[] scaleFactorsMajors, float scaleFactorsMinors, int gridSize)
		{
			checkArrayArgument (src, "src");
			if (scaleFactorsMajors != null) {
				if (scaleFactorsMajors.Length != n) {
					throw new ArgumentException ("scaleFactorsMajors must be null or the length must be of dimension n (" + n + ")");
				}
			}
			if (gridSize < 1) {
				throw new ArgumentException ("gridSize (" + gridSize + ") cannot be smaller than 1");
			}
			if (n < 0) {
				throw new ArgumentException ("n (" + n + ") cannot be negative");
			}
			if (n > gridSize * gridSize) {
				throw new ArgumentException ("n" + n + " cannot be greater than " + gridSize + "*" + gridSize);
			}
			int w = width / gridSize;
			if ((w % gridSize) != 0) {
				w++;
			}
			int h = width / gridSize;
			if ((h % gridSize) != 0) {
				h++;
			}
			int numBlocks = w * h;

			lock (threadSync) {
				Parallel.For (0, numBlocks, block => 
				{
					int startX = (block % w) * gridSize;
					int startY = (block / w) * gridSize;

					int endX = startX + gridSize;
					int endY = startY + gridSize;
					if (endX > width) {
						endX = width;
					}
					if (endY > height) {
						endY = height;
					}
					bool[,] keep = new bool[gridSize, gridSize];
					float[,] tmpBlock = new float[gridSize, gridSize];

					for (int y = startY; y < endY; y++) {
						for (int x = startX; x < endX; x++) {
							float val = src [x, y];
							if (val < 0) {
								val = -val;
							}
							tmpBlock [x - startX, y - startY] = val;
						}
					}
					for (int k = 0; k < n; k++) {
						float max = -1.0f;
						int maxIdxX = -1, maxIdxY = -1;
						for (int y = 0; y <  gridSize; y++) {
							for (int x = 0; x <  gridSize; x++) {
								if (!keep [x, y])
								if (tmpBlock [x, y] >= max) {
									max = tmpBlock [x, y];
									maxIdxX = x;
									maxIdxY = y;
								}
							}
						}
						keep [maxIdxX, maxIdxY] = true;
						//Scale all major coefficients (with greater amplitutes)
						//by the coresponding scale factor 
						if (scaleFactorsMajors != null)
							src [startX + maxIdxX, startY + maxIdxY] *= scaleFactorsMajors [k];
					}
					//all minor coefficients (with small amplitutes)
					//are multiplied by a certain factor (for denoising typically zero)
					for (int y = startY; y < endY; y++) {
						for (int x = startX; x < endX; x++) {
							if (!keep [x - startX, y - startY])
								src [x, y] *= scaleFactorsMinors;
						}
					}
				});
			}
		}

		/// <summary>
		/// Set all but the greatest n coefficient to zero in the defined grid size
		/// </summary>
		virtual public void CropCoefficients (float[,] src, int n, int gridSize)
		{
			ModifyCoefficients (src, n, null, 0.0f, gridSize);
		}

		/// <summary>
		/// Set all coefficient with an absolute value smaller then "minAbsoluteValue" to zero (deadzone)
		/// </summary>
		virtual public void CropCoefficients (float[,] src, float minAbsoluteValue)
		{
			checkArrayArgument (src, "src");
			lock (threadSync) {
				if (enableParallel) {
					Parallel.For (0, height, y => 
					{
						for (int x = 0; x < width; x++) {
							float val = src [x, y];
							if ((val < minAbsoluteValue) && (-val < minAbsoluteValue)) { //Same as Math.Abs(val) < minAbsoluteValue
								src [x, y] = 0.0f;
							}
						}
					});
				} else {
					for (int y = 0; y < height; y++) {
						for (int x = 0; x < width; x++) {
							float val = src [x, y];
							if ((val < minAbsoluteValue) && (-val < minAbsoluteValue)) { //Same as Math.Abs(val) < minAbsoluteValue
								src [x, y] = 0.0f;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// get the minimum and maximum amplitude (absolute values) of all coefficient values 
		/// </summary>
		virtual public void getCoefficientsRange (float[,] src, ref float min, ref float max)
		{
			checkArrayArgument (src, "src");
			float minVal = float.MaxValue;
			float maxVal = float.MinValue;
			lock (threadSync) {
				if (enableParallel) {
					object sync = new object ();
					Parallel.For (0, height, y => 
					{
						float thrdMinVal = float.MaxValue;
						float thrdMaxVal = float.MinValue;
						for (int x = 0; x < width; x++) {
							float val = src [x, y];
							if (val < 0) {
								val = -val;
							}
							if (val < thrdMinVal) {
								thrdMinVal = val;
							} else if (val > thrdMaxVal) {
								thrdMaxVal = val;
							}
						}
						lock (sync) {
							if (thrdMinVal < minVal) {
								minVal = thrdMinVal;
							}
							if (thrdMaxVal > maxVal) {
								maxVal = thrdMaxVal;
							}
						}
					});
				} else {
					for (int y = 0; y < height; y++) {
						for (int x = 0; x < width; x++) {
							float val = src [x, y];
							if (val < 0) {
								val = -val;
							}
							if (val < minVal) {
								minVal = val;
							} else if (val > maxVal) {
								maxVal = val;
							}
						}
					}
				}
				if (min != null) {
					min = minVal;
				}
				if (max != null) {
					max = maxVal;
				}
			}
		}

		/// <summary>
		/// Scales the n (length of the scaleFactors array) greatest coefficinets (for a defined grid size) by the value declared in scaleFactors.
		/// </summary>
		virtual public void ScaleCoefficients (float[,] src, float[] scaleFactors, int gridSize)
		{		
			if (scaleFactors == null) {
				throw new ArgumentException ("scaleFactors cannot be null");
			}
			if (scaleFactors.Length > gridSize * gridSize) {
				throw new ArgumentException ("scaleFactors lenght cannot be greater than " + gridSize * gridSize);
			}
			ModifyCoefficients (src, scaleFactors.Length, scaleFactors, 1.0f, gridSize);
		}
	}
}
