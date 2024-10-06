using UnityEngine;

public class MaterialSelector : MonoBehaviour
{
    [SerializeField] private Material[] materials;
    [SerializeField] private MeshRenderer meshRenderer;

    public void Select(int _index)
    {
        if (meshRenderer
            && materials != null
            && _index >= 0
            && _index < materials.Length)
        {
            meshRenderer.material = materials[_index];
        }
    }
}