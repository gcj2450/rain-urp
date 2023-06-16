using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGraphics.Scripts.AreaLight
{
	public partial class MyAreaLight : MonoBehaviour
	{
		private const CameraEvent c_cameraEvent = CameraEvent.AfterLighting;

		private static readonly float[,] s_offsets = new float[4, 2] {{1, 1}, {1, -1}, {-1, -1}, {-1, 1}};

		public MyAreaLightLUT areaLightLUTAsset;

		private readonly Dictionary<Camera, CommandBuffer> cameras = new Dictionary<Camera, CommandBuffer>();

		public Shader proxyShader;

		public Mesh cubeMesh;

		private Material proxyMaterial;

		private bool InitDirect()
		{
			if (proxyShader == null || cubeMesh == null)
			{
				return false;
			}

			proxyMaterial = new Material(proxyShader);
			proxyMaterial.hideFlags = HideFlags.HideAndDontSave;

			return true;
		}

		private void SetupLUTs()
		{
			proxyMaterial.SetTexture("_TransformInv_Diffuse", areaLightLUTAsset.transformInvTexture_Diffuse);
			proxyMaterial.SetTexture("_TransformInv_Specular", areaLightLUTAsset.transformInvTexture_Specular);
			proxyMaterial.SetTexture("_AmpDiffAmpSpecFresnel", areaLightLUTAsset.ampDiffAmpSpecFresnel);
		}

		private void SetupCommandBuffer()
		{
			//camera target is shadowmap
			if (InsideShadowmapCameraRender())
			{
				return;
			}

			var cam = Camera.current;
			var cmd = GetOrCreateCommandBuffer(cam);

			cmd.SetGlobalVector("_LightPos", transform.position);
			cmd.SetGlobalVector("_LightColor", GetColor());
			SetupLUTs();

			//vert_deferred vertex shader 需要 UnityDeferredLibrary.cginc
			//TODO:如果灯光与近平面和远平面相交，则将其渲染为四边形。
			//（还缺少：当靠近不相交时作为前面板渲染，模板优化）
			cmd.SetGlobalFloat("_LightAsQuad", 0);

			//向前偏移一点，以防止光照到自己-四边片
			var z = 0.01f;
			var t = transform;

			var lightVerts = new Matrix4x4();
			for (var i = 0; i < 4; i++)
			{
				lightVerts.SetRow(i
					, t.TransformPoint(new Vector3(size.x * s_offsets[i, 0], size.y * s_offsets[i, 1], z) * 0.5f));
			}

			cmd.SetGlobalMatrix("_LightVerts", lightVerts);

			if (enableShadows)
			{
				SetupShadowmapForSampling(cmd);
			}

			var m = Matrix4x4.TRS(new Vector3(0, 0, 10f), Quaternion.identity, Vector3.one * 20.0f);
			cmd.DrawMesh(cubeMesh, t.localToWorldMatrix * m, proxyMaterial, 0,
				enableShadows ? /*shadows*/ 0 : /*no shadows*/ 1);
		}

		private void Cleanup()
		{
			using var e = cameras.GetEnumerator();
			for (; e.MoveNext();)
			{
				var cam = e.Current;
				if (cam.Key != null && cam.Value != null)
				{
					cam.Key.RemoveCommandBuffer(c_cameraEvent, cam.Value);
				}
			}

			cameras.Clear();
		}

		private CommandBuffer GetOrCreateCommandBuffer(Camera cam)
		{
			if (cam == null)
			{
				return null;
			}

			CommandBuffer cmd = null;
			if (!cameras.ContainsKey(cam))
			{
				cmd = new CommandBuffer();
				cmd.name = /*"Area Light: "+*/gameObject.name;
				cameras[cam] = cmd;
				cam.AddCommandBuffer(c_cameraEvent, cmd);
				cam.depthTextureMode |= DepthTextureMode.Depth;
			}
			else
			{
				cmd = cameras[cam];
				cmd.Clear();
			}

			return cmd;
		}

		private void ReleaseTemporary(ref RenderTexture rt)
		{
			if (rt == null)
			{
				return;
			}

			RenderTexture.ReleaseTemporary(rt);
			rt = null;
		}

		private Color GetColor()
		{
			if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
			{
				return lightColor * intensity;
			}

			return new Color(
				Mathf.GammaToLinearSpace(lightColor.r),
				Mathf.GammaToLinearSpace(lightColor.g),
				Mathf.GammaToLinearSpace(lightColor.b),
				1.0f
			);
		}
	}
}