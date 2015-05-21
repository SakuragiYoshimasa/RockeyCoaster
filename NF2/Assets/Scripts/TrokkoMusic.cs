using UnityEngine;
using System.Collections;

public class TrokkoMusic : MonoBehaviour {


	private AudioSource Music;
	private SplineWalker infoSpline;
	private GameObject splineWalker;
	public float offset;
	public float musicspeed;

	void Awake(){
		splineWalker = GameObject.Find ("t_Camera");
		infoSpline = splineWalker.GetComponent<SplineWalker> ();
		Music = this.gameObject.GetComponent<AudioSource>();
	}

	// Update is called once per frame
	void Update () {
	
		Music.pitch = infoSpline.Speed / 40+offset;
	}
}
