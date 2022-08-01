using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour {
	
    static int baseColorId = Shader.PropertyToID("_BaseColor");
	
    [SerializeField]
    Color baseColor = Color.white;
}