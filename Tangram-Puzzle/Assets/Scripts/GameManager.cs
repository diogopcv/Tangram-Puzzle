using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager _Instance; // Singleton reference

    [SerializeField]
    private UnityEngine.EventSystems.EventSystem eventSystem; // EventSytem reference

    [SerializeField]
    GameObject menuPanel; // Menu panel reference

    void Awake()
    {
        if (_Instance == null)
        {
            _Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Start a new Tangram round
    /// </summary>
    /// <param name="tangramName">Name of tangram shape</param>
    public void StarTangramSection(string tangramName)
    {
        menuPanel.SetActive(false);
        TangramManager._Instance.StartTangram(tangramName);
    }

    /// <summary>
    /// Finish a tangram round
    /// </summary>
    public void FinishTangramSection()
    {
        eventSystem.enabled = false;
        StartCoroutine("FinishSection");
    }

    /// <summary>
    /// Coroutine to finish tangram round
    /// </summary>
    /// <returns></returns>
    IEnumerator FinishSection()
    {
        eventSystem.enabled = false;
        yield return new WaitForSeconds(2f);
        eventSystem.enabled = true;
        menuPanel.SetActive(true);
    }
}
