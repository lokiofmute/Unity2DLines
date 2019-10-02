//////////////////////////////////////////////////////////////////
/// Copyright Frank Verheyen, lokiofmute, 2019
/// 
/// This code is released under LGPL license.
/// Which in short means: do with it whatever you want.
/// If you use it, it would be so kind to list
/// me in the credits of your program.
//////////////////////////////////////////////////////////////////

using UnityEngine;

/// <summary>
/// Just a little class to enable switching the InputEngine's line prefab to polylines or splines,
/// for mere demonstration purposes
/// </summary>
public class SwitchInputEngineLineType : MonoBehaviour
{
    #region Properties
    public InputEngine InputEngine;

    public Material PolylineMaterial;
    public Material SplineMaterial;
    #endregion

    #region Private properties
    private Line LinePrefab => InputEngine?.LinePrefab?.GetComponent<Line>();
    private MeshRenderer LineMeshRenderer => InputEngine?.LinePrefab?.GetComponent<MeshRenderer>();
    #endregion

    private void Awake()
    {
        InputEngine = InputEngine ?? FindObjectOfType<InputEngine>();
        Debug.Assert(InputEngine != null);
        Debug.Assert(InputEngine.LinePrefab != null);
        Debug.Assert(PolylineMaterial != null);
        Debug.Assert(SplineMaterial != null);
    }

    /// <summary>
    /// set the InputEngine's LinePrefab parameters
    /// </summary>
    /// <param name="lineType"></param>
    /// <param name="material"></param>
    private void SetLineParameters(LineType lineType, Material material)
    {
        var linePrefab = LinePrefab;
        if (linePrefab != null)
            linePrefab.LineType = lineType;

        var lineMeshRenderer = LineMeshRenderer;
        if (lineMeshRenderer != null && material != null)
            lineMeshRenderer.material = material;
    }

    /// <summary>
    /// Choose polylines on the InputEngine
    /// </summary>
    public void ChoosePolylines() => SetLineParameters(LineType.Polyline, PolylineMaterial);

    /// <summary>
    /// Choose splines on the InputEngine
    /// </summary>
    public void ChooseSplines() => SetLineParameters(LineType.CatmulRomSpline, SplineMaterial);

    /// <summary>
    /// Clear the InputEngine's collection of lines
    /// </summary>
    public void ClearLines() => InputEngine?.Clear();
}
