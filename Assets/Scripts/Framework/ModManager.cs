using UnityEngine;

public class ModManager : MonoBehaviour {
    [SerializeField] private string _modsFolder = "Mods";

	void Start () {
	    string modsPath = Application.dataPath + "/" + _modsFolder;
        Debug.Log(modsPath);


	}
	
	
}
