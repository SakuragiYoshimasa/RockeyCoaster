using UnityEngine;
using System.Collections;

public class ScoreController : MonoBehaviour {


	public int sumScore;
	public GUIText pointText;

	void Awake(){
		DontDestroyOnLoad (this.gameObject);
		sumScore = 0;
		pointText = this.gameObject.GetComponent<GUIText> ();
	}
	public void GetCoin(){
		sumScore += 100;
	}

	void Update(){
		pointText.text = sumScore.ToString();

	}
}
