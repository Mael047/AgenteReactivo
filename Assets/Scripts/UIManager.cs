using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("HUD - Tiempo real")]
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtPerimetro;
    public TextMeshProUGUI txtArea;
    public TextMeshProUGUI txtContaminacion;

    [Header("Panel resumen")]
    public GameObject panelResumen;
    public TextMeshProUGUI txtResumenTitulo;
    public TextMeshProUGUI txtResumenPerimetro;
    public TextMeshProUGUI txtResumenArea;
    public TextMeshProUGUI txtResumenNivel;
    public TextMeshProUGUI txtResumenObjetos;

    // Contador global de objetos limpiados
    int totalObjetos = 0;

    void Start()
    {
        panelResumen.SetActive(false);
        ActualizarHUD("Explorando", 0f, 0f);
    }

    // Llamado por AgentAI cada frame mientras bordea
    public void ActualizarHUD(string estado, float perimetro, float area)
    {
        txtEstado.text = $"Estado: {estado}";
        txtPerimetro.text = $"Perímetro: {perimetro:F2} u";
        txtArea.text = $"Área: {area:F2} u²";

        string nivel = ClasificarContaminacion(area);
        txtContaminacion.text = $"Contaminación: {nivel}";
        txtContaminacion.color = ColorNivel(nivel);
    }

    // Llamado por AgentAI al terminar de bordear un objeto
    public void MostrarResumen(float perimetro, float area)
    {
        totalObjetos++;
        string nivel = ClasificarContaminacion(area);

        txtResumenTitulo.text = "Objeto identificado";
        txtResumenPerimetro.text = $"Perímetro: {perimetro:F2} u";
        txtResumenArea.text = $"Área estimada: {area:F2} u²";
        txtResumenNivel.text = $"Contaminación: {nivel}";
        txtResumenNivel.color = ColorNivel(nivel);
        txtResumenObjetos.text = $"Total limpiados: {totalObjetos}";

        StartCoroutine(MostrarYOcultar());
    }

    IEnumerator MostrarYOcultar()
    {
        panelResumen.SetActive(true);
        yield return new WaitForSeconds(5f);
        panelResumen.SetActive(false);
    }

    string ClasificarContaminacion(float area)
    {
        if (area < 1f) return "Bajo";
        else if (area < 5f) return "Medio";
        else return "Alto";
    }

    Color ColorNivel(string nivel)
    {
        return nivel switch
        {
            "Alto" => new Color(0.9f, 0.2f, 0.2f),
            "Medio" => new Color(0.95f, 0.6f, 0.1f),
            _ => new Color(0.2f, 0.8f, 0.3f)
        };
    }
}