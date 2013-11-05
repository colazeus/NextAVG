using UnityEngine;
using System.Threading;
using System.Collections;

//主引擎脚本.
public class GalgameEngines : MonoBehaviour {

	//任务队列.
	CommandQueue commandQueue;
	static AutoResetEvent looker = new AutoResetEvent(false);
	//状态
	bool isToWait;

	// Use this for initialization
	void Start () {
		commandQueue = new CommandQueue();
		Thread t = new Thread(Add);
		t.Start();
	}
	
	// Update is called once per frame
	void Update () {
		if(!isToWait && commandQueue.Count > 0)
			Show ();
		else if(Input.GetMouseButtonUp(0))
		{
			isToWait = false;
			looker.Set ();
		}
	}

	void Show () {
		lock(commandQueue){
			isToWait = true;
			Debug.Log(commandQueue.Dequeue());
		}
	}

	void Do (string text) {
		lock(commandQueue) {
			Debug.Log("加入了元素" + text);
			commandQueue.Enqueue(text);
		}
		looker.WaitOne();
	}

	void Add () {
		Do ("1");
		Do ("2");
	}
}
