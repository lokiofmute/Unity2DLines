//////////////////////////////////////////////////////////////////
/// Copyright Frank Verheyen, lokiofmute, 2019
/// 
/// This code is released under LGPL license.
/// Which in short means: do with it whatever you want.
/// If you use it, it would be so kind to list
/// me in the credits of your program.
//////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The line types we support
/// </summary>
public enum LineType
{
    Polyline,
    CatmulRomSpline,
}

[RequireComponent(typeof(MeshRenderer)),
 RequireComponent(typeof(MeshFilter)),
 ExecuteInEditMode]
public class Line : MonoBehaviour
{
    #region Data Members
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<int> _triangles = new List<int>();
    private readonly List<Vector3> _normals = new List<Vector3>();
    private List<Vector3> _splineVertices = new List<Vector3>();
    private int _lastHashCode;
    #endregion

    #region Properties
    /// <summary>
    /// Should the line geometry update automatically if some of its parameters change?
    /// </summary>
    public bool IsLiveUpdate = true;

    /// <summary>
    /// The kind of line
    /// </summary>
    public LineType LineType = LineType.CatmulRomSpline;

    /// <summary>
    /// If the line type is a spline, this is the subdivision amount
    /// </summary>
    [Range(1, 100)]
    public int SplineSubdivisionCount = 10;

    /// <summary>
    /// The with of the line geometry
    /// </summary>
    [Range(0.01f, 0.5f)]
    public float LineWidth = 0.2f;

    /// <summary>
    /// The list of vertices.
    /// note: if the line is a spline, these are the controlpoints
    /// </summary>
    public List<Vector3> Vertices { get; } = new List<Vector3>();

    /// <summary>
    /// Get the number of total points in the line
    /// note: if the line is a spline, this is the controlpoints + all subdivision points
    /// </summary>
    public int TotalPointCount
    {
        get
        {
            switch (LineType)
            {
                case LineType.Polyline:
                    return Vertices.Count;
                case LineType.CatmulRomSpline:
                    return (Vertices.Count - 1) * SplineSubdivisionCount;
                default:
                    return 0;
            }
        }
    }

    /// <summary>
    /// Get access to the line points
    /// note: if the line is a spline, this accesses all subdivided spline points
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Vector3 this[int index]
    {
        get
        {
            switch (LineType)
            {
                case LineType.Polyline:
                    return Vertices[index];
                case LineType.CatmulRomSpline:
                    // if the line needs updating, generate the cached spline points now
                    var len = Vertices.Count;
                    var splineVerticesCount = (len - 1) * SplineSubdivisionCount;
                    if (_splineVertices.Count != splineVerticesCount)
                    {
                        _splineVertices.Clear();
                        if (_splineVertices.Capacity < splineVerticesCount)
                            _splineVertices.Capacity = splineVerticesCount;
                        --len;
                        for (int t = -1; t < len; ++t)
                        {
                            var p0 = Vertices[Mathf.Clamp(t, 0, len)];
                            var p1 = Vertices[Mathf.Clamp(t + 1, 0, len)];
                            var p2 = Vertices[Mathf.Clamp(t + 2, 0, len)];
                            var p3 = Vertices[Mathf.Clamp(t + 3, 0, len)];
                            for (int i = 0; i < SplineSubdivisionCount; ++i)
                            {
                                _splineVertices.Add(GetCatmullRomPosition((float)i / SplineSubdivisionCount, p0, p1, p2, p3));
                            }
                        }
                    }
                    return _splineVertices[index];
                default:
                    return Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Get access to the last point of the line.
    /// </summary>
    public Vector3 LastPoint
    {
        get
        {
            return Vertices.Count <= 0 ? Vector3.zero : Vertices[Vertices.Count - 1];
        }
        set
        {
            if (Vertices.Count <= 0)
                return;
            Vertices[Vertices.Count - 1] = value;
            if (!IsLiveUpdate)
                Make();
        }
    }

    /// <summary>
    /// Get access to the one-but-last point of the line, if enough lines are available
    /// </summary>
    public Vector3 OneButLastPoint
    {
        get
        {
            return Vertices.Count <= 1 ? Vector3.zero : Vertices[Vertices.Count - 2];
        }
        set
        {
            if (Vertices.Count <= 1)
                return;
            Vertices[Vertices.Count - 2] = value;
            if (!IsLiveUpdate)
                Make();
        }
    }
    #endregion


    #region Helper Methods
    /// <summary>
    /// Snapshot the most important parameters of the line into a hash number, so we can easily
    /// see if there are any changes to the line, in comparison to a previous snapshot
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        var result = 17;
        var len = Vertices.Count;
        for (int t = 0; t < len; ++t)
            result = (result * 13) + Vertices[t].GetHashCode();
        return result +
                11 * LineWidth.GetHashCode() +
                13 * SplineSubdivisionCount.GetHashCode() +
                17 * LineType.GetHashCode();
    }

    /// <summary>
    /// Get a catmull-rom subdivision point from controlpoints
    /// </summary>
    /// <param name="t"></param>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <returns></returns>
    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        //The cubic polynomial: a + b * t + c * t^2 + d * t^3
        return 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
    }
    #endregion

    /// <summary>
    /// On first awake, make the line from any preloaded vertices
    /// </summary>
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        Debug.Assert(_meshFilter != null);

        _meshRenderer = GetComponent<MeshRenderer>();
        Debug.Assert(_meshRenderer != null);

        // make the mesh component if not available
        if (_meshFilter.sharedMesh == null)
            _meshFilter.sharedMesh = _mesh = new Mesh();

        Make();
    }

