using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    public PlayerController player;
        
    // [SerializeField]
    // public Shader shader;

    public Vector3 position { get => transform.position; private set => transform.position = value; }
    public Vector3 direction { get => transform.forward; private set => transform.forward = value; } // 2d but Vector3 for ez maths

    float distance = 10.0f;

    private void Awake() {
        // var camera = GetComponent<Camera>();
        // camera.RenderWithShader(shader, "");
        // camera.SetReplacementShader(shader, "");
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        var mouseX = Input.GetAxis("Mouse X");
        var mouseY = Input.GetAxis("Mouse Y");

        transform.RotateAround(player.position, Vector3.up, mouseX);
        transform.RotateAround(player.position, new Vector3(-direction.z, 0, direction.x), mouseY);

        position = player.position - (direction * distance);
    }
}
