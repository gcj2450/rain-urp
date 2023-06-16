﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;

namespace MyGraphics.Scripts.TAA
{
	public enum Pattern
	{
		Still,
		Uniform2,
		Uniform4,
		Uniform4_Helix,
		Uniform4_DoubleHelix,
		SkewButterfly,
		Rotated4,
		Rotated4_Helix,
		Rotated4_Helix2,
		Poisson10,
		Pentagram,
		Halton_2_3_X8,
		Halton_2_3_X16,
		Halton_2_3_X32,
		Halton_2_3_X256,
		MotionPerp2,
	}

	public class TAAFrustumJitterRenderPass : ScriptableRenderPass
	{
		private const string k_tag = "TAA_FrustumJitter";

		private static readonly bool _initialized = false;

		static TAAFrustumJitterRenderPass()
		{
			if (_initialized == false)
			{
				_initialized = true;

				// points_Pentagram
				Vector2 vh = new Vector2(points_Pentagram[0] - points_Pentagram[2],
					points_Pentagram[1] - points_Pentagram[3]);
				Vector2 vu = new Vector2(0.0f, 1.0f);
				TransformPattern(ref points_Pentagram, Mathf.Deg2Rad * (0.5f * Vector2.Angle(vu, vh)), 1.0f);

				// points_Halton_2_3_xN
				InitializeHalton_2_3(ref points_Halton_2_3_x8);
				InitializeHalton_2_3(ref points_Halton_2_3_x16);
				InitializeHalton_2_3(ref points_Halton_2_3_x32);
				InitializeHalton_2_3(ref points_Halton_2_3_x256);
			}
		}

		#region Point distributions

		private static float[] points_Still = new float[]
		{
			0.5f, 0.5f,
		};

		private static float[] points_Uniform2 = new float[]
		{
			-0.25f, -0.25f, //ll
			0.25f, 0.25f, //ur
		};

		private static float[] points_Uniform4 = new float[]
		{
			-0.25f, -0.25f, //ll
			0.25f, -0.25f, //lr
			0.25f, 0.25f, //ur
			-0.25f, 0.25f, //ul
		};

		private static float[] points_Uniform4_Helix = new float[]
		{
			-0.25f, -0.25f, //ll  3  1
			0.25f, 0.25f, //ur   \/|
			0.25f, -0.25f, //lr   /\|
			-0.25f, 0.25f, //ul  0  2
		};

		private static float[] points_Uniform4_DoubleHelix = new float[]
		{
			-0.25f, -0.25f, //ll  3  1
			0.25f, 0.25f, //ur   \/|
			0.25f, -0.25f, //lr   /\|
			-0.25f, 0.25f, //ul  0  2
			-0.25f, -0.25f, //ll  6--7
			0.25f, -0.25f, //lr   \
			-0.25f, 0.25f, //ul    \
			0.25f, 0.25f, //ur  4--5
		};

		private static float[] points_SkewButterfly = new float[]
		{
			-0.250f, -0.250f,
			0.250f, 0.250f,
			0.125f, -0.125f,
			-0.125f, 0.125f,
		};

		private static float[] points_Rotated4 = new float[]
		{
			-0.125f, -0.375f, //ll
			0.375f, -0.125f, //lr
			0.125f, 0.375f, //ur
			-0.375f, 0.125f, //ul
		};

		private static float[] points_Rotated4_Helix = new float[]
		{
			-0.125f, -0.375f, //ll  3  1
			0.125f, 0.375f, //ur   \/|
			0.375f, -0.125f, //lr   /\|
			-0.375f, 0.125f, //ul  0  2
		};

		private static float[] points_Rotated4_Helix2 = new float[]
		{
			-0.125f, -0.375f, //ll  2--1
			0.125f, 0.375f, //ur   \/
			-0.375f, 0.125f, //ul   /\
			0.375f, -0.125f, //lr  0  3
		};

		private static float[] points_Poisson10 = new float[]
		{
			-0.16795960f * 0.25f, 0.65544910f * 0.25f,
			-0.69096030f * 0.25f, 0.59015970f * 0.25f,
			0.49843820f * 0.25f, 0.83099720f * 0.25f,
			0.17230150f * 0.25f, -0.03882703f * 0.25f,
			-0.60772670f * 0.25f, -0.06013587f * 0.25f,
			0.65606390f * 0.25f, 0.24007600f * 0.25f,
			0.80348370f * 0.25f, -0.48096900f * 0.25f,
			0.33436540f * 0.25f, -0.73007030f * 0.25f,
			-0.47839520f * 0.25f, -0.56005300f * 0.25f,
			-0.12388120f * 0.25f, -0.96633990f * 0.25f,
		};

		private static float[] points_Pentagram = new float[]
		{
			0.000000f * 0.5f, 0.525731f * 0.5f, // head
			-0.309017f * 0.5f, -0.425325f * 0.5f, // lleg
			0.500000f * 0.5f, 0.162460f * 0.5f, // rarm
			-0.500000f * 0.5f, 0.162460f * 0.5f, // larm
			0.309017f * 0.5f, -0.425325f * 0.5f, // rleg
		};

		private static float[] points_Halton_2_3_x8 = new float[8 * 2];
		private static float[] points_Halton_2_3_x16 = new float[16 * 2];
		private static float[] points_Halton_2_3_x32 = new float[32 * 2];
		private static float[] points_Halton_2_3_x256 = new float[256 * 2];

		private static float[] points_MotionPerp2 = new float[]
		{
			0.00f, -0.25f,
			0.00f, 0.25f,
		};

