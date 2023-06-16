using UnityEngine;

namespace MyGraphics.Scripts.GPUDrivenTerrain
{
	//copy by https://zhuanlan.zhihu.com/p/388844386
	public class GPUTerrain : MonoBehaviour
	{
		public TerrainAsset terrainAsset;

		public bool isFrustumCullEnabled = true;
		public bool isHizOcclusionCullingEnabled = true;

		[Range(0.01f, 1000)] public float hizDepthBias = 1;

		[Range(0, 100)] public int boundsHeightRedundance = 5;

		[Range(0.1f, 1.9f)] public float distanceEvaluation = 1.2f;


		/// <summary>
		/// 是否处理LOD之间的接缝问题
		/// </summary>
		public bool seamLess = true;

		/// <summary>
		/// 在渲染的时候，Patch之间留出一定缝隙供Debug
		/// </summary>
		public bool patchDebug = false;

		public bool nodeDebug = false;

		public bool mipDebug = false;

		public bool patchBoundsDebug = false;

		private TerrainBuilder _traverse;

		private Material _terrainMaterial;

		private bool _isTerrainMaterialDirty = false;

		private void Start()
		{
			_traverse = new TerrainBuilder(terrainAsset);
			terrainAsset.boundsDebugMaterial.SetBuffer("_BoundsList", _traverse.patchBoundsBuffer);
			ApplySettings();
		}

		void OnValidate()
		{
			ApplySettings();
		}

		private void ApplySettings()
		{
			if (_traverse != null)
			{
				_traverse.isFrustumCullEnabled = this.isFrustumCullEnabled;
				_traverse.isBoundsBufferOn = this.patchBoundsDebug;
				_traverse.isHizOcclusionCullingEnabled = this.isHizOcclusionCullingEnabled;
				_traverse.boundsHeightRedundance = this.boundsHeightRedundance;
				_traverse.enableSeamDebug = this.patchDebug;
				_traverse.nodeEvalDistance = this.distanceEvaluation;
				_traverse.hizDepthBias = this.hizDepthBias;
			}

			_isTerrainMaterialDirty = true;
		}

		void OnDestroy()
		{
			_traverse.Dispose();
		}

		void Update()
		{
			// if (Input.GetKeyDown(KeyCode.Space))
			// {
			// 	_traverse.Dispatch();
			// }

			if (isHizOcclusionCullingEnabled == true && HizMapRenderPass.HiZMap == null)
			{
				return;
			}

			_traverse.Dispatch();
			var terrainMaterial = EnsureTerrainMaterial();
			if (_isTerrainMaterialDirty)
			{
				UpdateTerrainMaterialProeprties();
			}

			Graphics.DrawMeshInstancedIndirect(TerrainAsset.patchMesh, 0, terrainMaterial,
				new Bounds(Vector3.zero, Vector3.one * 10240), _traverse.patchIndirectArgs);
			if (patchBoundsDebug)
			{
				Graphics.DrawMeshInstancedIndirect(TerrainAsset.unitCubeMesh, 0,
					terrainAsset.boundsDebugMaterial,
					new Bounds(Vector3.zero, Vector3.one * 10240), _traverse.boundsIndirectArgs);
			}
		}

		private Material EnsureTerrainMaterial()
		{
			if (!_terrainMaterial)
			{
				var material = terrainAsset.terrainMaterial;
				material.SetTexture("_HeightMap", terrainAsset.heightMap);
				material.SetTexture("_NormalMap", terrainAsset.normalMap);
				material.SetTexture("_MainTex", terrainAsset.albedoMap);
				material.SetBuffer("_PatchList", _traverse.culledPatchBuffer);
				_terrainMaterial = material;
				UpdateTerrainMaterialProeprties();
			}

			return _terrainMaterial;
		}


		private void UpdateTerrainMaterialProeprties()
		{
			_isTerrainMaterialDirty = false;
			if (_terrainMaterial)
			{
				if (seamLess)
				{
					_terrainMaterial.EnableKeyword("_ENABLE_LOD_SEAMLESS");
				}
				else
				{
					_terrainMaterial.DisableKeyword("_ENABLE_LOD_SEAMLESS");
				}

				if (mipDebug)
				{
					_terrainMaterial.EnableKeyword("_ENABLE_MIP_DEBUG");
				}
				else
				{
					_terrainMaterial.DisableKeyword("_ENABLE_MIP_DEBUG");
				}

				if (patchDebug)
				{
					_terrainMaterial.EnableKeyword("_ENABLE_PATCH_DEBUG");
				}
				else
				{
					_terrainMaterial.DisableKeyword("_ENABLE_PATCH_DEBUG");
				}

				if (nodeDebug)
				{
					_terrainMaterial.EnableKeyword("_ENABLE_NODE_DEBUG");
				}
				else
				{
					_terrainMaterial.DisableKeyword("_ENABLE_NODE_DEBUG");
				}

				_terrainMaterial.SetVector("_WorldSize", terrainAsset.worldSize);
				_terrainMaterial.SetMatrix("_WorldToNormalMapMatrix",
					Matrix4x4.Scale(this.terrainAsset.worldSize).inverse);
			}
		}
	}
}