using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class JFPackage {
	//命令数据包.
	[System.Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ComanndPackage
	{
		public string Msg;
	}
}
