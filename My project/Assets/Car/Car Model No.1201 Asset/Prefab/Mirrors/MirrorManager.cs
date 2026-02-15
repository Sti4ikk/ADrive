using UnityEngine;

public class CarMirrorsManager : MonoBehaviour
{
    [System.Serializable]
    public class MirrorSetup
    {
        public string name;
        public Camera camera;
        public RenderTexture renderTexture;
        public MeshRenderer mirrorRenderer; // Renderer ������� ��� �������� ���������
        [Range(64, 1024)] public int resolution = 256;
        public bool isActive = true;
    }

    [Header("�������")]
    public MirrorSetup leftMirror;
    public MirrorSetup rightMirror;
    public MirrorSetup rearMirror;

    [Header("��������� ��������")]
    [Range(1, 4)] public int updateInterval = 2; // ��������� ������ N-� ����
    public bool alternateUpdates = true; // ���������� ���������� ������
    public float farClipPlane = 80f;
    public bool enableShadows = false;

    [Header("������������ �����������")]
    public bool useDynamicQuality = true;
    public Rigidbody carRigidbody;
    public Camera playerCamera; // ������� ������ ��� �������� ���������

    [Header("��������� �� ��������")]
    public float lowSpeedThreshold = 20f; // ��/�
    public float highSpeedThreshold = 80f;
    [Range(1, 4)] public int lowSpeedInterval = 1;
    [Range(1, 4)] public int mediumSpeedInterval = 2;
    [Range(1, 4)] public int highSpeedInterval = 3;

    [Header("���������� (������ ��� ������)")]
    public int currentFPS;
    public float currentSpeed;

    private int frameCounter = 0;
    private MirrorSetup[] allMirrors;

    void Start()
    {
        allMirrors = new MirrorSetup[] { leftMirror, rightMirror, rearMirror };

        foreach (var mirror in allMirrors)
        {
            SetupMirror(mirror);
        }
    }

    void SetupMirror(MirrorSetup mirror)
    {
        if (mirror.camera == null) return;

        // ������ Render Texture ���� �� ��������
        if (mirror.renderTexture == null)
        {
            mirror.renderTexture = new RenderTexture(mirror.resolution, mirror.resolution, 16);
            mirror.renderTexture.filterMode = FilterMode.Bilinear;
            mirror.renderTexture.name = mirror.name + "_RT";
        }

        // ��������� ������
        mirror.camera.targetTexture = mirror.renderTexture;
        mirror.camera.enabled = false; // �������� �������
        mirror.camera.farClipPlane = farClipPlane;
        mirror.camera.allowHDR = false;
        mirror.camera.allowMSAA = false;
        mirror.camera.renderingPath = RenderingPath.Forward;

        // ����
        if (!enableShadows)
        {
            mirror.camera.clearFlags = CameraClearFlags.SolidColor;
            mirror.camera.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
        }

        // ��������� �������� ���� (UI � �.�.)
        mirror.camera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
    }

    void LateUpdate()
    {
        frameCounter++;

        // ������������ �����������
        if (useDynamicQuality)
        {
            UpdateDynamicQuality();
        }

        // ����������
        currentFPS = (int)(1f / Time.unscaledDeltaTime);
        if (carRigidbody != null)
        {
            currentSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;
        }

        // ���������� ������
        if (alternateUpdates)
        {
            UpdateMirrorsAlternating();
        }
        else
        {
            UpdateMirrorsSimultaneous();
        }
    }

    void UpdateDynamicQuality()
    {
        if (carRigidbody == null) return;

        float speed = carRigidbody.linearVelocity.magnitude * 3.6f;

        // ������ ������� ���������� � ����������� �� ��������
        if (speed < lowSpeedThreshold)
        {
            updateInterval = lowSpeedInterval;
        }
        else if (speed < highSpeedThreshold)
        {
            updateInterval = mediumSpeedInterval;
        }
        else
        {
            updateInterval = highSpeedInterval;
        }
    }

    void UpdateMirrorsSimultaneous()
    {
        // ��� ������� ����������� ������������ ������ N-� ����
        if (frameCounter % updateInterval != 0) return;

        foreach (var mirror in allMirrors)
        {
            if (mirror.isActive && IsMirrorVisible(mirror))
            {
                mirror.camera.Render();
            }
        }
    }

    void UpdateMirrorsAlternating()
    {
        // ������� ����������� �� ������� ��� ������������� ��������
        int mirrorIndex = (frameCounter / updateInterval) % allMirrors.Length;

        if (frameCounter % updateInterval == 0)
        {
            var mirror = allMirrors[mirrorIndex];
            if (mirror.isActive && IsMirrorVisible(mirror))
            {
                mirror.camera.Render();
            }
        }
    }

    bool IsMirrorVisible(MirrorSetup mirror)
    {
        // ��������� ��������� ������� ������� �������
        if (playerCamera == null || mirror.mirrorRenderer == null) return true;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        return GeometryUtility.TestPlanesAABB(planes, mirror.mirrorRenderer.bounds);
    }

    void OnDestroy()
    {
        // ������� ������
        foreach (var mirror in allMirrors)
        {
            if (mirror.renderTexture != null)
            {
                mirror.renderTexture.Release();
            }
        }
    }

    // ������ ��� ��������� �������� � runtime
    public void SetQualityPreset(QualityPreset preset)
    {
        switch (preset)
        {
            case QualityPreset.Ultra:
                SetResolutions(512, 512, 512);
                updateInterval = 1;
                alternateUpdates = false;
                farClipPlane = 100f;
                enableShadows = true;
                break;

            case QualityPreset.High:
                SetResolutions(512, 512, 256);
                updateInterval = 2;
                alternateUpdates = false;
                farClipPlane = 80f;
                enableShadows = false;
                break;

            case QualityPreset.Medium:
                SetResolutions(256, 256, 256);
                updateInterval = 2;
                alternateUpdates = true;
                farClipPlane = 60f;
                enableShadows = false;
                break;

            case QualityPreset.Low:
                SetResolutions(256, 256, 128);
                updateInterval = 3;
                alternateUpdates = true;
                farClipPlane = 40f;
                enableShadows = false;
                break;

            case QualityPreset.Performance:
                SetResolutions(128, 128, 128);
                updateInterval = 4;
                alternateUpdates = true;
                farClipPlane = 30f;
                enableShadows = false;
                break;
        }

        // ��������� ���������
        foreach (var mirror in allMirrors)
        {
            SetupMirror(mirror);
        }
    }

    void SetResolutions(int left, int right, int rear)
    {
        leftMirror.resolution = left;
        rightMirror.resolution = right;
        rearMirror.resolution = rear;
    }
}

public enum QualityPreset
{
    Ultra,      // ������������ �������� - 512x512, ������ ����
    High,       // ������� - 512/256, ������ 2-� ����
    Medium,     // ������� - 256x256, �����������
    Low,        // ������ - 256/128, �����������
    Performance // ������������������ - 128x128, ������ 4-� ����
}