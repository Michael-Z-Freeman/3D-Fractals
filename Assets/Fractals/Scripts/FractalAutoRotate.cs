using UnityEngine;

/// <summary>
/// Rotates the ray-march proxy transform only while the application is playing.
/// The fractal remains a shader effect; this component merely changes the
/// proxy volume's local-to-world transform.
/// </summary>
public sealed class FractalAutoRotate : MonoBehaviour
{
    [Tooltip("Degrees per second around the local X, Y and Z axes.")]
    public Vector3 degreesPerSecond = new Vector3(8f, 18f, 0f);

    private void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
