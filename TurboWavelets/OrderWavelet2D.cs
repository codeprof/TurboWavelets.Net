using System;
using System.Threading.Tasks;

namespace TurboWavelets.Net
{
    public class OrderWavelet2D : Wavelet2D
    {
		/// <summary>
		/// A fast implementation of a two-dimensional ordering transformation
		/// for arbitary lenghts (works for all sizes, not just power of 2)
		/// This does not perform a complete wavelet transformation. It
		/// just does the ordering of the values.
		/// The implementation takes advantage of multiple CPU cores.
		/// </summary>
		/// <param name="width">The width of the transformation</param>
		/// <param name="height">The width of the transformation</param>
        public OrderWavelet2D(int width, int height)
            : base(2, 2, width, height)
        {   
        }

        /// <summary>
        /// Initalizes a two dimensional wavelet transformation
        /// </summary>
        public OrderWavelet2D(int width, int height, int minSize)
            : base(minSize, 2, width, height)
        {
        }

        override protected void TransformRows(float[,] src, float[,] dst, int w, int h)
        {
			Parallel.For(0, h, y => 
			{
				int length = w;
				if (length >= allowedMinSize) {
					int half          = length >> 1;
					int offsrc        = 0;
					// number of low frequency values
					int num_lf_values = half + (length & 1);

					for (int i = 0; i < half; i++) {
						dst[i, y]                 = src[offsrc, y];
						dst[i + num_lf_values, y] = src[offsrc + 1, y];
						offsrc += 2;
					}							
					if ((length & 1) != 0)
						dst[num_lf_values, y] = src[length - 1, y];
				}
				else
				{
					for(int i = 0; i < length; i++)
						dst[i, y] = src[i, y];
				}
			});
        }

        override protected void TransformCols(float[,] src, float[,] dst, int w, int h)
        {
			Parallel.For(0, w, x => 
			{
				int length = h;
				if (length >= allowedMinSize) {
					int half          = length >> 1;
					int offsrc        = 0;
					// number of low frequency values
					int num_lf_values = half + (length & 1);

					for (int i = 0; i < half; i++) {
						dst[x, i]                 = src[x, offsrc];
						dst[x, i + num_lf_values] = src[x, offsrc + 1];
						offsrc += 2;
					}							
					if ((length & 1) != 0)
						dst[x, num_lf_values] = src[x, length - 1];
				}
				else
				{
					for(int i = 0; i < length; i++)
						dst[x, i] = src[x, i];
				}
			});
        }

        override protected void InvTransformRows(float[,] src, float[,] dst, int w, int h)
        {
			Parallel.For(0, h, y => 
			{
				int length = w;
				if (length >= allowedMinSize) {
					int half          = length >> 1;
					int offdst        = 0;
					// number of low frequency values
					int num_lf_values = half + (length & 1);

					for (int i = 0; i < half; i++) {
						dst[offdst, y] = src[i, y];
						dst[offdst + 1, y] = src[i + num_lf_values, y];
						offdst += 2;
					}							
					if ((length & 1) != 0)
						dst[length - 1, y] = src[num_lf_values, y]; 
				}
				else
				{
					for(int i = 0; i < length; i++)
						dst[i, y] = src[i, y];
				}
			});
        }

        override protected void InvTransformCols(float[,] src, float[,] dst, int w, int h)
        {
			Parallel.For(0, w, x => 
			{
				int length = h;
				if (length >= allowedMinSize) {
					int half          = length >> 1;
					int offdst        = 0;
					// number of low frequency values
					int num_lf_values = half + (length & 1);

					for (int i = 0; i < half; i++) {
						dst[x, offdst] = src[x, i];
						dst[x, offdst + 1] = src[x, i + num_lf_values];
						offdst += 2;
					}							
					if ((length & 1) != 0)
						dst[x, length - 1] = src[x, num_lf_values]; 
				}
				else
				{
					for(int i = 0; i < length; i++)
						dst[x, i] = src[x, i];
				}
			});
        }
	}
}