using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace SafeLayout
{
	public class SafeLayoutCommand : Command
	{
		public SafeLayoutCommand()
		{
			// Rhino only creates one instance of each command class defined in a
			// plug-in, so it is safe to store a refence in a static property.
			Instance = this;
		}

		///<summary>The only instance of this command.</summary>
		public static SafeLayoutCommand Instance
		{
			get; private set;
		}

		///<returns>The command name as it appears on the Rhino command line.</returns>
		public override string EnglishName
		{
			get { return "SafeLayout"; }
		}

		protected override Result RunCommand(RhinoDoc doc, RunMode mode)
		{
			// Get persistent settings (from Registry)
			PersistentSettings settings = this.PlugIn.Settings;
			bool save_settings = false;
			bool enabled = settings.GetBool("enabled", true);
			bool new_layer_visible_in_layout = settings.GetBool("new_layer_layout_visible", false);

			GetOption go = new GetOption();
			OptionToggle option_toggle_enabled = new OptionToggle(enabled, "off", "on");
			OptionToggle option_toggle_newLayerLayoutVisible = new OptionToggle(new_layer_visible_in_layout, "off", "on");
			go.AddOptionToggle("enabled", ref option_toggle_enabled);
			go.AddOptionToggle("new_layer_visible_in_layout", ref option_toggle_newLayerLayoutVisible);
			go.SetCommandPrompt("Safe Layout Settings");

			Rhino.Input.GetResult get_rc = go.Get();
			Result rc = go.CommandResult();

			if (enabled != option_toggle_enabled.CurrentValue)
			{
				enabled = option_toggle_enabled.CurrentValue;
				settings.SetBool("enabled", enabled);
				save_settings = true;
			}
			if (new_layer_visible_in_layout != option_toggle_newLayerLayoutVisible.CurrentValue)
			{
				new_layer_visible_in_layout = option_toggle_newLayerLayoutVisible.CurrentValue;
				settings.SetBool("new_layer_visible_in_layout", new_layer_visible_in_layout);
				save_settings = true;
			}

			if (save_settings) this.PlugIn.SaveSettings();

			return rc;
		}
	}
}
