using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MyGraphics.Scripts.Skinner
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class SkinnerTrail : MonoBehaviour, ISkinnerSetting
	{
		[SerializeField] private Material mat;

		[SerializeField] private SkinnerSource source;

		[SerializeField] [Tooltip("Reference to a template object used for rendering trail lines.")]
		public SkinnerTrailTemplate template;

		[SerializeField] public bool useMRT;

		//Dynamics settings
		//----------------------------------

		[SerializeField, Tooltip("Limits an amount of a vertex movement. This only affects changes " +
		                         "in vertex positions (doesn't change velocity vectors).")]
		private float speedLimit = 0.4f;

		[SerializeField, Tooltip("Drag coefficient (damping coefficient).")]
		private float drag = 5;

		//Line width modifier
		//----------------------------------

		[SerializeField, Min(0f), Tooltip("Part of lines under this speed will be culled.")]
		private float cutoffSpeed = 0;

		[SerializeField, Min(0f), Tooltip("Increases the line width based on its speed.")]
		private float speedToWidth = 0.02f;


		[SerializeField, Min(0f), Tooltip("The maximum width of lines.")]
		private float maxWidth = 0.05f;

		//Other settings
		//----------------------------------

		[SerializeField, Tooltip("Determines the random number sequence used for the effect.")]
		private int randomSeed = 0;

		private bool reconfigured;
		private bool resetMat;

		private SkinnerData data;

		public SkinnerSource Source
		{
			get => source;
			set
			{
				source = value;
				reconfigured = true;
			}
		}

		public SkinnerTrailTemplate Template
		{
			get => template;
			set
			{
				template = value;
				reconfigured = true;
			}
		}

		/// Limits an amount of a vertex movement. This only affects changes
		/// in vertex positions (doesn't change velocity vectors).
		public float SpeedLimit
		{
			get => speedLimit;
			set => speedLimit = value;
		}

		/// Drag coefficient (damping coefficient).
		public float Drag
		{
			get => drag;
			set => drag = value;
		}

		/// Part of lines under this speed will be culled.
		public float CutoffSpeed
		{
			get => cutoffSpeed;
			set
			{
				cutoffSpeed = Mathf.Max(value, 0);
				resetMat = true;
			}
		}

		/// Increases the line width based on its speed.
		public float SpeedToWidth
		{
			get => speedToWidth;
			set
			{
				speedToWidth = Mathf.Max(value, 0);
				resetMat = true;
			}
		}

		/// The maximum width of lines.
		public float MaxWidth
		{
			get => maxWidth;
			set
			{
				maxWidth = Mathf.Max(value, 0);
				resetMat = true;
			}
		}

		/// Determines the random number sequence used for the effect.
		public int RandomSeed
		{
			get => randomSeed;
			set
			{
				randomSeed = value;
				reconfigured = true;
			}
		}


		public Material Mat => mat;
		public SkinnerData Data => data;
		public bool UseMRT
		{
			get => useMRT;
			set => useMRT = value;
		}
		public bool Reconfigured => reconfigured;
		public int Width => Source == null || Source.Model == null ? 0 : Source.Model.VertexCount;
		public int Height => Template == null ? 0 : Template.HistoryLength;

		public bool CanRender =>
			mat != null && template != null && source != null && source.CanRender;

		private void OnEnable()
		{
			if (!CanRender)
			{
				return;
			}

			GetComponent<MeshFilter>().mesh = template.Mesh;
			data = new SkinnerData()
			{
				mat = mat
			};
			reconfigured = true;
			resetMat = true;
			SkinnerManager.Instance.Register(this);
		}

		private void OnDisable()
		{
			SkinnerManager.Instance.Remove(this);
		}

		private void Reset()
		{
			reconfigured = true;
			resetMat = true;
		}

		// private void OnValidate()
		// {
		// 	reconfigured = true;
		// 	resetMat = true;
		// }

		public void UpdateMat()
		{
			reconfigured = false;

			if (resetMat)
			{
				resetMat = false;
				mat.SetVector(SkinnerShaderConstants.LineWidth_ID,
					new Vector4(maxWidth, cutoffSpeed, speedToWidth / maxWidth, 0));
			}

			if (data.HaveRTs)
			{
				mat.SetTexture(SkinnerShaderConstants.TrailPositionTex_ID,
					data.CurrTex(TrailRTIndex.Position));
				mat.SetTexture(SkinnerShaderConstants.TrailVelocityTex_ID,
					data.CurrTex(TrailRTIndex.Velocity));
				mat.SetTexture(SkinnerShaderConstants.TrailOrthnormTex_ID,
					data.CurrTex(TrailRTIndex.Orthnorm));
				mat.SetTexture(SkinnerShaderConstants.TrailPrevPositionTex_ID,
					data.PrevTex(TrailRTIndex.Position));
				mat.SetTexture(SkinnerShaderConstants.TrailPrevVelocityTex_ID,
					data.PrevTex(TrailRTIndex.Velocity));
				mat.SetTexture(SkinnerShaderConstants.TrailPrevOrthnormTex_ID,
					data.PrevTex(TrailRTIndex.Orthnorm));
			}
		}
	}
}