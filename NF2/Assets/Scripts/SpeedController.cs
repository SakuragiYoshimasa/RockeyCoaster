using UnityEngine;
using System.Collections;

public class SpeedController : MonoBehaviour {
	private SplineWalker infoSplinewalker;
	private Vector3 befPos;
	private Vector3 aftPos;
	public Vector3 SubPos;
	public float MinSpeed;
	public float MaxSpeed;
	public float Gravity;
	public bool gamestart=false;
	private float i = 0f;
	public GUIText count;

	void Awake(){
		count = GameObject.Find("Count").GetComponent<GUIText>();
	}
	// Use this for initialization
	void Start () {
		infoSplinewalker = this.gameObject.GetComponent<SplineWalker>();
		befPos = gameObject.transform.position;

	}
	// Update is called once per frame
	void Update () {
		if(gamestart){
		aftPos = transform.position;
		SubPos = aftPos - befPos;
		if (SubPos.y>=0) {
			if(infoSplinewalker.Speed>MinSpeed){
				infoSplinewalker.Speed-=Gravity*SubPos.y;
			}else{
				infoSplinewalker.Speed=MinSpeed;
			}
		} else {
			if(infoSplinewalker.Speed<=MaxSpeed){
			 
				infoSplinewalker.Speed-=Gravity*SubPos.y;
			}else{
				infoSplinewalker.Speed=MaxSpeed;
			}
		}
		befPos = aftPos;
		}
	}

	void OnGUI(){
		if(!gamestart){
			infoSplinewalker.Speed=0f;
			if(i<500){
				i+=1;
				Debug.Log(i);
				if(i<100){
					count.text="5";
				}else{
					if(i<200){
						count.text="4";
					}else{
						if(i<300){
							count.text = "3";
						}else{
							if(i<400){
								count.text = "2";
							}else{
								count.text="1";
							}
						}
					}
				}
			}
			if(i>=500){
				Destroy(count);
				gamestart=true;			
			}

		}

	}
}
