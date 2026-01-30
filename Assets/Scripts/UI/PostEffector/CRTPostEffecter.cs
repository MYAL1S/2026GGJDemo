using UnityEngine;

/// <summary>
/// CRT 复古显示器后处理效果
/// 实现 IPostProcessEffect 接口以支持框架管理
/// </summary>
[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CRTPostEffecter : MonoBehaviour, IPostProcessEffect
{
    [Header("材质设置")]
    [Tooltip("CRT 效果材质，留空则自动从 Resources 加载")]
    public Material material;

    [Header("白噪声设置")]
    [Range(0, 100)]
    public int whiteNoiseFrequency = 0;
    [Range(0.01f, 2f)]
    public float whiteNoiseLength = 0.1f;
    private float whiteNoiseTimeLeft = 0.1f;

    [Header("屏幕跳动设置")]
    [Range(0, 100)]
    public int screenJumpFrequency = 0;
    [Range(0.01f, 2f)]
    public float screenJumpLength = 0.2f;
    [Range(0f, 1f)]
    public float screenJumpMinLevel = 0.1f;
    [Range(0f, 1f)]
    public float screenJumpMaxLevel = 0.9f;
    private float screenJumpTimeLeft;

    [Header("闪烁设置")]
    [Range(0f, 0.1f)]
    public float flickeringStrength = 0;
    [Range(1f, 200f)]
    public float flickeringCycle = 111f;

    [Header("色差设置")]
    public bool isChromaticAberration = true;
    [Range(0f, 0.05f)]
    public float chromaticAberrationStrength = 0.003f;

    [Header("重影设置")]
    public bool isMultipleGhost = true;
    [Range(0f, 0.1f)]
    public float multipleGhostStrength = 0.01f;

    [Header("扫描线设置")]
    public bool isScanline = true;
    public bool isMonochrome = false;

    [Header("画面撕裂设置")]
    public bool isSlippage = true;
    public bool isSlippageNoise = true;
    [Range(0f, 0.05f)]
    public float slippageStrength = 0.005f;
    [Range(0.1f, 5f)]
    public float slippageInterval = 1f;
    [Range(1f, 100f)]
    public float slippageScrollSpeed = 10f;
    [Range(1f, 50f)]
    public float slippageSize = 11f;

    [Header("信箱模式")]
    public bool isLetterBox = false;
    public bool isLetterBoxEdgeBlur = false;
    public LeterBoxType letterBoxType;
    public enum LeterBoxType
    {
        Black,
        Blur
    }

    [Header("胶片污渍")]
    public bool isFilmDirt = false;
    public Texture2D filmDirtTex;

    [Header("贴花纹理")]
    public bool isDecalTex = false;
    public Texture2D decalTex;
    public Vector2 decalTexPos = new Vector2(0.75f,0.7f);
    public Vector2 decalTexScale = Vector2.one * 5;

    [Header("降分辨率")]
    public bool isLowResolution = false;
    public Vector2Int resolutions = new Vector2Int(640, 480);

    #region Shader 属性 ID 缓存
    private int _WhiteNoiseOnOff;
    private int _ScanlineOnOff;
    private int _MonochormeOnOff;
    private int _ScreenJumpLevel;
    private int _FlickeringStrength;
    private int _FlickeringCycle;
    private int _SlippageStrength;
    private int _SlippageSize;
    private int _SlippageInterval;
    private int _SlippageScrollSpeed;
    private int _SlippageNoiseOnOff;
    private int _SlippageOnOff;
    private int _ChromaticAberrationStrength;
    private int _ChromaticAberrationOnOff;
    private int _MultipleGhostOnOff;
    private int _MultipleGhostStrength;
    private int _LetterBoxOnOff;
    private int _LetterBoxType;
    private int _LetterBoxEdgeBlurOnOff;
    private int _DecalTex;
    private int _DecalTexOnOff;
    private int _DecalTexPos;
    private int _DecalTexScale;
    private int _FilmDirtOnOff;
    private int _FilmDirtTex;
    #endregion

    private Camera _camera;
    private bool _isInitialized = false;
    private const string DEFAULT_MATERIAL_PATH = "UI/PostEffector/CRT/Material/CRT";

    #region IPostProcessEffect 接口实现

    public string EffectName => "CRT Screen Effect";

    public bool IsEnabled
    {
        get => enabled;
        set => enabled = value;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            Debug.LogWarning($"[{EffectName}] 已经初始化，跳过重复初始化");
            return;
        }

        // 获取摄像机组件
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError($"[{EffectName}] 未找到 Camera 组件！");
            return;
        }

        // 配置摄像机
        ConfigureCamera();

        // 加载材质
        if (!LoadMaterial())
        {
            return;
        }

        // 缓存 Shader 属性 ID
        CacheShaderPropertyIDs();

        _isInitialized = true;
        Debug.Log($"[{EffectName}] 初始化完成 - Camera: {_camera.name}");
    }

    public void Cleanup()
    {
        // 清理资源
        if (material != null)
        {
            // 注意：如果材质是从 Resources 加载的，这里不需要销毁
            // 因为 Resources 会自动管理资源生命周期
            Debug.Log($"[{EffectName}] 清理资源");
        }
        _isInitialized = false;
    }

    #endregion

    #region Unity 生命周期

    private void Awake()
    {
        // Awake 中不初始化，避免与 UIMgr 的 Initialize 调用冲突
        // 仅在 Editor 模式下且组件是手动添加的情况下初始化
        if (Application.isEditor && !Application.isPlaying)
        {
            _camera = GetComponent<Camera>();
        }
    }

    private void Start()
    {
        // 如果未通过 UIMgr 初始化（例如手动挂载的情况），则在 Start 中初始化
        if (!_isInitialized)
        {
            Initialize();
        }
    }


    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion

    #region 初始化辅助方法

    /// <summary>
    /// 配置摄像机设置
    /// </summary>
    private void ConfigureCamera()
    {
        if (_camera == null) return;

        // 禁用 MSAA（后处理不兼容抗锯齿）
        _camera.allowMSAA = false;

        // 多摄像机模式提示
        if (_camera.clearFlags == CameraClearFlags.Depth)
        {
            Debug.Log($"[{EffectName}] 检测到多摄像机配置 (Depth Only)");
        }
    }

    /// <summary>
    /// 加载 CRT 材质
    /// </summary>
    private bool LoadMaterial()
    {
        // 如果已经手动赋值，直接使用
        if (material != null)
        {
            Debug.Log($"[{EffectName}] 使用手动赋值的材质: {material.name}");
            return true;
        }

        // 从 Resources 自动加载
        material = ResMgr.Instance.Load<Material>(DEFAULT_MATERIAL_PATH);
        if (material == null)
        {
            Debug.LogError($"[{EffectName}] 无法加载材质！路径: Resources/{DEFAULT_MATERIAL_PATH}.mat");
            return false;
        }

        Debug.Log($"[{EffectName}] 材质自动加载成功: {material.name}");
        return true;
    }

    /// <summary>
    /// 缓存 Shader 属性 ID（性能优化）
    /// </summary>
    private void CacheShaderPropertyIDs()
    {
        _WhiteNoiseOnOff = Shader.PropertyToID("_WhiteNoiseOnOff");
        _ScanlineOnOff = Shader.PropertyToID("_ScanlineOnOff");
        _MonochormeOnOff = Shader.PropertyToID("_MonochormeOnOff");
        _ScreenJumpLevel = Shader.PropertyToID("_ScreenJumpLevel");
        _FlickeringStrength = Shader.PropertyToID("_FlickeringStrength");
        _FlickeringCycle = Shader.PropertyToID("_FlickeringCycle");
        _SlippageStrength = Shader.PropertyToID("_SlippageStrength");
        _SlippageSize = Shader.PropertyToID("_SlippageSize");
        _SlippageInterval = Shader.PropertyToID("_SlippageInterval");
        _SlippageScrollSpeed = Shader.PropertyToID("_SlippageScrollSpeed");
        _SlippageNoiseOnOff = Shader.PropertyToID("_SlippageNoiseOnOff");
        _SlippageOnOff = Shader.PropertyToID("_SlippageOnOff");
        _ChromaticAberrationStrength = Shader.PropertyToID("_ChromaticAberrationStrength");
        _ChromaticAberrationOnOff = Shader.PropertyToID("_ChromaticAberrationOnOff");
        _MultipleGhostOnOff = Shader.PropertyToID("_MultipleGhostOnOff");
        _MultipleGhostStrength = Shader.PropertyToID("_MultipleGhostStrength");
        _LetterBoxOnOff = Shader.PropertyToID("_LetterBoxOnOff");
        _LetterBoxType = Shader.PropertyToID("_LetterBoxType");
        _DecalTex = Shader.PropertyToID("_DecalTex");
        _DecalTexOnOff = Shader.PropertyToID("_DecalTexOnOff");
        _DecalTexPos = Shader.PropertyToID("_DecalTexPos");
        _DecalTexScale = Shader.PropertyToID("_DecalTexScale");
        _FilmDirtOnOff = Shader.PropertyToID("_FilmDirtOnOff");
        _FilmDirtTex = Shader.PropertyToID("_FilmDirtTex");
    }

    #endregion

    #region 渲染处理

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // 安全检查
        if (material == null)
        {
            Debug.LogWarning($"[{EffectName}] 材质为空，跳过后处理");
            Graphics.Blit(src, dest);
            return;
        }

        // 应用所有效果参数
        ApplyWhiteNoise();
        ApplyLetterBox();
        ApplyScanlineAndColor();
        ApplyFlickering();
        ApplyChromaticAberration();
        ApplyMultipleGhost();
        ApplyFilmDirt();
        ApplySlippage();
        ApplyScreenJump();
        ApplyDecalTexture();

        // 执行渲染
        RenderWithEffect(src, dest);
    }

    /// <summary>
    /// 应用白噪声效果
    /// </summary>
    private void ApplyWhiteNoise()
    {
        whiteNoiseTimeLeft -= Time.deltaTime;
        if (whiteNoiseTimeLeft <= 0)
        {
            if (Random.Range(0, 1000) < whiteNoiseFrequency)
            {
                material.SetInteger(_WhiteNoiseOnOff, 1);
                whiteNoiseTimeLeft = whiteNoiseLength;
            }
            else
            {
                material.SetInteger(_WhiteNoiseOnOff, 0);
            }
        }
    }

    /// <summary>
    /// 应用信箱模式
    /// </summary>
    private void ApplyLetterBox()
    {
        material.SetInteger(_LetterBoxOnOff, isLetterBox ? 0 : 1);
        material.SetInteger(_LetterBoxType, (int)letterBoxType);
    }

    /// <summary>
    /// 应用扫描线和颜色效果
    /// </summary>
    private void ApplyScanlineAndColor()
    {
        material.SetInteger(_ScanlineOnOff, isScanline ? 1 : 0);
        material.SetInteger(_MonochormeOnOff, isMonochrome ? 1 : 0);
    }

    /// <summary>
    /// 应用闪烁效果
    /// </summary>
    private void ApplyFlickering()
    {
        material.SetFloat(_FlickeringStrength, flickeringStrength);
        material.SetFloat(_FlickeringCycle, flickeringCycle);
    }

    /// <summary>
    /// 应用色差效果
    /// </summary>
    private void ApplyChromaticAberration()
    {
        material.SetFloat(_ChromaticAberrationStrength, chromaticAberrationStrength);
        material.SetInteger(_ChromaticAberrationOnOff, isChromaticAberration ? 1 : 0);
    }

    /// <summary>
    /// 应用重影效果
    /// </summary>
    private void ApplyMultipleGhost()
    {
        material.SetInteger(_MultipleGhostOnOff, isMultipleGhost ? 1 : 0);
        material.SetFloat(_MultipleGhostStrength, multipleGhostStrength);
    }

    /// <summary>
    /// 应用胶片污渍
    /// </summary>
    private void ApplyFilmDirt()
    {
        material.SetInteger(_FilmDirtOnOff, isFilmDirt ? 1 : 0);
        if (filmDirtTex != null)
        {
            material.SetTexture(_FilmDirtTex, filmDirtTex);
        }
    }

    /// <summary>
    /// 应用画面撕裂效果
    /// </summary>
    private void ApplySlippage()
    {
        material.SetInteger(_SlippageOnOff, isSlippage ? 1 : 0);
        material.SetFloat(_SlippageInterval, slippageInterval);
        material.SetFloat(_SlippageNoiseOnOff, isSlippageNoise ? Random.Range(0, 1f) : 1);
        material.SetFloat(_SlippageScrollSpeed, slippageScrollSpeed);
        material.SetFloat(_SlippageStrength, slippageStrength);
        material.SetFloat(_SlippageSize, slippageSize);
    }

    /// <summary>
    /// 应用屏幕跳动效果
    /// </summary>
    private void ApplyScreenJump()
    {
        screenJumpTimeLeft -= Time.deltaTime;
        if (screenJumpTimeLeft <= 0)
        {
            if (Random.Range(0, 1000) < screenJumpFrequency)
            {
                float level = Random.Range(screenJumpMinLevel, screenJumpMaxLevel);
                material.SetFloat(_ScreenJumpLevel, level);
                screenJumpTimeLeft = screenJumpLength;
            }
            else
            {
                material.SetFloat(_ScreenJumpLevel, 0);
            }
        }
    }

    /// <summary>
    /// 应用贴花纹理
    /// </summary>
    private void ApplyDecalTexture()
    {
        material.SetInteger(_DecalTexOnOff, isDecalTex ? 1 : 0);
        if (decalTex != null)
        {
            material.SetTexture(_DecalTex, decalTex);
            material.SetVector(_DecalTexPos, decalTexPos);
            material.SetVector(_DecalTexScale, decalTexScale);
        }
    }

    /// <summary>
    /// 执行最终渲染
    /// </summary>
    private void RenderWithEffect(RenderTexture src, RenderTexture dest)
    {
        if (isLowResolution && resolutions.x > 0 && resolutions.y > 0)
        {
            // 使用降分辨率效果
            RenderTexture target = RenderTexture.GetTemporary(
                Mathf.Max(1, src.width / 2),
                Mathf.Max(1, src.height / 2),
                0,
                src.format
            );
            Graphics.Blit(src, target);
            Graphics.Blit(target, dest, material);
            RenderTexture.ReleaseTemporary(target);
        }
        else
        {
            // 直接渲染
            Graphics.Blit(src, dest, material);
        }
    }

    #endregion

    #region 公共方法（用于运行时调整）

    /// <summary>
    /// 重置所有效果为默认值
    /// </summary>
    public void ResetToDefault()
    {
        whiteNoiseFrequency = 1;
        whiteNoiseLength = 0.1f;
        screenJumpFrequency = 1;
        screenJumpLength = 0.2f;
        flickeringStrength = 0.002f;
        isScanline = true;
        isMonochrome = false;
        isChromaticAberration = true;
        isMultipleGhost = true;
        isSlippage = true;
        isLowResolution = true;

        Debug.Log($"[{EffectName}] 已重置为默认设置");
    }

    /// <summary>
    /// 设置效果强度（0-1）
    /// </summary>
    public void SetIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        flickeringStrength = 0.002f * intensity;
        chromaticAberrationStrength = 0.005f * intensity;
        multipleGhostStrength = 0.01f * intensity;
        slippageStrength = 0.005f * intensity;
    }

    #endregion
}