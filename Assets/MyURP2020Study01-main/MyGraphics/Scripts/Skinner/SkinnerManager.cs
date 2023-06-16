using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace MyGraphics.Scripts.Skinner
{
	public sealed class SkinnerManager
	{
		private static SkinnerManager instance;
		public static SkinnerManager Instance => instance ??= new SkinnerManager();

		//其实可以用字典来 来再次重构但是这里数量比较少 就算了
		private SkinnerSourceContainer sources;
		private SkinnerRenderContainer<SkinnerParticle, ParticlesRTIndex> particles;
		private SkinnerRenderContainer<SkinnerTrail, TrailRTIndex> trails;
		private SkinnerRenderContainer<SkinnerGlitch, GlitchRTIndex> glitches;
		private SkinnerRenderContainer<SkinnerDebug, DebugRTIndex> debugs;

		public List<SkinnerSource> Sources => sources.Skinners;
		public List<SkinnerParticle> Particles => particles.Skinners;
		public List<SkinnerTrail> Trails => trails.Skinners;
		public List<SkinnerGlitch> Glitches => glitches.Skinners;
		public List<SkinnerDebug> Debugs => debugs.Skinners;

		private SkinnerManager()
		{
			sources = new SkinnerSourceContainer();
			particles = new SkinnerRenderContainer<SkinnerParticle, ParticlesRTIndex>();
			trails = new SkinnerRenderContainer<SkinnerTrail, TrailRTIndex>();
			glitches = new SkinnerRenderContainer<SkinnerGlitch, GlitchRTIndex>();
			debugs = new SkinnerRenderContainer<SkinnerDebug, DebugRTIndex>();
		}

		public static bool CheckInstance()
		{
			return instance != null;
		}

		public void Update()
		{
			sources.Update();
			particles.Update();
			trails.Update();
			glitches.Update();
			debugs.Update();
		}

		//需要在 Pass中调用  不能在feature中
		//因为 pass 是被添加到渲染队列里面 之后执行的
		public void AfterRendering()
		{
			particles.AfterRendering();
			trails.AfterRendering();
			glitches.AfterRendering();
			debugs.AfterRendering();
			//不能调整顺序  sources的要放在最后
			sources.AfterRendering();
		}

		public void Register(SkinnerSource obj)
		{
			sources.Register(obj);
		}

		public void Register(SkinnerParticle obj)
		{
			particles.Register(obj);
		}

		public void Register(SkinnerTrail obj)
		{
			trails.Register(obj);
		}

		public void Register(SkinnerGlitch obj)
		{
			glitches.Register(obj);
		}

		public void Register(SkinnerDebug obj)
		{
			debugs.Register(obj);
		}


		public void Remove(SkinnerSource obj)
		{
			sources.Remove(obj);
			TryDestroy();
		}

		public void Remove(SkinnerParticle obj)
		{
			particles.Remove(obj);
			TryDestroy();
		}

		public void Remove(SkinnerTrail obj)
		{
			trails.Remove(obj);
			TryDestroy();
		}

		public void Remove(SkinnerGlitch obj)
		{
			glitches.Remove(obj);
			TryDestroy();
		}

		public void Remove(SkinnerDebug obj)
		{
			debugs.Remove(obj);
			TryDestroy();
		}

		private void TryDestroy()
		{
			if (sources.CanDestroy && particles.CanDestroy && trails.CanDestroy 
			    && glitches.CanDestroy && debugs.CanDestroy)
			{
				instance = null;
			}
		}
	}
}