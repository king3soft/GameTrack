using System;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;

public class CallNativeCode : MonoBehaviour {

	[DllImport("native")]
	private static extern float add(float x, float y);
	
	[DllImport("native")]
	private static extern void perf_init();
	
	[DllImport("native")]
	private static extern void perf_counter(string name, float val);

	[DllImport("native")]
	private static extern void perf_event(string eventName);
	
	[DllImport("native")]
	private static extern void perf_trigger_scene(string scene_name);
	
	[DllImport("native")]
	private static extern void perf_pss();
	
	
	[DllImport("native")]
	private static extern void perf_stop();
	
	public void Perf_Init()
	{
		Debug.Log("Perf_Init");
		perf_init();
	}

	public void Perf_Log()
	{
		Debug.Log("Perf_Log");
		for (int i = 0; i < 100; i++)
		{
			perf_counter("tick", Random.value);
			perf_counter("update", Random.value);
			if( i%10 == 0)
				perf_event("event" + i);
			if ( i%5 == 0)
				perf_trigger_scene("scene" + i);
		}
	}
	
	public void Perf_Stop()
	{
		Debug.Log("Perf_Stop");
		perf_stop();
	}

	public void Perf_Pss()
	{
		for (int i = 0; i < int.MaxValue; i++)
		{
			perf_pss();
		}
	}
}
