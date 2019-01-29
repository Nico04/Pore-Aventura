﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisibilityControler : MonoBehaviour {
	public static VisibilityControler Instance;

	public SolidBoundariesBuilder SolidBoundariesBuilder;
	public TracerInjectionGridBuilder TracerInjectionGridBuilder;
	public StreamlinesBuilder StreamlinesBuilder;
	public GameObject SpawnParticlesHolderBatch;
	public GameObject StreamlinesHolder;
	
	public Material StructureTransparentMaterial;
	private Material _structureOpaqueMaterial;

	public Text BuildersStatusText;

	VisibilityControler() => Instance = this;
	
	private bool _askBuildersStatusTextUpdate = true;
	public void AskBuildersStatusUpdate() => _askBuildersStatusTextUpdate = true;

	private void Update() {
		if (_askBuildersStatusTextUpdate) {
			_askBuildersStatusTextUpdate = false;
			UpdateBuildersStatus();
		}
	}

	private bool _solidBoundariesVisibilityKeyIsDown = false;
	private bool _tracersVisibilityKeyIsDown = false;
	private bool _streamlinesVisibilityKeyIsDown = false;
	private void OnGUI() {
		if (!_solidBoundariesVisibilityKeyIsDown && Input.GetButtonDown("SolidBoundariesVisibility")) {
			_solidBoundariesVisibilityKeyIsDown = true;

			if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) { 
				//Toggle transparency
				ToggleSolidBoundariesTransparency();
			} else {
				//toggle visibility
				SolidBoundariesBuilder.IsVisible = !SolidBoundariesBuilder.IsVisible;
			}
		} else if (_solidBoundariesVisibilityKeyIsDown && Input.GetButtonUp("SolidBoundariesVisibility")) {
			_solidBoundariesVisibilityKeyIsDown = false;
		}

		if (!_tracersVisibilityKeyIsDown && Input.GetButtonDown("TracersVisibility")) {
			_tracersVisibilityKeyIsDown = true;
			TracerInjectionGridBuilder.IsVisible = !TracerInjectionGridBuilder.IsVisible;

			//SpawnParticlesHolderBatch.GetComponent<Renderer>().enabled = !SpawnParticlesHolderBatch.GetComponent<Renderer>().enabled;		//Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).
		} else if (_tracersVisibilityKeyIsDown && Input.GetButtonUp("TracersVisibility")) {
			_tracersVisibilityKeyIsDown = false;
		}

		if (!_streamlinesVisibilityKeyIsDown && Input.GetButtonDown("StreamlinesVisibility")) {
			_streamlinesVisibilityKeyIsDown = true;
			StreamlinesBuilder.IsVisible = !StreamlinesBuilder.IsVisible;

			//StreamlinesHolder.GetComponent<Renderer>().enabled = !StreamlinesHolder.GetComponent<Renderer>().enabled;       //Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).
		} else if (_streamlinesVisibilityKeyIsDown && Input.GetButtonUp("StreamlinesVisibility")) {
			_streamlinesVisibilityKeyIsDown = false;
		}
	}

	private bool _isStructureTransparent = false;
	private void ToggleSolidBoundariesTransparency() {
		//Get default opaque Material reference
		if (!_isStructureTransparent && _structureOpaqueMaterial == null)
			_structureOpaqueMaterial = SolidBoundariesBuilder.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;

		_isStructureTransparent = !_isStructureTransparent;
		Material material = _isStructureTransparent ? StructureTransparentMaterial : _structureOpaqueMaterial;

		for (int i = 0; i < SolidBoundariesBuilder.transform.childCount; i++) {
			SolidBoundariesBuilder.transform.GetChild(i).gameObject.GetComponent<Renderer>().material = material;
		}

		/** Old Method, not optimized because if the material is set to transparent, performance are as bad when color is opaque or transparent.
		_isStructureTransparent = !_isStructureTransparent;
		float transparency = _isStructureTransparent ? 0.3f : 1f;

		for (int i = 0; i < StructureHolder.transform.childCount; i++) {
			var particle = StructureHolder.transform.GetChild(i).gameObject;
			var particleMaterial = particle.GetComponent<Renderer>().material;

			//Toggle alpha channel
			particleMaterial.color = new Color(particleMaterial.color.r, particleMaterial.color.g, particleMaterial.color.b, transparency);
		}
		*/
	}

	private void UpdateBuildersStatus() {
		string text = "";
		foreach (var builder in Builder.Builders) {
			//First part
			text += "\n" + ColorizeUiText(builder.Name, builder.IsVisible ? Color.white : Color.gray);

			//Second part
			if (builder.IsLoading)
				text += $" <Building...>";

			//Third part
			if (builder.Type != Builder.Types.None)
				text += $" ({builder.Type.ToString()})";
		}

		BuildersStatusText.text = text;
	}


	private string ColorizeUiText(string input, Color color) {
		return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{input}</color>";
	}

	//Converti le text en text barré.
	public string StrikeThrough(string s) {
		string strikeThrough = "";
		foreach (char c in s) {
			strikeThrough = strikeThrough + c + '\u0336';
		}
		return strikeThrough;
	}
}