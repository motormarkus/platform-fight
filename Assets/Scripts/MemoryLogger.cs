using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>
/// Kirjaa pelin aikana muistinkäytön lokiin (Editor.log / Player.log): tekstuurit, kokonaismuisti, näytönohjaimen muisti ja nykyinen alue.
/// Vain editorissa ja kehitysbuildeissa. Lokirivit alkavat tekstillä [MUISTI], joten ne löytää helposti.
/// </summary>
public class MemoryLogger : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    const float Every = 15f;
    float next;
    string lastArea = "";
    float peakTex, peakTotal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindFirstObjectByType<MemoryLogger>() != null) return;
        var go = new GameObject("Muistiloki");
        DontDestroyOnLoad(go);
        go.AddComponent<MemoryLogger>();
        Debug.Log("[MUISTI] Näytönohjain: " + SystemInfo.graphicsDeviceName + ", VRAM " + SystemInfo.graphicsMemorySize + " MB, RAM " + SystemInfo.systemMemorySize + " MB");
    }

    void Update()
    {
        string area = Area.Current != null ? Area.Current.areaName : "-";
        float tex = Texture.currentTextureMemory / 1048576f;
        float total = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
        peakTex = Mathf.Max(peakTex, tex); peakTotal = Mathf.Max(peakTotal, total);
        if (area != lastArea || Time.unscaledTime >= next)
        {
            next = Time.unscaledTime + Every; lastArea = area;
            Debug.Log("[MUISTI] alue=" + area + " scenet=" + SceneManager.sceneCount + " tekstuurit=" + tex.ToString("0") + " MB (huippu " + peakTex.ToString("0") + ")  kokonais(Unity)=" + total.ToString("0") + " MB (huippu " + peakTotal.ToString("0") + ")");
        }
    }
#endif
}
