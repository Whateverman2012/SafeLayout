# -*- coding: utf-8 -*-
# Author : JF Lahos 2019/07/19
# Goal  : In layout space : All layers will be globally visibles (detail space left untouched)
#       : In model space  : It has it own layer states.
#       : A new layer will be either 'on' or 'off' depending on option in
#       : Document properties | Document user text : SafeLayout_NewLayerState
#       : If shift key is pressed while creation, SafeLayout_NewLayerState will be
#       : inversed momentarily
#
# Usage : Run file at Rhino start (Options | General | Command Lists)
#         -_RunPythonScript "your_path\SafeLayout_YYMMDD_HHMM.py"
#       : Or run it in python editor
# Disclaimer : Use at your own risk.

import Rhino
import rhinoscriptsyntax as rs
import scriptcontext as sc
import Eto

# Events -------------------------------------------------------------------------------------------------------------------

def RegisterEvent(event, event_name, event_function):
	if sc.sticky.has_key(event_name):      # Unregistering first
		event -= sc.sticky[event_name]
	sc.sticky[event_name] = event_function # Update/Store function
	event += event_function                # Registering

# RhinoDoc_LayerTableEvent_SetNewLayerStateInLayouts

# Initialize global variables for SetNewLayerStateInLayouts function
def SetNewLayerStateInLayouts_Init():
	# Get it from document user text
	new_layer_state = rs.GetDocumentUserText("SafeLayout_NewLayerState")
	if new_layer_state is None:
		new_layer_state = "off"
	else:
		new_layer_state = new_layer_state.lower()
		if not new_layer_state in ['on', 'off']:
			new_layer_state = 'off'
		
	sc.sticky["SafeLayout_NewLayerState"] = new_layer_state
	# Set it to document user text
	rs.SetDocumentUserText("SafeLayout_NewLayerState", new_layer_state)

def SetNewLayerStateInLayouts(sender, e):
	if e.EventType != Rhino.DocObjects.Tables.LayerTableEventType.Added: # New layer event
		return # "Added" event only

	if Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.MainViewport.ViewportType == Rhino.Display.ViewportType.DetailViewport:
		return # Inside a detail... 

	keep_layer_on = (sc.sticky["SafeLayout_NewLayerState"] == "on")
	
	# Shift inverse behavior
	if Eto.Forms.Keyboard.Modifiers & Eto.Forms.Keys.Shift: # Shift is pressed
		keep_layer_on = not keep_layer_on

	if keep_layer_on:
		return
	
	# Turn it off
	pages = sc.doc.Views.GetPageViews()
	layer = sc.doc.Layers.FindIndex(e.LayerIndex)
	if layer: # layer's valid ? parse all details in all layouts and turn it off
		for p in pages:
			for detail in p.GetDetailViews():
				Rhino.DocObjects.Layer.SetPerViewportVisible(layer, detail.Id, False)
						
# RhinoView_SetActive_ChangeLayerStates

# Initialize global variables for ChangeLayerStates function
def ChangeLayerStates_Init():
	sc.sticky["ModelLayerStatesName"] = "SafeLayout_ModelLayerStates"
	sc.sticky["LastViewType"] = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.MainViewport.ViewportType

def ChangeLayerStates(sender, e):
	if e.View.MainViewport.ViewportType == sc.sticky["LastViewType"]:
		return
	
	# Switching to model space
	if e.View.MainViewport.ViewportType == Rhino.Display.ViewportType.StandardModelingViewport:
		# Restore model space layer states
		if Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.FindName(sc.sticky["ModelLayerStatesName"]) != -1:
			Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Restore(sc.sticky["ModelLayerStatesName"], Rhino.DocObjects.Tables.RestoreLayerProperties.Visible)
		e.View.Redraw()
	# Switching from model space
	else:
		# Save model space layer states
		Rhino.RhinoDoc.ActiveDoc.NamedLayerStates.Save(sc.sticky["ModelLayerStatesName"])
		# Turn on all model space layers
		for layer in sc.doc.Layers:
			layer.IsVisible = True
			layer.CommitChanges()
		e.View.Redraw()
		
	# Remember last space
	sc.sticky["LastViewType"] = e.View.MainViewport.ViewportType

# LayerTableEvent_Added : set new layers invisibles in existing layouts
SetNewLayerStateInLayouts_Init()
RegisterEvent(
	Rhino.RhinoDoc.LayerTableEvent,
	"RhinoDoc_LayerTableEvent_SetNewLayerStateInLayouts",
	SetNewLayerStateInLayouts)

# ViewEvent_SetActive : ModelSpace keeps its own layer states, LayoutSpace keeps all layers visibles
ChangeLayerStates_Init()
RegisterEvent(
	Rhino.Display.RhinoView.SetActive,
	"RhinoView_SetActive_ChangeLayerStates",
	ChangeLayerStates)