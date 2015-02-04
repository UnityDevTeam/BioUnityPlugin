using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class MouseOrbit : MonoBehaviour
{
    public Vector3 Target;

    public float XSpeed = 0.01f;
    public float YSpeed = 0.01f;

    public float YMinLimit = -90f;
    public float YMaxLimit = 90f;

    const float DefaultDistance = 5.0f;

    [HideInInspector]
    public float X = 0.0f;

    [HideInInspector]
    public float Y = 0.0f;

    [Range(1, 200)]
    public float Distance = DefaultDistance;

    void OnGUI()
    {
        if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
        {
            EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be display
        }
        else if (Event.current.type == EventType.mouseDrag && Event.current.button == 1)
        {
            X += Event.current.delta.x * 0.2f;
            Y += Event.current.delta.y * 0.2f;
        }
        else if (Event.current.type == EventType.mouseDrag && Event.current.button == 2)
        {
            Target += gameObject.transform.up * Event.current.delta.y * 0.01f;
            Target -= gameObject.transform.right * Event.current.delta.x * 0.01f;
        }
        else if (Event.current.type == EventType.ScrollWheel)
        {
            Distance -= Event.current.delta.y * 0.2f;
        }
    }

    void Update()
    {
        //if (Input.GetMouseButton(2))
        //{
        //    _x += Input.GetAxis("Mouse X") * XSpeed * 0.02f;
        //    _y -= Input.GetAxis("Mouse Y") * YSpeed * 0.02f;
        //    _y = ClampAngle(_y, YMinLimit, YMaxLimit);
        //}

        //if (Input.GetMouseButton(2))
        //{
        //    target -= gameObject.transform.up * Input.GetAxis("Mouse Y") * 0.25f;
        //    target -= gameObject.transform.right * Input.GetAxis("Mouse X") * 0.25f;
        //}

        //float scale = 20;

        //if (Input.GetKey(KeyCode.W))
        //{
        //    target += gameObject.transform.forward * Time.deltaTime * scale;
        //}

        //if (Input.GetKey(KeyCode.A))
        //{
        //    target -= gameObject.transform.right * Time.deltaTime * scale;
        //}

        //if (Input.GetKey(KeyCode.D))
        //{
        //    target += gameObject.transform.right * Time.deltaTime * scale;
        //}

        //if (Input.GetKey(KeyCode.S))
        //{
        //    target -= gameObject.transform.forward * Time.deltaTime * scale;
        //}

        //if (Input.GetKey(KeyCode.F))
        //{
        //    distance = DefaultDistance;
        //}

        //if (Input.GetAxis("Mouse ScrollWheel") > 0.0f) // forward
        //{
        //    distance += 0.5f;
        //}
        //if (Input.GetAxis("Mouse ScrollWheel") < 0.0f) // back
        //{
        //    distance -= 0.5f;
        //}

        var rotation = Quaternion.Euler(Y, X, 0.0f);
        var position = rotation * new Vector3(0.0f, 0.0f, -Distance) + Target;

        transform.rotation = rotation;
        transform.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360.0f)
            angle += 360.0f;

        if (angle > 360.0f)
            angle -= 360.0f;

        return Mathf.Clamp(angle, min, max);
    }
}
