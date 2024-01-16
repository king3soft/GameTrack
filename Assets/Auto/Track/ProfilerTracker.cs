using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
// ReSharper disable InconsistentNaming
public class ProfilerTracker : MonoBehaviour
{
    // Profiling state.
    enum RenderState
    {
        eDrawcalls = 0,
        eBatches,
        eTotal
    }
    private readonly long[] mRenderData = new long[(ushort)RenderState.eTotal];
    
    // Profiling recorder.
    private ProfilerRecorder batchesRecorder;
    private ProfilerRecorder drawCallsRecorder;
    private ProfilerRecorder meshStatsRecorder;

    private Recorder[] srpBatchesRecorders;

    private void Awake()
    {
        StringBuilder headBuilder = new StringBuilder();
        headBuilder.Append("ts,category");
        for(RenderState i = 0; i < RenderState.eTotal; i++)
        {
            headBuilder.Append(",");
            headBuilder.Append(i.ToString());
        }
        GTrackSDK.GameTrack_ProfInit(headBuilder.ToString());
    }

    private void OnEnable()
    {
        batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
    }

    private void LateUpdate()
    {
        mRenderData[(int)RenderState.eDrawcalls] = drawCallsRecorder.LastValue;
        mRenderData[(int)RenderState.eBatches] = batchesRecorder.LastValue;
        //Debug.LogFormat("{0}/{1}", mRenderData[(int)RenderState.eDrawcalls], mRenderData[(int)RenderState.eBatches]);
        GTrackSDK.GameTrack_ProfUpdate(mRenderData, (int)RenderState.eTotal);
    }
    
    private void OnDisable()
    {
        meshStatsRecorder.Dispose();
        drawCallsRecorder.Dispose();
        batchesRecorder.Dispose();
    }
}