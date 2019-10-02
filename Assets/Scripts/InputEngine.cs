//////////////////////////////////////////////////////////////////
/// Copyright Frank Verheyen, lokiofmute, 2019
/// 
/// This code is released under LGPL license.
/// Which in short means: do with it whatever you want.
/// If you use it, it would be so kind to list
/// me in the credits of your program.
//////////////////////////////////////////////////////////////////

using UnityEngine;

public class InputEngine : MonoBehaviour
{
    #region Data Members
    private bool _wasMouseDown;
    private bool _wasPanning;
    private Line _currentLine;
    private Vector2 _previousPos;
    #endregion

    #region Properties
    public Camera Camera;
    public float LineSampleDistance = 3;
    public Transform LinesParent;
    public GameObject LinePrefab;
    public int SplineSubdivisionCount = 3;
    public float LineWidth = 0.1f;
    public float PositionBlend = 0.5f;
    #endregion

    private void Awake()
    {
        Debug.Assert(LinesParent != null);
        Debug.Assert(LinePrefab != null);
        Camera = Camera ?? Camera.current ?? FindObjectOfType<Camera>();
        Debug.Assert(Camera != null);
    }

    private void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0);
        bool isPanning = Input.GetKey(KeyCode.Space);
        var pos = Input.mousePosition;
        if (Input.touchCount > 0 && Input.touches.Length > 0)
        {
            pos = Input.touches[0].position;
            isMouseDown = true;
            if (Input.touchCount > 2)
                isPanning = true;
        }
        pos.z = Camera.nearClipPlane;
        pos = Camera.ScreenToWorldPoint(pos);
        pos.z = 0;

        // when panning for the first time, reset the previous position
        if (!_wasPanning && isPanning)
            _previousPos = pos;

        if (isMouseDown)
        {
            if (isPanning)
            {
                var delta = pos - new Vector3(_previousPos.x, _previousPos.y);
                LinesParent.Translate(delta);
            }
            else
            {
                if (_wasMouseDown)
                {
                    var sqrDistance = (pos - _currentLine.OneButLastPoint).sqrMagnitude;
                    if (sqrDistance > LineSampleDistance * LineSampleDistance)
                    {
                        // add to the current line
                        _currentLine.Vertices.Add(Vector3.Lerp(_currentLine.LastPoint, pos, PositionBlend));
                        _currentLine.Make();
                    }
                    else
                    {
                        // move the last point of the current line
                        _currentLine.LastPoint = pos;
                        _currentLine.Make();
                    }
                }
                else
                {
                    // start a new line
                    var go = Instantiate(LinePrefab);
                    go.transform.parent = LinesParent;
                    _currentLine = go.GetComponent<Line>();
                    _currentLine.IsLiveUpdate = false;
                    _currentLine.LineWidth = LineWidth;
                    _currentLine.SplineSubdivisionCount = SplineSubdivisionCount;
                    _currentLine.Vertices.Add(pos);
                }
            }
        }
        else
        {
            _currentLine = null;
            _wasPanning = false;
        }

        _previousPos = pos;
        _wasMouseDown = isMouseDown;
        _wasPanning = isPanning;
    }

    /// <summary>
    /// Clear all lines
    /// </summary>
    public void Clear()
    {
        var len = LinesParent.childCount;
        for(int t = 0; t < len; ++t)
            Destroy(LinesParent.GetChild(t).gameObject);
    }
}
