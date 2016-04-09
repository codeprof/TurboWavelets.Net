using System;
using System.Threading.Tasks;

namespace TurboWavelets
{
    public abstract class Wavelet2D
    {
		protected int width;
		protected int height;
		protected int minSize;
		protected int allowedMinSize;

        /// <summary>
        /// Initalizes a two dimensional wavelet transformation
        /// </summary>
        public Wavelet2D(int minSize, int allowedMinSize, int width, int height)
        {
            if (minSize < allowedMinSize)
            {
                throw new ArgumentException("minSize cannot be smaller than " + allowedMinSize);
            }

            if (width < minSize || height < minSize)
            {
                throw new ArgumentException("width and height must be greater or equal to " + minSize);
            }
			this.width = width;
			this.height = height;
			this.minSize = minSize;
			this.allowedMinSize = allowedMinSize;
        }

        /// <summary>
        /// returns the width for the two dimensional wavelet transformation
        /// </summary>
        public int Width
        {
            get
            {
                return width;
            }
        }

        /// <summary>
        /// returns the height for the two dimensional wavelet transformation
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }

        virtual protected void TransformRows(float[,] src, float[,] dst, int w, int h)
        {
            //will be overwritten by method of child class...
        }

        virtual protected void TransformCols(float[,] src, float[,] dst, int w, int h)
        {
            //will be overwritten by method of child class...
        }

        virtual protected void InvTransformRows(float[,] src, float[,] dst, int w, int h)
        {
            //will be overwritten by method of child class...
        }

        virtual protected void InvTransformCols(float[,] src, float[,] dst, int w, int h)
        {
            //will be overwritten by method of child class...
        }

		/// <summary>
        /// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
        virtual public void TransformIsotropic2D (float[,] src)
		{
			float [,] tmp = new float[width, height];
			int w = width, h = height;
			while ((w > minSize) && (h > minSize)) {
				//Console.WriteLine (width.ToString () + "  " + height.ToString () + "\n");
				TransformRows ( src, tmp, w, h);
				TransformCols ( tmp, src, w, h);
				w >>= 1;
				h >>= 1;
			}
		}


		/// <summary>
        /// Perfroms a two dimensional isotropic wavelet transformation for an array. The result is copied back to the declared array.
		/// </summary>
        virtual public void BacktransformIsotropic2D (float[,] src)
		{
			//Calculate the integral digits of log to the base two of the maximum of "width" and "height" 
			//The resulting number of "width | height" cannot have a greater log to the base 2 (integral digits)
			//than the greater of both values.
			int log2 = 1;
			int test = 1;
			while (test < (width | height)) {
				test <<= 1;
				log2++;
			}
			float[,] tmp = new float[width, height];
			int i = 1;
			while (i <= log2) {
				int w = width >> (log2 - i);
				int h = height >> (log2 - i);
				if ((w > 3) && (h > 3)) {
					//Console.WriteLine (w.ToString () + "  " + h.ToString () + "\n");
					InvTransformCols (src, tmp, w, h);
					InvTransformRows (tmp, src, w, h);
				}
				i++;
			}
		}

		virtual protected void ModifyCoefficients (float[,] src, int n,float[] scaleFactorsMajors, float scaleFactorsMinors, int gridSize)
		{
			if (scaleFactorsMajors != null)
			{
				if (scaleFactorsMajors.Length != n)
					throw new ArgumentException("scaleFactorsMajors must be null or the length must be of dimension n (" + n + ")");
			}
			if (gridSize < 1)
				throw new ArgumentException("gridSize (" + gridSize + ") cannot be smaller than 1" );
			if (n < 0)
				throw new ArgumentException("n (" + n + ") cannot be negative" );
			if (n > gridSize * gridSize)
				throw new ArgumentException("n" + n + " cannot be greater than " + gridSize + "*" + gridSize );
			int w = width /  gridSize;
			if ((w %  gridSize) != 0)
				w++;
			int h = width /  gridSize;
			if ((h %  gridSize) != 0)
				h++;
			int numBlocks = w * h;

			Parallel.For(0, numBlocks, block => 
			{
				int startX = (block % w) * gridSize;
				int startY = (block / w) * gridSize;

				int endX = startX + gridSize;
				int endY = startY + gridSize;
				if (endX > width)
					endX = width;
				if (endY > height)
					endY = height;

				float[,] tmpBlock = new float[gridSize, gridSize];
				bool[,] keep = new bool[gridSize, gridSize];
				Array.Clear(tmpBlock, 0, tmpBlock.Length);
				Array.Clear(keep, 0, keep.Length);

				for (int y = startY; y < endY; y++) {
					for (int x = startX; x < endX; x++) {
						tmpBlock [x - startX, y - startY] = Math.Abs (src [x, y]);
						keep [x - startX, y - startY] = false;
					}
				}
				for (int k = 0; k < n; k++) {
					float max = -1.0f;
					int maxIdxX = -1, maxIdxY = -1;
					for (int y = 0; y <  gridSize; y++) {
						for (int x = 0; x <  gridSize; x++) {
							if (!keep [x, y])
							if (tmpBlock [x, y] >= max) {
								max = tmpBlock[x,y];
								maxIdxX = x;
								maxIdxY = y;
							}
						}
					}
					keep[maxIdxX, maxIdxY] = true;
					//Scale all major coefficients (with greater amplitutes)
					//by the declared scale factor 
					if (scaleFactorsMajors != null)
							src[startX + x, startY + y] *= scaleFactorsMajors[k];

				}
				//all minor coefficients (with small amplitutes)
				//are multiplied by a certain factor (for denoising typically zero)
				for (int y = startY; y < endY; y++) {
					for (int x = startX; x < endX; x++) {
						if (!keep [x - startX, y - startY])
							src[x, y] *= scaleFactorsMinors;
					}
				}
			});
		}

        /// <summary>
        /// Set all but the greatest n coefficient to zero in the defined grid size
        /// </summary>
		virtual public void CropCoefficients (float[,] src, int n, int gridSize)
		{
			ModifyCoefficients (src, n, null, 0.0f, gridSize);
		}

        /// <summary>
        /// Scales the n (length of the scaleFactors array) greatest coefficinets (for a defined grid size) by the value declared in scaleFactors.
        /// </summary>
		virtual public void ScaleCoefficients (float[,] src, float[] scaleFactors, int gridSize)
		{
			if (scaleFactors == null)
				throw new ArgumentException("scaleFactors cannot be null");
			if (scaleFactors.Length > gridSize * gridSize)
				throw new ArgumentException("scaleFactors lenght cannot be greater than " + gridSize * gridSize);
			ModifyCoefficients (src,  scaleFactors.Length, scaleFactors, 1.0f, gridSize);
		}

	}
}
