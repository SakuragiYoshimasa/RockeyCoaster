using UnityEngine;
using System.Collections;

public class EndingController : MonoBehaviour {

	private GameObject score;
	public GUIText scoreText;
	void Awake(){
		score = GameObject.Find ("Score");
		scoreText = GameObject.Find ("ScoreText").GetComponent<GUIText>();;
	}
	// Use this for initialization
	void Start () {
	
		scoreText.text = score.GetComponent<ScoreController>().sumScore.ToString();
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)){
			Destroy(score);
			Application.LoadLevel("Start");

		}
	}
}
