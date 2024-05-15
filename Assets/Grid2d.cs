using UnityEngine;

public class GridCreator : MonoBehaviour
{
    public int rows = 5;
    public int columns = 5;
    public float cubeSize = 1.0f;  // Size of each cube
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;  // Automatically assign the main camera if not set
            if (mainCamera == null)
            {
                Debug.LogError("No main camera found in the scene. Please assign a main camera.");
                return;
            }
        }

        CreateGrid();
        SetupPixelPerfectCamera();
    }

    void CreateGrid()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(j * cubeSize, i * cubeSize, 0);
                cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                //cubeRenderer.material.color = Random.ColorHSV();
            }
        }
    }

    void SetupPixelPerfectCamera()
    {
        mainCamera.orthographic = true;
        float gridWidth = columns * cubeSize;
        float gridHeight = rows * cubeSize;
        float screenAspectRatio = (float)Screen.width / Screen.height;
        float gridAspectRatio = gridWidth / gridHeight;

        if (gridAspectRatio > screenAspectRatio)
        {
            // Grid is wider than screen
            mainCamera.orthographicSize = gridWidth / screenAspectRatio / 2;
        }
        else
        {
            // Grid is taller or equal to screen height
            mainCamera.orthographicSize = gridHeight / 2;
        }

        mainCamera.transform.position = new Vector3(gridWidth / 2f - .5f, gridHeight / 2f - .5f, -10);
        mainCamera.transform.LookAt(new Vector3(gridWidth / 2f - .5f, gridHeight / 2f - .5f, 0));
    }
}
