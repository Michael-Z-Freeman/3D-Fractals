using UnityEngine;

/// <summary>
/// A small geometry prototype based on the Menger parameters exposed by the
/// Unreal MI_Mecha UberMenger/Structura materials. This deliberately uses
/// instanced cubes as a dependable first visual target before porting the
/// ray-marched distance estimator to HLSL.
/// </summary>
[ExecuteAlways]
public sealed class MengerSponge : MonoBehaviour
{
    [Range(0, 3)] public int iterations = 2;
    [Min(0.1f)] public float size = 6f;
    public Color baseColor = new Color(0.04f, 0.32f, 0.60f, 1f);
    [ColorUsage(true, true)] public Color emissionColor = new Color(0.0f, 0.9f, 1.6f, 1f);
    public bool rotate = true;
    [Min(0f)] public float rotationDegreesPerSecond = 12f;

    private Material material;
    private bool rebuilding;

    private void OnEnable() => Rebuild();
    private void OnValidate() => Rebuild();

    [ContextMenu("Rebuild Menger Sponge")]
    public void Rebuild()
    {
        // Creating children fires OnValidate in edit mode; ignore the re-entrant call.
        if (rebuilding) return;
        rebuilding = true;
        try
        {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = "Menger Prototype Material" };
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", emissionColor);

        Build(Vector3.zero, size, iterations);
        }
        finally
        {
            rebuilding = false;
        }
    }

    private void Build(Vector3 center, float currentSize, int level)
    {
        if (level == 0)
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.name = "Menger Cell";
            cell.transform.SetParent(transform, false);
            cell.transform.localPosition = center;
            cell.transform.localScale = Vector3.one * currentSize * 0.96f;
            cell.GetComponent<Renderer>().sharedMaterial = material;
            DestroyCollider(cell);
            return;
        }

        float childSize = currentSize / 3f;
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            int axesAtCenter = (x == 0 ? 1 : 0) + (y == 0 ? 1 : 0) + (z == 0 ? 1 : 0);
            if (axesAtCenter >= 2) continue; // Menger: remove face-centres and centre.
            Build(center + new Vector3(x, y, z) * childSize, childSize, level - 1);
        }
    }

    private static void DestroyCollider(GameObject cell)
    {
        Collider collider = cell.GetComponent<Collider>();
        if (collider == null) return;
        if (Application.isPlaying) Destroy(collider);
        else DestroyImmediate(collider);
    }

    private void Update()
    {
        if (rotate && Application.isPlaying)
            transform.Rotate(12f, rotationDegreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
