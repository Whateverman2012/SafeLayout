﻿using System;
using System.Diagnostics;

namespace SafeLayout
{
	public class SafeLayoutPlugIn : Rhino.PlugIns.PlugIn
	{
		public SafeLayoutPlugIn()
		{
			Instance = this;

			if (!this.Settings.Keys.Contains("enabled"))
				this.Settings.SetBool("enabled", true);
			if (!this.Settings.Keys.Contains("new_layer_layout_visible"))
				this.Settings.SetBool("new_layer_visible_in_layout", false);

			Rhino.Display.RhinoView.SetActive += RhinoView_SetActive;
			Rhino.RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;
		}

		private void RhinoDoc_LayerTableEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
		{

			// enabled ?
			if (!this.Settings.GetBool("enabled")) return;

			// Not add event
			if (e.EventType != Rhino.DocObjects.Tables.LayerTableEventType.Added) return;

			//  In detail view
			if (Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ViewportType == Rhino.Display.ViewportType.DetailViewport) return;

			// Holding shift inverse SafeLayout new layer behavior.
			if (this.Settings.GetBool("new_layer_visible_in_layout") != ((Eto.Forms.Keyboard.Modifiers & Eto.Forms.Keys.Shift) != 0)) return; 

			// Hide layer in every layouts and details
			Rhino.DocObjects.Layer layer = Rhino.RhinoDoc.ActiveDoc.Layers.FindIndex(e.LayerIndex);
			foreach (Rhino.Display.RhinoPageView pageView in Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews())
			{
				//layer.SetPerViewportVisible(pageView.MainViewport.Id, false);
				foreach (Rhino.DocObjects.DetailViewObject detail in pageView.GetDetailViews())
					layer.SetPerViewportVisible(detail.Id, false);
			}

			Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
			//RhinoApp.WriteLine("SL : RhinoDoc_LayerTableEvent");	
		}

		private void RhinoView_SetActive(object sender, Rhino.Display.ViewEventArgs e)
		{
			// enabled ?
			if (!this.Settings.GetBool("enabled")) return;

			if (last_view_type != e.View.MainViewport.ViewportType || last_view_type == (Rhino.Display.ViewportType)(-1))
			{
				last_view_type = e.View.MainViewport.ViewportType;

				if (e.View.MainViewport.ViewportType == Rhino.Display.ViewportType.StandardModelingViewport)
				{
					//Rhino.RhinoApp.WriteLine("from layout");
					Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Restore(layer_states_name, Rhino.DocObjects.Tables.RestoreLayerProperties.Visible);
				}
				else
				{
					//Rhino.RhinoApp.WriteLine("from model");
					if (!Rhino.RhinoDoc.ActiveDoc.IsOpening)
					{
						Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Save(layer_states_name);
					}
					foreach (Rhino.DocObjects.Layer layer in Rhino.RhinoDoc.ActiveDoc.Layers)
						if (!layer.IsDeleted)
							Rhino.RhinoDoc.ActiveDoc.Layers.ForceLayerVisible(layer.Id);
				}
				Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
			}
			//RhinoApp.WriteLine("SL : RhinoView_SetActive");
		}

		///<summary>Gets the only instance of the SafeLayoutPlugIn plug-in.</summary>
		public static SafeLayoutPlugIn Instance
		{
			get; private set;
		}

		public override Rhino.PlugIns.PlugInLoadTime LoadTime { get => Rhino.PlugIns.PlugInLoadTime.AtStartup; }
		private Rhino.Display.ViewportType last_view_type = (Rhino.Display.ViewportType)(-1);
		private const String layer_states_name = "SafeLayout:ModelSpace";
	}
}