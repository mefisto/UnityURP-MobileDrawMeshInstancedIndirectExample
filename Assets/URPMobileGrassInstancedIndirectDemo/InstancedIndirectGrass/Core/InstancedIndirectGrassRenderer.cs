﻿//see this for ref: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html

using UnityEngine;

[ExecuteAlways]
public class InstancedIndirectGrassRenderer : MonoBehaviour
{
    [Range(1,100000)]
    public int instanceCount = 20000;
    public Material instanceMaterial;

    //global ref to this script
    [HideInInspector]
    public static InstancedIndirectGrassRenderer instance;

    private int cachedInstanceCount = -1;
    private Vector3 cachedPivotPos = Vector3.negativeInfinity;
    private Vector3 cachedLocalScale = Vector3.negativeInfinity;
    private Mesh cachedGrassMesh;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;

    void LateUpdate()
    {
        instance = this; // assign global ref using this script

        // Update _TransformBuffer in grass shader if needed
        UpdateBuffersIfNeeded();

        // Render     
        Graphics.DrawMeshInstancedIndirect(GetGrassMeshCache(), 0, instanceMaterial, new Bounds(transform.position, transform.localScale), argsBuffer);
    }
    void OnDisable()
    {
        //release all compute buffers
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(300, 50, 200, 30), "Instance Count: " + instanceCount.ToString());
        instanceCount = Mathf.Max(1,(int)(GUI.HorizontalSlider(new Rect(300, 100, 200, 30), instanceCount / 10000f, 0, 10)) *10000);
    }

    Mesh GetGrassMeshCache()
    {
        if (!cachedGrassMesh)
        {
            //if not exist, create a 3 vertices hardcode triangle grass mesh
            cachedGrassMesh = new Mesh();

            //first grass (vertices)
            Vector3[] verts = new Vector3[3];
            verts[0] = new Vector3(-0.25f, 0);
            verts[1] = new Vector3(+0.25f, 0);
            verts[2] = new Vector3(-0.0f, 1);
            //first grass (Triangles index)
            int[] trinagles = new int[3] { 2, 1, 0, }; //order to fit Cull Back in grass shader

            cachedGrassMesh.SetVertices(verts);
            cachedGrassMesh.SetTriangles(trinagles, 0);
        }

        return cachedGrassMesh;
    }

    void UpdateBuffersIfNeeded()
    {
        //early exit if no need update buffer
        if (cachedInstanceCount == instanceCount &&
            cachedPivotPos == transform.position &&
            cachedLocalScale == transform.localScale &&
            argsBuffer != null &&
            positionBuffer != null)
            {
                return;
            }
        //=============================================
        if (argsBuffer != null)
            argsBuffer.Release();
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        if (positionBuffer != null)
            positionBuffer.Release();
        Vector4[] positions = new Vector4[instanceCount];
        positionBuffer = new ComputeBuffer(positions.Length, sizeof(float)*4); //float4

        //keep grass visual the same
        Random.InitState(123);

        //spawn grass inside gizmo cube 
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = Vector3.zero;
            //local pos
            pos.x += Random.Range(-1f, 1f) * transform.localScale.x;
            pos.z += Random.Range(-1f, 1f) * transform.localScale.z;

            //local rotate
            //TODO: allow this gameobject's rotation affect grass, make sure to update bending grass's imaginary camera rotation also

            //world positon
            pos += transform.position;

            //world scale
            float size = Random.Range(2f, 5f);

            positions[i] = new Vector4(pos.x,pos.y,pos.z, size);
        }

        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_TransformBuffer", positionBuffer);
        instanceMaterial.SetVector("_PivotPosWS", transform.position);
        instanceMaterial.SetVector("_BoundSize", new Vector2(transform.localScale.x,transform.localScale.z));

        // Indirect args
        args[0] = (uint)GetGrassMeshCache().GetIndexCount(0);
        args[1] = (uint)instanceCount;
        args[2] = (uint)GetGrassMeshCache().GetIndexStart(0);
        args[3] = (uint)GetGrassMeshCache().GetBaseVertex(0);
        args[4] = 0;

        argsBuffer.SetData(args);

        //update cache to prevent future no-op update
        cachedInstanceCount = instanceCount;
        cachedPivotPos = transform.position;
        cachedLocalScale = transform.localScale;
    }
}