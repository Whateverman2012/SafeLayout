using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SafeLayout
{
	public class SafeLayoutPlugIn : Rhino.PlugIns.PlugIn
	{
		public SafeLayoutPlugIn()
		{
			Instance = this;
			Rhino.Display.RhinoView.SetActive += RhinoView_SetActive;
			Rhino.RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;

			if (this.Settings.Keys.Contains("new_layer_layout_visible"))
				new_layer_layout_visible = this.Settings.GetBool("new_layer_layout_visible");
			else
				this.Settings.SetBool("new_layer_layout_visible", new_layer_layout_visible);
		}

		private void RhinoDoc_LayerTableEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
		{
			// Not add event
			if (e.EventType != Rhino.DocObjects.Tables.LayerTableEventType.Added) return;
			//  In detail view
			//Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ViewportType;
			if (Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ViewportType == Rhino.Display.ViewportType.DetailViewport) return;

			// Update value possibly changed by command
			new_layer_layout_visible = this.Settings.GetBool("new_layer_layout_visible");

			// Holding shift inverse SafeLayout new layer behavior.
			if (new_layer_layout_visible != ((Eto.Forms.Keyboard.Modifiers & Eto.Forms.Keys.Shift) != 0)) return; 

			// Hide layer in every layouts and details
			Rhino.DocObjects.Layer layer = Rhino.RhinoDoc.ActiveDoc.Layers.FindIndex(e.LayerIndex);
			foreach (Rhino.Display.RhinoPageView pageView in Rhino.RhinoDoc.ActiveDoc.Views.GetPageViews())
			{
				//layer.SetPerViewportVisible(pageView.MainViewport.Id, false);
				foreach (Rhino.DocObjects.DetailViewObject detail in pageView.GetDetailViews())
					layer.SetPerViewportVisible(detail.Id, false);
			}
		}

		private void RhinoView_SetActive(object sender, Rhino.Display.ViewEventArgs e)
		{
			if (last_view_type != e.View.MainViewport.ViewportType)
			{
				if (e.View.MainViewport.ViewportType == Rhino.Display.ViewportType.StandardModelingViewport)
				{
					//Rhino.RhinoApp.WriteLine("from layout");
					Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Restore(layer_states_name, Rhino.DocObjects.Tables.RestoreLayerProperties.Visible);
				}
				else
				{
					//Rhino.RhinoApp.WriteLine("from model");
					Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Save(layer_states_name);
					foreach (Rhino.DocObjects.Layer layer in Rhino.RhinoDoc.ActiveDoc.Layers)
						Layer_Set_Visibility(layer, true);
				}

				last_view_type = e.View.MainViewport.ViewportType;
			}
		}

		private void Layer_Set_Visibility(Rhino.DocObjects.Layer a_layer, bool a_state) {
			if (a_layer.ParentLayerId != Guid.Empty)
				Layer_Set_Visibility(Rhino.RhinoDoc.ActiveDoc.Layers.FindId(a_layer.ParentLayerId), a_state);
			a_layer.IsVisible = a_state;
		}

		///<summary>Gets the only instance of the SafeLayoutPlugIn plug-in.</summary>
		public static SafeLayoutPlugIn Instance
		{
			get; private set;
		}

		public override Rhino.PlugIns.PlugInLoadTime LoadTime { get => Rhino.PlugIns.PlugInLoadTime.AtStartup; }
		private Rhino.Display.ViewportType last_view_type = Rhino.Display.ViewportType.StandardModelingViewport;
		private const String layer_states_name = "SafeLayout:ModelSpace";
		private bool new_layer_layout_visible = false;
	}
}