		private static void TransformPattern(ref float[] seq, float theta, float scale)
		{
			float cs = Mathf.Cos(theta);
			float sn = Mathf.Sin(theta);
			for (int i = 0, j = 1, n = seq.Length; i != n; i += 2, j += 2)
			{
				float x = scale * seq[i];
				float y = scale * seq[j];
				seq[i] = x * cs - y * sn;
				seq[j] = x * sn + y * cs;
			}
		}

		// http://en.wikipedia.org/wiki/Halton_sequence
		private static float HaltonSeq(int prime, int index = 1 /* NOT! zero-based */)
		{
			float r = 0f;
			float f = 1f;
			int i = index;
			while (i > 0)
			{
				f /= prime;
				r += f * (i % prime);
				i = (int) Mathf.Floor(i / (float) prime);
			}

			return r;
		}

		private static void InitializeHalton_2_3(ref float[] seq)
		{
			for (int i = 0, n = seq.Length / 2; i != n; i++)
			{
				float u = HaltonSeq(2, i + 1) - 0.5f;
				float v = HaltonSeq(3, i + 1) - 0.5f;
				seq[2 * i + 0] = u;
				seq[2 * i + 1] = v;
			}
		}

		private static float[] AccessPointData(Pattern pattern)
		{
			switch (pattern)
			{
				case Pattern.Still:
					return points_Still;
				case Pattern.Uniform2:
					return points_Uniform2;
				case Pattern.Uniform4:
					return points_Uniform4;
				case Pattern.Uniform4_Helix:
					return points_Uniform4_Helix;
				case Pattern.Uniform4_DoubleHelix:
					return points_Uniform4_DoubleHelix;
				case Pattern.SkewButterfly:
					return points_SkewButterfly;
				case Pattern.Rotated4:
					return points_Rotated4;
				case Pattern.Rotated4_Helix:
					return points_Rotated4_Helix;
				case Pattern.Rotated4_Helix2:
					return points_Rotated4_Helix2;
				case Pattern.Poisson10:
					return points_Poisson10;
				case Pattern.Pentagram:
					return points_Pentagram;
				case Pattern.Halton_2_3_X8:
					return points_Halton_2_3_x8;
				case Pattern.Halton_2_3_X16:
					return points_Halton_2_3_x16;
				case Pattern.Halton_2_3_X32:
					return points_Halton_2_3_x32;
				case Pattern.Halton_2_3_X256:
					return points_Halton_2_3_x256;
				case Pattern.MotionPerp2:
					return points_MotionPerp2;
				default:
					Debug.LogError("missing point distribution");
					return points_Halton_2_3_x16;
			}
		}

		public static int AccessLength(Pattern pattern)
		{
			return AccessPointData(pattern).Length / 2;
		}

		#endregion

		private TAAPostProcess settings;

		public static Vector4 activeSample = Vector4.zero; // xy = current sample, zw = previous sample
		public int activeIndex = -1;

		private float3 focalMotionPos = float3.zero;
		private float3 focalMotionDir = Vector3.right;

		public TAAFrustumJitterRenderPass()
		{
			profilingSampler = new ProfilingSampler(k_tag);
		}


		public void Setup(TAAPostProcess _settings, Camera _camera)
		{
			settings = _settings;
			settings.activeSample = Vector4.zero;
			_camera.ResetProjectionMatrix();
		}

		public Vector2 Sample(Pattern pattern, int index)
		{
			float[] points = AccessPointData(pattern);
			int n = points.Length / 2;
			int i = index % n;

			float patternScale = settings.patternScale.value;

			float x = patternScale * points[2 * i + 0];
			float y = patternScale * points[2 * i + 1];

			if (pattern != Pattern.MotionPerp2)
				return new Vector2(x, y);
			else
				return new Vector2(x, y).Rotate(
					Vector2.right.SignedAngle(new Vector2(focalMotionDir.x, focalMotionDir.y)));
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var camera = renderingData.cameraData.camera;
			if (camera.orthographic)
			{
				activeSample = Vector4.zero;
				activeIndex = -1;
				// settings.activeSample = activeSample;

				return;
			}

			CommandBuffer cmd = CommandBufferPool.Get(k_tag);
			using (new ProfilingScope(cmd, profilingSampler))
			{
				// update motion dir
				{
					float3 oldWorld = focalMotionPos;
					float3 newWorld = camera.transform.TransformVector(camera.nearClipPlane * Vector3.forward);

					float4x4 worldToCameraMatrix = camera.worldToCameraMatrix;
					float3 oldPoint = math.mul(worldToCameraMatrix, new float4(oldWorld, 1f)).xyz;
					float3 newPoint = math.mul(worldToCameraMatrix, new float4(newWorld, 1f)).xyz;
					float3 newDelta = newPoint - oldPoint;
					newDelta.z = 0f;

					var sqMag = math.lengthsq(newDelta);
					if (sqMag != 0f)
					{
						// yes, apparently this is necessary instead of newDelta.normalized... because facepalm
						var dir = newDelta / math.sqrt(sqMag);
						focalMotionPos = newWorld;
						focalMotionDir = Vector3.Slerp(focalMotionDir, dir, 0.2f);
						//Debug.Log("CHANGE focalMotionDir " + focalMotionDir.ToString("G4") + " delta was " + newDelta.ToString("G4") + " delta.mag " + newDelta.magnitude);
					}
				}


				// update jitter
				{
					var pattern = settings.pattern.value;

					activeIndex += 1;
					activeIndex %= AccessLength(pattern);

					Vector2 sample = Sample(pattern, activeIndex);
					activeSample.z = activeSample.x;
					activeSample.w = activeSample.y;
					activeSample.x = sample.x;
					activeSample.y = sample.y;
					settings.activeSample = activeSample;
					var proj = camera.GetPerspectiveProjection(sample.x, sample.y);
					cmd.SetProjectionMatrix(proj);
					camera.projectionMatrix = proj;
				}

				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}