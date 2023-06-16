using UnityEngine;

namespace MyGraphics.Scripts.GPUDrivenTerrain
{
	public static class TextureUtility
	{
		public static RenderTexture CreateRenderTextureWithMipTextures(Texture2D[] mipmaps, RenderTextureFormat format)
		{
			var mip0 = mipmaps[0];
			var descriptor =
				new RenderTextureDescriptor(mip0.width, mip0.height, format, 0, mipmaps.Length)
				{
					autoGenerateMips = false,
					useMipMap = true
				};
			var rt = new RenderTexture(descriptor)
			{
				filterMode = mip0.filterMode
			};
			rt.Create();
			for (var i = 0; i < mipmaps.Length; i++)
			{
				Graphics.CopyTexture(mipmaps[i], 0, 0, rt, 0, i);
			}

			return rt;
		}

		public static RenderTexture CreateLODMap(int size)
		{
			var descriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.R8, 0, 1)
			{
				autoGenerateMips = false, 
				enableRandomWrite = true
			};
			RenderTexture rt = new RenderTexture(descriptor) {filterMode = FilterMode.Point};
			rt.Create();
			return rt;
		}
	}
}