    /// <summary>
    /// Make the line with the current parameters (linewidt, vertices..)
    /// </summary>
    public void Make()
    {
        _mesh.Clear();
        _vertices.Clear();
        _splineVertices.Clear();

        // local function to add vertices at both sides of the point
        void AddVertices(Vector3 point, Vector3 delta)
        {
            var ortho = new Vector3(-delta.y * LineWidth, delta.x * LineWidth);
            _vertices.Add(point + ortho);
            _vertices.Add(point - ortho);
        }

        // build the vertices
        var len = TotalPointCount;
        if (len > 1)
        {
            if(_vertices.Capacity < len << 1)
                _vertices.Capacity = len << 1;
            var prev = this[0];
            var delta = Vector3.zero;
            for (int t = 1; t < len; ++t)
            {
                var curr = this[t];
                delta = (delta + (curr - prev).normalized).normalized;
                AddVertices(prev, delta);
                prev = curr;
            }
            // add the last point too
            if (len > 1)
                AddVertices(prev, (prev - this[len - 2]).normalized);

            // build the triangles
            _triangles.Clear();
            if (_vertices.Count > 2)
            {
                _triangles.Capacity = len * 3;
                for (int t = 0; t < len - 1; ++t)
                {
                    var tt = t * 2;
                    if (tt + 3 >= _vertices.Count)
                        break;
                    _triangles.Add(tt);
                    _triangles.Add(tt + 2);
                    _triangles.Add(tt + 1);

                    _triangles.Add(tt + 1);
                    _triangles.Add(tt + 2);
                    _triangles.Add(tt + 3);
                }
            }

            _normals.Clear();
            if (_normals.Capacity < _vertices.Count)
                _normals.Capacity = _vertices.Count;
            var normal = new Vector3(0, 0, -1);
            for (int t = 0; t < _vertices.Count; ++t)
                _normals.Add(normal);
        }

        // stuff it into the mesh
        _mesh.vertices = _vertices.ToArray();
        _mesh.normals = _normals.ToArray();
        _mesh.triangles = _triangles.ToArray();

        // record the hashcode
        if (IsLiveUpdate)
            _lastHashCode = GetHashCode();
    }

    private void Update()
    {
        // if live update is ON, and something changed in the line's parameters,
        // rebuild the line geometry automatically.
        if (IsLiveUpdate && _lastHashCode != GetHashCode())
            Make();
    }
}
