using UnityEngine;
using System.Threading;
using System.Collections;

//主引擎脚本.
public class GalgameEngines : MonoBehaviour {

	//上次同步时间.
	private float mSynchronous;

	//Socket对象.
	public JFSocket mJFsorket;

	// Use this for initialization
	void Start () {
		mJFsorket = JFSocket.GetInstance();
	}
	
	// Update is called once per frame
	void Update () {
		if(mJFsorket.comanndPackages.Count > 0){
			foreach(JFPackage.ComanndPackage cp in mJFsorket.comanndPackages)
			{
				//Debug.Log(cp.Msg);
			}
			mJFsorket.comanndPackages.Clear();
		}
	}
}
