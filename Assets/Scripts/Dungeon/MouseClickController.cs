using UnityEngine;
using UnityEngine.Events;

public class MouseClickController : MonoBehaviour
{
    public Vector3 clickPosition;
    private Ray _debugRay;
    private float _debugRayDistance;
    
    public UnityEvent<Vector3> OnClick;
    // Update is called once per frame
    void Update()
    {
        // Get the mouse click position in world space
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay( Input.mousePosition );
            if (Physics.Raycast( mouseRay, out RaycastHit hitInfo ))
            {
                Vector3 clickWorldPosition = hitInfo.point;
                clickPosition = clickWorldPosition;

                _debugRay = mouseRay;
                _debugRayDistance = hitInfo.distance;

                OnClick.Invoke(clickPosition);
            }
        }
        
        DebugExtension.DebugWireSphere(clickPosition, Color.blue, .5f);
        Debug.DrawRay(_debugRay.origin, _debugRay.direction * _debugRayDistance, Color.yellow);
    }
}
