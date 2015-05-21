using UnityEngine;
using System.Collections;

public class StartMenu :MonoBehaviour{

	void Update(){

		if(Input.GetMouseButtonDown(0)){
			Debug.Log("as");
			Application.LoadLevel("demo");
		}
	}
}
