/*
 * Kinect2ModelControllerV2.cs - Handles rotating the bones of a model to match 
 * 			rotations derived from the bone positions given by the kinect
 * 
 * 		Developed by Peter Kinney -- 6/30/2011
 *  	Modified by ほえたん -- 8/10/2014
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class Kinect2ModelControllerV2 : MonoBehaviour {
	
	//Assignments for a bitmask to control which bones to look at and which to ignore
	public enum BoneMask
	{
		None = 0x0,
		//EMPTY = 0x1,
		Spine = 0x2,
		Shoulder_Center = 0x4,
		Head = 0x8,
		Shoulder_Left = 0x10,
		Elbow_Left = 0x20,
		Wrist_Left = 0x40,
		Hand_Left = 0x80,
		Shoulder_Right = 0x100,
		Elbow_Right = 0x200,
		Wrist_Right = 0x400,
		Hand_Right = 0x800,
		Hips = 0x1000,
		Knee_Left = 0x2000,
		Ankle_Left = 0x4000,
		Foot_Left = 0x8000,
		//EMPTY = 0x10000,
		Knee_Right = 0x20000,
		Ankle_Right = 0x40000,
		Foot_Right = 0x80000,
		All = 0xEFFFE,
		Torso = 0x1000000 | Spine | Shoulder_Center | Head, //the leading bit is used to force the ordering in the editor
		Left_Arm = 0x1000000 | Shoulder_Left | Elbow_Left | Wrist_Left | Hand_Left,
		Right_Arm = 0x1000000 |  Shoulder_Right | Elbow_Right | Wrist_Right | Hand_Right,
		Left_Leg = 0x1000000 | Hips | Knee_Left | Ankle_Left | Foot_Left,
		Right_Leg = 0x1000000 | Hips | Knee_Right | Ankle_Right | Foot_Right,
		R_Arm_Chest = Right_Arm | Spine,
		No_Feet = All & ~(Foot_Left | Foot_Right),
		Upper_Body = Head |Elbow_Left | Wrist_Left | Hand_Left | Elbow_Right | Wrist_Right | Hand_Right
	}
	
	public GameObject BodySourceManager;
	private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
	private BodySourceManager _BodyManager;
	
	public GameObject Root;
	public GameObject SpineBase;
	public GameObject SpineMid;
	public GameObject Neck;
	public GameObject Head;
	public GameObject CollarLeft;
	public GameObject ShoulderLeft;
	public GameObject ElbowLeft;
	public GameObject WristLeft;
	public GameObject HandLeft;
	public GameObject CollarRight;
	public GameObject ShoulderRight;
	public GameObject ElbowRight;
	public GameObject WristRight;
	public GameObject HandRight;
	public GameObject HipOverride;
	public GameObject HipLeft;
	public GameObject KneeLeft;
	public GameObject AnkleLeft;
	public GameObject FootLeft;
	public GameObject HipRight;
	public GameObject KneeRight;
	public GameObject AnkleRight;
	public GameObject FootRight;
	public GameObject SpineShoulder;
	public GameObject HandTipLeft;
	public GameObject ThumbLeft;
	public GameObject HandTipRight;
	public GameObject ThumbRight;
	
	public int player;
	public BoneMask Mask = BoneMask.All;
	public bool animated;
	public float blendWeight = 1;
	public float fRotRootX = 15.0f;
	
	private GameObject[] _bones; //internal handle for the bones of the model
	private uint _nullMask = 0x0;
	
	private Quaternion[] _baseRotation; //starting orientation of the joints
	private Vector3[] _boneDir; //in the bone's local space, the direction of the bones
	private Vector3[] _boneUp; //in the bone's local space, the up vector of the bone
	private Vector3 _hipRight; //right vector of the hips
	private Vector3 _chestRight; //right vectory of the chest
	
	
	// Use this for initialization
	void Start () {
		//store bones in a list for easier access, everything except Hip_Center will be one
		//higher than the corresponding Kinect.NuiSkeletonPositionIndex (because of the hip_override)
		_bones = new GameObject[(int)Kinect.JointType.ThumbRight + 1] {
			null, SpineMid, SpineShoulder, Neck,
			CollarLeft, ShoulderLeft, ElbowLeft, WristLeft,
			CollarRight, ShoulderRight, ElbowRight, WristRight,
			HipOverride, HipLeft, KneeLeft, AnkleLeft,
			null, HipRight, KneeRight, AnkleRight,
			Head, HandLeft, HandRight, FootLeft, FootRight};
		//SpineShoulder, HandTipLeft, ThumbLeft, HandTipRight, ThumbRight, FootLeft, FootRight};
		
		//determine which bones are not available
		for(int ii = 0; ii < _bones.Length; ii++)
		{
			if(_bones[ii] == null)
			{
				_nullMask |= (uint)(1 << ii);
			}
		}
		
		//store the base rotations and bone directions (in bone-local space)
		_baseRotation = new Quaternion[(int)Kinect.JointType.ThumbRight + 1];
		_boneDir = new Vector3[(int)Kinect.JointType.ThumbRight + 1];
		
		//first save the special rotations for the hip and spine
		_hipRight = HipRight.transform.position - HipLeft.transform.position;
		_hipRight = HipOverride.transform.InverseTransformDirection(_hipRight);
		
		_chestRight = ShoulderRight.transform.position - ShoulderLeft.transform.position;
		_chestRight = SpineMid.transform.InverseTransformDirection(_chestRight);
		
		//get direction of all other bones
		for( int ii = 0; ii < (int)Kinect.JointType.ThumbRight- 4; ii++)
		{
			if((_nullMask & (uint)(1 << ii)) <= 0)
			{
				//save initial rotation
				_baseRotation[ii] = _bones[ii].transform.localRotation;
				//if the bone is the end of a limb, get direction from this bone to one of the extras (hand or foot).
				if(ii % 4 == 3 && ((_nullMask & (uint)(1 << (ii/4) + (int)Kinect.JointType.ThumbRight - 4)) <= 0))
				{
					_boneDir[ii] = _bones[(ii/4) + (int)Kinect.JointType.ThumbRight - 4].transform.position - _bones[ii].transform.position;
				}
				//if the bone is the hip_override (at boneindex Hip_Left, get direction from average of left and right hips
				else if(ii == (int)Kinect.JointType.HipLeft && HipLeft != null && HipRight != null)
				{
					_boneDir[ii] = ((HipRight.transform.position + HipLeft.transform.position) / 2.0f) - HipOverride.transform.position;
				}
				//otherwise, get the vector from this bone to the next.
				else if((_nullMask & (uint)(1 << ii+1)) <= 0)
				{
					_boneDir[ii] = _bones[ii+1].transform.position - _bones[ii].transform.position;
				}
				else
				{
					continue;
				}
				//Since the spine of the kinect data is ~40 degrees back from the hip,
				//check what angle the spine is at and rotate the saved direction back to match the data
				if(ii == (int)Kinect.JointType.SpineMid)
				{
					float angle = Vector3.Angle(transform.up,_boneDir[ii]);
					_boneDir[ii] = Quaternion.AngleAxis(angle,transform.right) * _boneDir[ii];
				}
				//transform the direction into local space.
				_boneDir[ii] = _bones[ii].transform.InverseTransformDirection(_boneDir[ii]);
			}
		}
		//make _chestRight orthogonal to the direction of the spine.
		_chestRight -= Vector3.Project(_chestRight, _boneDir[(int)Kinect.JointType.SpineMid]);
		//make _hipRight orthogonal to the direction of the hip override
		Vector3.OrthoNormalize(ref _boneDir[(int)Kinect.JointType.HipLeft],ref _hipRight);
		// Root
		Root.transform.localRotation = Quaternion.Euler(fRotRootX, 0.0f, 0.0f);
	}
	
	void Update () {
		//update the data from the kinect if necessary
		if (BodySourceManager == null)
		{
			return;
		}
		
		_BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
		if (_BodyManager == null)
		{
			return;
		}
		
		Kinect.Body[] data = _BodyManager.GetData();
		if (data == null)
		{
			return;
		}
		
		List<ulong> trackedIds = new List<ulong>();
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{
				trackedIds.Add (body.TrackingId);
			}
		}
		/*
		List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
		
		// First delete untracked bodies
		foreach(ulong trackingId in knownIds)
		{
			if(!trackedIds.Contains(trackingId))
			{
				Destroy(_Bodies[trackingId]);
				_Bodies.Remove(trackingId);
			}
		}
		*/
		foreach(var body in data)
		{
			if (body == null)
			{
				continue;
			}
			
			if(body.IsTracked)
			{				
				if(!_Bodies.ContainsKey(body.TrackingId))
				{
					//_Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
				}
				
				for( int ii = 0; ii < (int)Kinect.JointType.ThumbRight - 4; ii++)
				{
					if( ((uint)Mask & (uint)(1 << ii) ) > 0 && (_nullMask & (uint)(1 << ii)) <= 0 )
					{
						RotateJoint(body, ii);
					}
				}
			}
		}
	}
	
	private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
	{
		return new Vector3(joint.Position.X, joint.Position.Y + 1.0f, -joint.Position.Z + 2.0f);
	}
	
	void RotateJoint(Kinect.Body body, int bone) {
		//if blendWeight is 0 there is no need to compute the rotations
		if( blendWeight <= 0 ){ return; }
		Vector3 upDir = new Vector3();
		Vector3 rightDir = new Vector3();
		
		if(bone == (int)Kinect.JointType.SpineMid)
		{
			upDir = ((HipLeft.transform.position + HipRight.transform.position) / 2.0f) - HipOverride.transform.position;
			rightDir = HipRight.transform.position - HipLeft.transform.position;
		}
		
		//if the model is not animated, reset rotations to fix twisted joints
		if(!animated){_bones[bone].transform.localRotation = _baseRotation[bone];}
		//if the required bone data from the kinect isn't available, return
		Kinect.Joint? boneJoint = body.Joints[(Kinect.JointType)bone];
		if( !boneJoint.HasValue )
		{
			return;
		}
		//get the target direction of the bone in world space
		//for the majority of bone it's bone - 1 to bone, but Hip_Override and the outside
		//shoulders are determined differently.
		
		Vector3 dir = _boneDir[bone];
		Vector3 target;
		
		//if bone % 4 == 0 then it is either an outside shoulder or the hip override
		if(bone % 4 == 0)
		{
			//hip override is at Hip_Left
			if(bone == (int)Kinect.JointType.HipLeft)
			{
				//target = vector from hip_center to average of hips left and right
				target = ((GetVector3FromJoint(body.Joints[Kinect.JointType.HipLeft]) + GetVector3FromJoint(body.Joints[Kinect.JointType.HipRight])) / 2.0f) - GetVector3FromJoint(body.Joints[Kinect.JointType.SpineMid]);
			}
			//otherwise it is one of the shoulders
			else
			{
				//target = vector from shoulder_center to bone
				target = GetVector3FromJoint(body.Joints[(Kinect.JointType)bone]) - GetVector3FromJoint(body.Joints[Kinect.JointType.SpineShoulder]);
			}
		}
		else
		{
			//target = vector from previous bone to bone
			target = GetVector3FromJoint(body.Joints[(Kinect.JointType)bone]) - GetVector3FromJoint(body.Joints[(Kinect.JointType)bone-1]);
		}
		//transform it into bone-local space (independant of the transform of the controller)
		target = transform.TransformDirection(target);
		target = _bones[bone].transform.InverseTransformDirection(target);
		//create a rotation that rotates dir into target
		Quaternion quat = Quaternion.FromToRotation(dir,target);
		//if bone is the spine, add in the rotation along the spine
		if(bone == (int)Kinect.JointType.SpineMid)
		{
			//rotate the chest so that it faces forward (determined by the shoulders)
			dir = _chestRight;
			target = GetVector3FromJoint(body.Joints[Kinect.JointType.ShoulderRight]) - GetVector3FromJoint(body.Joints[Kinect.JointType.ShoulderLeft]);
			
			target = transform.TransformDirection(target);
			target = _bones[bone].transform.InverseTransformDirection(target);
			target -= Vector3.Project(target,_boneDir[bone]);
			
			quat *= Quaternion.FromToRotation(dir,target);
			
			//_bones[bone].transform.position = GetVector3FromJoint(body.Joints[Kinect.JointType.SpineMid]);
		}
		//if bone is the hip override, add in the rotation along the hips
		else if(bone == (int)Kinect.JointType.HipLeft)
		{
			//rotate the hips so they face forward (determined by the hips)
			dir = _hipRight;
			target = GetVector3FromJoint(body.Joints[Kinect.JointType.HipRight]) - GetVector3FromJoint(body.Joints[Kinect.JointType.HipLeft]);
			
			target = transform.TransformDirection(target);
			target = _bones[bone].transform.InverseTransformDirection(target);
			target -= Vector3.Project(target,_boneDir[bone]);
			
			quat *= Quaternion.FromToRotation(dir,target);
		}
		
		//reduce the effect of the rotation using the blend parameter
		quat = Quaternion.Lerp(Quaternion.identity, quat, blendWeight);
		//apply the rotation to the local rotation of the bone
		_bones[bone].transform.localRotation = _bones[bone].transform.localRotation * quat;
		
		if(bone == (int)Kinect.JointType.SpineMid)
		{
			restoreBone(_bones[(int)Kinect.JointType.HipLeft],_boneDir[(int)Kinect.JointType.HipLeft],upDir);
			restoreBone(_bones[(int)Kinect.JointType.HipLeft],_hipRight,rightDir);
		}
		
		return;
	}
	
	void restoreBone(GameObject bone,Vector3 dir, Vector3 target)
	{
		//transform target into bone-local space (independant of the transform of the controller)
		//target = transform.TransformDirection(target);
		target = bone.transform.InverseTransformDirection(target);
		//create a rotation that rotates dir into target
		Quaternion quat = Quaternion.FromToRotation(dir,target);
		bone.transform.localRotation *= quat;
	}
}