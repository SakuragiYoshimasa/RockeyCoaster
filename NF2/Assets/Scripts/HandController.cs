
using UnityEngine;
using System.Collections;

public class HandController : MonoBehaviour {


	public ScoreController score;
	public GameObject getParticle;
	public GameObject getLastParticle;
	void Awake(){
		score = GameObject.FindWithTag("Score").GetComponent<ScoreController>();

	}

	void OnCollisionEnter(Collision other){
		Debug.Log (other.gameObject.tag);
	if (other.gameObject.tag == "Coin") {
			Instantiate(getParticle,other.gameObject.transform.position,other.gameObject.transform.rotation);
			Destroy (other.gameObject);
			score.GetCoin ();
			}
		if (other.gameObject.tag == "LastCoin") {
			Destroy(other.gameObject);
			Instantiate(getLastParticle,other.gameObject.transform.position,other.gameObject.transform.rotation);
			score.GetCoin();
			GUIText ScController= GameObject.Find("Score").GetComponent<GUIText>();
			Destroy(ScController,0.5f);
			StartCoroutine(nextlevel());

		}

	}
	IEnumerator nextlevel(){
		yield return new WaitForSeconds(1.5f);
		Application.LoadLevel("Ending");}
}
