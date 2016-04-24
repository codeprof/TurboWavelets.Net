// 
// Wavelet2D.cs
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

/*! \mainpage TurboWavelets.Net documentation
 * TurboWavelets.Net provides very fast, flexible and compact implementations of discrete wavelet transformations in C#.
 * Unlike others this implementation has no limitation is sizes for the transformation (lengths like 39, 739,... are possible, not just power of two numbers) 
 * At the moment only floating point numbers are supported.

 * \section Features
 * - 1D biorthogonal 5/3 wavelet using the lifting scheme (for arbitrary sizes, not just power of 2)
 * - 2D biorthogonal 5/3 wavelet using the lifting scheme (for arbitrary sizes, not just power of 2)
 * - 2D haar wavelet (for arbitrary sizes, not just power of 2)
 * - 2D cascade sorting of coefficients  (for arbitrary sizes, not just power of 2)
 * - Scale/Crop coefficients in a defined grid
 * - apply a deadzone
 * - Multithreaded and threadsafe
 * 
 * \section Licence
 * MIT License (MIT)
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace TurboWavelets
{
	/// <summary>
	/// A abstract basis class which provides common functionality
	/// for different implementations of a "2D Wavelet transformation"
	/// </summary>
	public abstract class Wavelet2D
	{
		/// <summary>
		/// Prototype of a delegate called to inform caller about progress and to provide possibility to abort
		/// </summary
		public delegate bool ProgressDelegate (float progress);
		#region protected attributes
		/// <summary>
		/// Width of the wavelet transformation
		/// </summary>
		protected int width;
		/// <summary>
		/// Height of the wavelet transformation
		/// </summary>
		protected int height;
		/// <summary>
		/// min. size for horizontal and vertical transformation
		/// </summary>
		protected int minSize;
		/// <summary>
		/// The allowed minimum value for minSize (limitation of the algorithmn implementation)
		/// </supmmary>
		protected int allowedMinSize;
		#endregion
		#region private attributes
		/// <summary>
		/// Setting whether threads should be used to accelerate execution
		/// </summary>
		private volatile bool enableParallel = true;
		/// <summary>
		/// Setting whether temporary memory should cached (or allocated if needed)
		/// </summary>
		private volatile bool enableCacheing = false;
		/// <summary>
		/// temporary buffer used to store transformation results
		/// </summary>
		private volatile float[,] cachedArray = null;
		/// <summary>
		//Synchronisaion object used to make all calls thread safe.
		//Note than using [MethodImpl(MethodImplOptions.Synchronized)] is not sufficient, as
		//the temporary and src array can be used by different methods at the same time
		/// </summary>
		private object threadSync = new object ();
		/// <summary>
		/// Delegate called to inform caller about progress and to provide possibility to abort
		/// </summary>
		private ProgressDelegate progressDelegate;
		/// <summary>
		/// Flag which indicates wheter the current task is aborted
		/// </summary>
		private volatile bool progressAbort;
		/// <summary>
		/// Synchronisaion object used for progress handling
		/// </summary>
		private object progressSync = null;
		/// <summary>
		/// The progress value of the current task
		/// </summary>
		private long progressValue;
		/// <summary>
		/// The maximal progress value of the current task
		/// </summary>
		private long progressMax;
		#endregion
		#region public constructors
		/// <summary>
		/// Initalizes a two dimensional wavelet cascade transformation.
		/// By the transformation the data is split up in a high- and a low-pass. The low-pass data
		/// is repeatedly transformed again until the horizontal or vertical length reaches "minSize".
		/// </summary>
		/// <param name="minSize">minimum size up to a transformation is applied (can be set arbitrary)</param>
		/// <param name="allowedMinSize">minimum size up to a transformation can be applied (implementation depended)</param>
		/// <param name="width">starting width of the transformation</param>
		/// <param name="height">starting height of the transformation</param>
		/// <exception cref="ArgumentException"></exception>			
		public Wavelet2D (int minSize, int allowedMinSize, int width, int height)
		{
			if (allowedMinSize < 1) {
				throw new ArgumentException ("allowedMinSize cannot be less than one");
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
		#endregion
		#region public properties
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
		#endregion
		#region private methods for progress handling
		/// <summary>
		/// Updates the progress value of the current task by the declared increment and
		/// calls a callback to notify the caller about the progress and give
		/// the possiblily to abort the current task.
		/// </summary>
		/// <param name="progressDelegate">a delegate to notify the caller about the progress and to give
		/// the possiblity to abort the current task. Can be set to null if notification is not required</param>	
		/// <param name="maxValue">The maximal progress value (to calculate progress percentage). Can be set to 0 if used with TransformIsotropic2D() or BacktransformIsotropic2D() </param>
		private void initProgress (ProgressDelegate progressDelegate, long maxValue = 0)
		{
			if (progressDelegate != null) {
				this.progressSync = new object ();
			} else {
				this.progressSync = null;
			}
			int w = width, h = height;
			this.progressMax = maxValue;
			//Calculate the exact maximal value for the progress value if not declared (used by TransformIsotropic2D() and BacktransformIsotropic2D())			
			if (this.progressMax == 0) {
				while ((w >= minSize) && (h >= minSize)) {
					this.progressMax += 2 * w * h;
					w = -(-w >> 1);
					h = -(-h >> 1);
				}
			}
			this.progressDelegate = progressDelegate;
			this.progressValue = 0;
			this.progressAbort = false;
		}

		/// <summary>
		/// Updates the progress value of the current task by the declared increment and
		/// calls a callback to notify the caller about the progress and give
		/// the possiblily to abort the current task.
		/// </summary>
		/// <param name="increment">Value by which the progress is increased</param>	
		/// </returns>True if the current task should be aborted. False otherwise.</returns>
		private bool updateProgress (long increment)
		{
			bool abort = false;
			if (progressSync != null) {
				lock (progressSync) {
					if (!progressAbort) {
						progressValue += increment;
						if (progressValue > progressMax) {
							progressValue = progressMax;
						}
						progressAbort = progressDelegate ((float)progressValue / (float)progressMax * 100.0f);
					} else {
						//Make sure delegate not called after abort
					}
					abort = progressAbort;
				}
			}
			return abort;
		}
		#endregion
		#region private helper methods
		/// <summary>
		/// Provides a temporary 2D float array with the in the constructor declared dimensions
		/// </summary>
		protected float[,] getTempArray ()
		{
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
			return tmp;
		}

		/// <summary>
		/// Helper method to check a 2D float array to have the correct dimensions and is not null
		/// </summary>
		/// <param name="src">a 2D float array</param>
		/// <param name="name">name of src in the calling method</param>
		/// <exception cref="ArgumentException"></exception>
		private void checkArrayArgument (float[,] src, string name)
		{
			if (src == null) {
				throw new ArgumentException (name + " cannot be null");
			}
			if (src.GetLength (0) < width) {
				throw new ArgumentException ("first dimension of " + name + " cannot be smaller than " + width);
			}
			if (src.GetLength (1) < height) {
				throw new ArgumentException ("second dimension of " + name + " cannot be smaller than " + height);
			}
		}

		/// <summary>
		/// Private method to modify the coefficients of a single block of the declared grid size.
		/// The coefficients in the block are sorted by their absolute values (from high to low).
		/// The first n coefficients are multiplied with the coresponding value in the "scaleFactorsMajors"-array.
		/// If "scaleFactorsMajors" is null the values remain unchanged.
		/// The remaining coefficients are mulitplied with the fixed value "scaleFactorsMinors".
		/// </summary>
		/// <param name="src">a 2D float array</param>
		/// <param name="n">n greatest coefficients multiplied by the coresponding value in the "scaleFactorsMajors"-array or remaing unchanged if "scaleFactorsMajors" is null</param>
		/// <param name="scaleFactorsMajors">float array of size n with scaling factors</param>
		/// <param name="scaleFactorsMinors">scaling factor for remaining coefficients</param>
		/// <param name="gridSize">Size of the grid (horizontally and vertically)</param>
		/// <param name="startX">start position in first dimension</param>
		/// <param name="startY">start position in second dimension</param>	
		private void ModfiyBlock (float[,] src, int n, float[] scaleFactorsMajors, float scaleFactorsMinors, int gridSize, int startX, int startY)
		{
			//Note: ModfiyBlock should not be called directly, as
			//it is not thread safe. The critical section must be started
			//in the calling method
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
						if (tmpBlock [x, y] > max) {
							max = tmpBlock [x, y];
							maxIdxX = x;
							maxIdxY = y;
						}
					}
				}
				keep [maxIdxX, maxIdxY] = true;
				//Scale all major coefficients (with greater amplitutes)
				//by the coresponding scale factor 
				if (scaleFactorsMajors != null) {
					int x = startX + maxIdxX;
					int y = startY + maxIdxY;
					//x and y can be out of bounds!
					if (x > endX - 1) {
						x = endX - 1;
					}
					if (y > endY - 1) {
						y = endY - 1;
					}
					src [x, y] *= scaleFactorsMajors [k];
				}
			}
			//all minor coefficients (with small amplitutes)
			//are multiplied by a certain factor (for denoising typically zero)
			for (int y = startY; y < endY; y++) {
				for (int x = startX; x < endX; x++) {
					if (!keep [x - startX, y - startY])
						src [x, y] *= scaleFactorsMinors;
				}
			}
		}

		/// <exception cref="ArgumentException"></exception>
		private void ModifyCoefficients (float[,] src, int n, float[] scaleFactorsMajors, float scaleFactorsMinors, int gridSize, ProgressDelegate progressDelegate)
		{
			//Note: ModifyCoefficients should not be called directly, as
			//it is not thread safe. The critical section must be started
			//in the calling method
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
				throw new ArgumentException ("n (" + n + ") cannot be greater than " + gridSize + "*" + gridSize);
			}
			int w = width / gridSize;
			if ((width % gridSize) != 0) {
				w++;
			}
			int h = height / gridSize;
			if ((height % gridSize) != 0) {
				h++;
			}
			int numBlocks = w * h;
			initProgress (progressDelegate, numBlocks);

			if (this.enableParallel) {
				Parallel.For (0, numBlocks, (block, loopState) => 
				{
					int startX = (block % w) * gridSize;
					int startY = (block / w) * gridSize;
					ModfiyBlock (src, n, scaleFactorsMajors, scaleFactorsMinors, gridSize, startX, startY);
					if (updateProgress (1)) {
						loopState.Stop ();
					}
				});
			} else {
				for (int block = 0; block < numBlocks; block++) {
					int startX = (block % w) * gridSize;
					int startY = (block / w) * gridSize;
					ModfiyBlock (src, n, scaleFactorsMajors, scaleFactorsMinors, gridSize, startX, startY);
					if (updateProgress (1)) {
						break;
					}
				}
			}
		}

		private void getBlockCoefficientsRange (float[,] src, int offsetX, int offsetY, int width, int height, out float min, out float max, bool enableParallel, bool enableProgress, ProgressDelegate progressDelegate)
		{
			float minVal = float.MaxValue;
			float maxVal = float.MinValue;
			if (enableParallel) {
				object sync = new object ();
				Parallel.For (0, height, (y, loopState) => 
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
					if (enableProgress) {
						if (updateProgress (1)) {
							loopState.Stop ();
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
					if (enableProgress) {
						if (updateProgress (1)) {
							break;
						}
					}
				}
			}
			min = minVal;
			max = maxVal;
		}
		#endregion
		#region virtual methods which get overwritten by derived class
		/// <summary>
		/// Performs a single horizontal transformation (transformation of a single row)
		/// </summary>
		/// <param name="src">2d float array on which should be used as source for the transformation</param>
		/// <param name="dst">2d float array on which should be used as destination for the transformation</param>
		/// <param name="y">index of the row which should be transformed</param>
		/// <param name="length">number of entries to transform</param>
		virtual protected void TransformRow (float[,] src, float[,] dst, int y, int length)
		{
			//will be overwritten by method of derived class...
		}

		/// <summary>
		/// Performs a single vertical transformation (transformation of a single column)
		/// </summary>
		/// <param name="src">2d float array on which should be used as source for the transformation</param>
		/// <param name="dst">2d float array on which should be used as destination for the transformation</param>
		/// <param name="x">index of the row which should be transformed</param>
		/// <param name="length">number of entries to transform</param>
		virtual protected void TransformCol (float[,] src, float[,] dst, int x, int length)
		{
			//will be overwritten by method of derived class...
		}

		/// <summary>
		/// Performs a single inverse horizontal transformation (transformation of a single row)
		/// </summary>
		/// <param name="src">2d float array on which should be used as source for the transformation</param>
		/// <param name="dst">2d float array on which should be used as destination for the transformation</param>
		/// <param name="y">index of the row which should be transformed</param>
		/// <param name="length">number of entries to transform</param>
		virtual protected void InvTransformRow (float[,] src, float[,] dst, int y, int length)
		{
			//will be overwritten by method of derived class...
		}

		/// <summary>
		/// Performs a single inverse vertical transformation (transformation of a single column)
		/// </summary>
		/// <param name="src">2d float array on which should be used as source for the transformation</param>
		/// <param name="dst">2d float array on which should be used as destination for the transformation</param>
		/// <param name="x">index of the row which should be transformed</param>
		/// <param name="length">number of entries to transform</param>
		virtual protected void InvTransformCol (float[,] src, float[,] dst, int x, int length)
		{
			//will be overwritten by method of derived class...
		}
		#endregion
		virtual protected void TransformRows (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, h, (y, loopState) => 
				{
					if (updateProgress (w)) {
						loopState.Stop ();
					}
					TransformRow (src, dst, y, w);
				});
			} else {
				for (int y = 0; y < h; y++) {
					if (updateProgress (w)) {
						break;
					}
					TransformRow (src, dst, y, w);
				}
			}
		}

		virtual protected void TransformCols (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, w, (x, loopState) => 
				{
					if (updateProgress (h)) {
						loopState.Stop ();
					}
					TransformCol (src, dst, x, h);
				});
			} else {
				for (int x = 0; x < w; x++) {
					if (updateProgress (h)) {
						break;
					}
					TransformCol (src, dst, x, h);
				}
			}
		}

		virtual protected void InvTransformRows (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, h, (y, loopState) => 
				{
					if (updateProgress (w)) {
						loopState.Stop ();
					}
					InvTransformRow (src, dst, y, w);
				});
			} else {
				for (int y = 0; y < h; y++) {
					if (updateProgress (w)) {
						break;
					}
					InvTransformRow (src, dst, y, w);
				}
			}
		}

		virtual protected void InvTransformCols (float[,] src, float[,] dst, int w, int h)
		{
			if (enableParallel) {
				Parallel.For (0, w, (x, loopState) => 
				{
					if (updateProgress (h)) {
						loopState.Stop ();
					}
					InvTransformCol (src, dst, x, h);
				});
			} else {
				for (int x = 0; x < w; x++) {
					if (updateProgress (h)) {
						break;
					}
					InvTransformCol (src, dst, x, h);
				}
			}
		}

		/// <summary>
		/// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
		/// <param name="src">two dimensional float array to perform the the wavelet transformation on</param>	
		/// <exception cref="ArgumentException"></exception>
		virtual public void TransformIsotropic2D (float[,] src, ProgressDelegate progressDelegate = null)
		{
			lock (threadSync) {
				checkArrayArgument (src, "src");
				float[,] tmp = getTempArray ();
				int w = width, h = height;
				initProgress (progressDelegate);
				while ((w >= minSize) && (h >= minSize) && (!updateProgress(0))) {
					TransformRows (src, tmp, w, h);
					TransformCols (tmp, src, w, h);
					// shift always rounds down (towards negative infinity)
					//However, for odd lengths we have one low-pass value more than
					//high-pass values. By shifting the negative value and negating the result
					//we get the desired result.
					w = -(-w >> 1);
					h = -(-h >> 1);
				}
			}
		}

		/// <summary>
		/// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		virtual public void BacktransformIsotropic2D (float[,] src, ProgressDelegate progressDelegate = null)
		{
			lock (threadSync) {
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
				float[,] tmp = getTempArray ();
				int i = 1;
				initProgress (progressDelegate);
				while ((i <= log2) && (!updateProgress(0))) {
					//Shift always rounds down (towards negative infinity)
					//However, for odd lengths we have one more low-pass value than
					//high-pass values. By shifting the negative value and negating the result
					//we get the desired result.
					int w = -(-width >> (log2 - i));
					int h = -(-height >> (log2 - i));

					if ((w >= minSize) && (h >= minSize)) {
						InvTransformCols (src, tmp, w, h);
						InvTransformRows (tmp, src, w, h);
					}
					i++;
				}
			}
		}

		/// <summary>
		/// Scales the n (length of the scaleFactors array) greatest coefficinets (for a defined grid size) by the value declared in scaleFactors.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		virtual public void ScaleCoefficients (float[,] src, float[] scaleFactors, int gridSize, ProgressDelegate progressDelegate = null)
		{		
			lock (threadSync) {
				if (scaleFactors == null) {
					throw new ArgumentException ("scaleFactors cannot be null");
				}
				if (scaleFactors.Length > gridSize * gridSize) {
					throw new ArgumentException ("scaleFactors lenght cannot be greater than " + gridSize * gridSize);
				}
				ModifyCoefficients (src, scaleFactors.Length, scaleFactors, 1.0f, gridSize, progressDelegate);
			}
		}

		/// <summary>
		/// Set all but the greatest n coefficient to zero in the defined grid size
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		virtual public void CropCoefficients (float[,] src, int n, int gridSize, ProgressDelegate progressDelegate = null)
		{
			lock (threadSync) {
				ModifyCoefficients (src, n, null, 0.0f, gridSize, progressDelegate);
			}
		}

		/// <summary>
		/// Set all coefficient with an absolute value smaller then "minAbsoluteValue" to zero (deadzone)
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		virtual public void CropCoefficients (float[,] src, float minAbsoluteValue, ProgressDelegate progressDelegate = null)
		{
			lock (threadSync) {
				checkArrayArgument (src, "src");
				initProgress (progressDelegate, height);
				if (enableParallel) {
					Parallel.For (0, height, (y, loopState) => 
					{
						for (int x = 0; x < width; x++) {
							float val = src [x, y];
							if ((val < minAbsoluteValue) && (-val < minAbsoluteValue)) { //Same as Math.Abs(val) < minAbsoluteValue
								src [x, y] = 0.0f;
							}
						}
						if (updateProgress (1)) {
							loopState.Stop ();
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
						if (updateProgress (1)) {
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// get the minimum and maximum amplitude (absolute values) of all coefficient values 
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		virtual public void getCoefficientsRange (float[,] src, out float min, out float max, ProgressDelegate progressDelegate = null)
		{
			lock (threadSync) {
				checkArrayArgument (src, "src");
				initProgress (progressDelegate, height);
				getBlockCoefficientsRange (src, 0, 0, width, height, out min, out max, enableParallel, true, progressDelegate);
			}
		}

/*
		virtual public float getQuantil(float[,] src, float p)
		{
			int numCoeffs = p * width * height;
			int gridSize = (int)(Math.Sqrt(numCoeffs) + 1);

			int w = width / gridSize;
			if ((width % gridSize) != 0) {
				w++;
			}
			int h = height / gridSize;
			if ((height % gridSize) != 0) {
				h++;
			}

			float[,] minValues = new float[w, h];
			float[,] maxValues = new float[w, h];
			int numBlocks = w * h;
			initProgress (progressDelegate, numBlocks);

			if (this.enableParallel) {
				Parallel.For (0, numBlocks, (block, loopState) => 
				{
					int startX = (block % w) * gridSize;
					int startY = (block / w) * gridSize;
					int endX = startX + gridSize;
					int endY = startY + gridSize;

					getBlockCoefficientsRange (src, startX, startY, endX - startX, endY - startY, minValues[], max, enableParallel, progressDelegate);

					ModfiyBlock (src, n, scaleFactorsMajors, scaleFactorsMinors, gridSize, startX, startY);
					if (updateProgress (1)) {
						loopState.Stop ();
					}
				});
			} else {
				for (int block = 0; block < numBlocks; block++) {
					int startX = (block % w) * gridSize;
					int startY = (block / w) * gridSize;
					ModfiyBlock (src, n, scaleFactorsMajors, scaleFactorsMinors, gridSize, startX, startY);
					if (updateProgress (1)) {
						break;
					}
				}
			}
		}
*/
	}
}
