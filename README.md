# TurboWavelets.Net
<a href="https://ci.appveyor.com/project/codeprof/turbowavelets-net"><img src="https://ci.appveyor.com/api/projects/status/5j9nc0pqnd7aibbh?svg=true"></a></br>

TurboWavelets.Net provides very fast, flexible and compact implementations of discrete wavelet transformations in C#.
Unlike others this implementation has no limitation is sizes for the transformation (lengths like 39, 739,... are possible, not just power of two numbers) 
At the moment only floating point numbers are supported.
# Features:
- 1D biorthogonal 5/3 wavelet using the lifting scheme (for arbitrary sizes, not just power of 2)
- 2D biorthogonal 5/3 wavelet using the lifting scheme (for arbitrary sizes, not just power of 2)
- 2D haar wavelet (for arbitrary sizes, not just power of 2)
- 2D cascade sorting of coefficients  (for arbitrary sizes, not just power of 2)
- Scale/Crop coefficients in a defined grid
- apply a deadzone
- Multithreaded and threadsafe

# Licence
MIT License (MIT)
