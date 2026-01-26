using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EnsureCameraRenders : MonoBehaviour
{
    private GameObject dummyQuad;
    
    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        
        // 如果是 Depth Only 模式，创建一个不可见的渲染对象
        if (cam.clearFlags == CameraClearFlags.Depth)
        {
            CreateInvisibleRenderTrigger();
        }
    }
    
    private void CreateInvisibleRenderTrigger()
    {
        // 创建一个极小的不可见 Quad
        dummyQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dummyQuad.name = "[CRT] Render Trigger";
        dummyQuad.transform.SetParent(transform);
        
        Camera cam = GetComponent<Camera>();
        dummyQuad.transform.localPosition = new Vector3(0, 0, cam.nearClipPlane + 0.01f);
        dummyQuad.transform.localRotation = Quaternion.identity;
        dummyQuad.transform.localScale = Vector3.one * 0.001f; // 极小尺寸
        
        // 设置为 UI 层，确保被 UI 摄像机渲染
        dummyQuad.layer = LayerMask.NameToLayer("UI");
        
        // 使用完全透明的材质
        Renderer renderer = dummyQuad.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("UI/Default"));
        mat.color = new Color(1, 1, 1, 0); // 完全透明
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        // 移除碰撞体
        Destroy(dummyQuad.GetComponent<Collider>());
        
        Debug.Log("[CRT] Created invisible render trigger for UI camera");
    }
    
    private void OnDestroy()
    {
        if (dummyQuad != null)
        {
            Destroy(dummyQuad);
        }
    }
}