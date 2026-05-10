using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Prefabs de basura")]
    public GameObject[] trashPrefabs; // arrastra tus 3 prefabs aquí

    [Header("Rango de escala aleatoria")]
    public float minScale = 0.5f;
    public float maxScale = 2f;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SpawnTrash();
        }
    }

    void SpawnTrash()
    {
        // Convierte el click a posición en el mundo
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // Elige un prefab al azar entre los 3
        int index = Random.Range(0, trashPrefabs.Length);
        GameObject prefab = trashPrefabs[index];

        // Escala aleatoria uniforme
        float scale = Random.Range(minScale, maxScale);

        GameObject trash = Instantiate(prefab, mousePos, Quaternion.identity);
        trash.transform.localScale = Vector3.one * scale;

        // Rotación aleatoria para que no salgan todos iguales
        trash.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }
}