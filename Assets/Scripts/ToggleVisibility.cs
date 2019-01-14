using UnityEngine;
using UnityEngine.Experimental.VFX;

public class ToggleVisibility : MonoBehaviour {
	public GameObject StructureHolder;
	public GameObject SpawnParticlesHolder;
	public GameObject SpawnParticlesHolderBatch;
	public GameObject StreamlinesHolder;
	
	public Material StructureTransparentMaterial;
	private Material StructureOpaqueMaterial;

	private void Start() {
		//Get default opaque Material reference
		StructureOpaqueMaterial = StructureHolder.transform.GetChild(0).gameObject.GetComponent<Renderer>().material;
	}

	private bool _askToggleStructureVisibility = false;
	private bool _askToggleSpawnParticlesVisibility = false;
	private bool _askToggleStreamlinesVisibility = false;

	private void Update() {
		if (_askToggleStructureVisibility) {
			_askToggleStructureVisibility = false;
			StructureHolder.SetActive(!StructureHolder.activeInHierarchy);      //SetActive must be called in the Update() and NOT in OnGUI()
		}

		if (_askToggleSpawnParticlesVisibility) {
			_askToggleSpawnParticlesVisibility = false;
			SpawnParticlesHolder.SetActive(!SpawnParticlesHolder.activeInHierarchy);        //SetActive must be called in the Update() and NOT in OnGUI()
			SpawnParticlesHolderBatch.GetComponent<Renderer>().enabled = !SpawnParticlesHolderBatch.GetComponent<Renderer>().enabled;		//Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).
		}

		if (_askToggleStreamlinesVisibility) {
			_askToggleStreamlinesVisibility = false;
			StreamlinesHolder.SetActive(!StreamlinesHolder.activeInHierarchy);        //SetActive must be called in the Update() and NOT in OnGUI()
			StreamlinesHolder.GetComponent<Renderer>().enabled = !StreamlinesHolder.GetComponent<Renderer>().enabled;       //Disabling the renderer pauses the vfx too (Disabling the gameObject containing the vfx reset the vfx, and that's not what we want).
		}
	}

	private bool _structureVisibilityKeyIsDown = false;
	private bool _spawnParticlesVisibilityKeyIsDown = false;
	private bool _streamlinesVisibilityKeyIsDown = false;
	private void OnGUI() {
		if (!_structureVisibilityKeyIsDown && Input.GetButtonDown("StructureVisibility")) {
			_structureVisibilityKeyIsDown = true;

			if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) { 
				//Toggle transparency
				ToggleStructureTransparency();
			} else {
				//Ask toggle visibility
				_askToggleStructureVisibility = true;
			}
		} else if (_structureVisibilityKeyIsDown && Input.GetButtonUp("StructureVisibility")) {
			_structureVisibilityKeyIsDown = false;
		}

		if (!_spawnParticlesVisibilityKeyIsDown && Input.GetButtonDown("SpawnParticlesVisibility")) {
			_spawnParticlesVisibilityKeyIsDown = true;
			_askToggleSpawnParticlesVisibility = true;
		} else if (_spawnParticlesVisibilityKeyIsDown && Input.GetButtonUp("SpawnParticlesVisibility")) {
			_spawnParticlesVisibilityKeyIsDown = false;
		}

		if (!_streamlinesVisibilityKeyIsDown && Input.GetButtonDown("StreamlinesVisibility")) {
			_streamlinesVisibilityKeyIsDown = true;
			_askToggleStreamlinesVisibility = true;
		} else if (_streamlinesVisibilityKeyIsDown && Input.GetButtonUp("StreamlinesVisibility")) {
			_streamlinesVisibilityKeyIsDown = false;
		}
	}

	private bool _isStructureTransparent = false;
	private void ToggleStructureTransparency() {
		_isStructureTransparent = !_isStructureTransparent;
		Material material = _isStructureTransparent ? StructureTransparentMaterial : StructureOpaqueMaterial;

		for (int i = 0; i < StructureHolder.transform.childCount; i++) {
			StructureHolder.transform.GetChild(i).gameObject.GetComponent<Renderer>().material = material;
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
}
