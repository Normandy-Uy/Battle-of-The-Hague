using UnityEngine;

/// <summary>Scene giant boss display names + legacy prefab root names.</summary>
public static class DutzGiantBossNames
{
    public const string PrincessZara = "Princess Zara";
    public const string GeneralRook = "General Rook";
    public const string Trililing = "Trililing";

    public const string GongBong = "Gong Bong";
    public const string Cawetan = "Cawetan";
    public const string Cawetano = "Cawetano";
    public const string Tamby = "Tamby";
    public const string Jonrem = "JONREM";
    public const string JonremPolicePrefix = "Jonrem Police";
    public const string MartyR = "Marty R";
    public const string ETol = "E-TOL";
    public const string BeybiM = "BEYBI M";
    public const string Hontavirus = "HONTAVIRUS";
    public const string LengLengLugaw = "LENG LENG LUGAW";
    public const string Gerbil = "Gerbil";
    public const string Joles = "JOLES";
    public const string Stone = "STONE";
    public const string MarkoLekta = "MARKO LEKTA";
    public const string MBilyar = "M BILYAR";
    public const string Piyaya = "Piyaya";
    public const string LironSinta = "Liron Sinta";
    public const string BoyIdol = "Boy Idol";
    public const string KBilyar = "K Bilyar";
    public const string IAmBaby = "I am baby";

    public const string LegacyGrandma = "SimpleCitizens_Grandma_White";
    public const string LegacyMid = "SimpleCitizens_Hippie_Giant_Mid";
    public const string LegacyEnd = "SimpleCitizens_Hippie_Giant";

    public static bool IsPrincessZara(string objectName) =>
        objectName == PrincessZara || objectName == GongBong;

    public static bool IsGongBong(string objectName) => objectName == GongBong;

    public static bool IsCawetan(string objectName) =>
        objectName == Cawetan || objectName == Cawetano;

    public static bool IsETol(string objectName) => objectName == ETol;

    public static bool IsBeybiM(string objectName) => objectName == BeybiM;

    public static bool IsHontavirus(string objectName) => objectName == Hontavirus;

    public static bool IsLengLengLugaw(string objectName) => objectName == LengLengLugaw;

    public static bool IsLevel03EndBoss(string objectName) => objectName == BeybiM;

    public static bool IsTamby(string objectName) => objectName == Tamby;

    public static bool IsGeneralRook(string objectName) =>
        objectName == GeneralRook || objectName == MartyR || objectName == LegacyMid;

    /// <summary>Level 1 Tamby or Level 2 General Rook — same mid-lane chase role, separate characters.</summary>
    public static bool IsMidTrackGiant(string objectName) =>
        IsTamby(objectName) || IsGeneralRook(objectName);

    public static bool IsJonrem(string objectName) => objectName == Jonrem;

    public static bool IsGerbil(string objectName) => objectName == Gerbil;

    public static bool IsJoles(string objectName) =>
        objectName == Joles || string.Equals(objectName, "Joles", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsStone(string objectName) =>
        objectName == Stone || string.Equals(objectName, "Stone", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsMarkoLekta(string objectName) =>
        objectName == MarkoLekta
        || string.Equals(objectName, "Marko Lekta", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsMBilyar(string objectName) =>
        objectName == MBilyar
        || string.Equals(objectName, "M Bilyar", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsPiyaya(string objectName) =>
        objectName == Piyaya
        || string.Equals(objectName, "PIYAYA", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsLironSinta(string objectName) =>
        objectName == LironSinta
        || string.Equals(objectName, "LIRON SINTA", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsBoyIdol(string objectName) =>
        objectName == BoyIdol
        || string.Equals(objectName, "BOY IDOL", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsKBilyar(string objectName) =>
        objectName == KBilyar
        || string.Equals(objectName, "K BILYAR", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsIAmBaby(string objectName) =>
        objectName == IAmBaby
        || string.Equals(objectName, "I AM BABY", System.StringComparison.OrdinalIgnoreCase)
        || string.Equals(objectName, "I Am Baby", System.StringComparison.OrdinalIgnoreCase);

    public static bool IsJonremPolice(string objectName) =>
        !string.IsNullOrEmpty(objectName)
        && objectName.StartsWith(JonremPolicePrefix, System.StringComparison.Ordinal);

    public static bool IsJonremEscort(string objectName) =>
        IsJonrem(objectName) || IsJonremPolice(objectName);

    public static bool IsTrililing(string objectName) =>
        objectName == Trililing || objectName == ETol || objectName == BeybiM || objectName == LegacyEnd;

    public static bool IsAnyGiantBoss(string objectName) =>
        IsPrincessZara(objectName) || IsCawetan(objectName) || IsMidTrackGiant(objectName)
        || IsTrililing(objectName) || IsJonrem(objectName) || IsGerbil(objectName)
        || IsJoles(objectName) || IsStone(objectName) || IsMarkoLekta(objectName)
        || IsMBilyar(objectName) || IsPiyaya(objectName) || IsLironSinta(objectName)
        || IsBoyIdol(objectName) || IsKBilyar(objectName) || IsIAmBaby(objectName)
        || IsHontavirus(objectName) || IsLengLengLugaw(objectName);

    public static GameObject FindHontavirus() => FindFirst(Hontavirus);

    public static GameObject FindLengLengLugaw() => FindFirst(LengLengLugaw);

    public static GameObject FindCawetan() => FindFirst(Cawetan, Cawetano);

    public static GameObject FindPrincessZara() =>
        FindFirst(PrincessZara, GongBong);

    public static GameObject FindGongBong() => FindFirst(GongBong);

    public static GameObject FindGeneralRook() =>
        FindFirst(GeneralRook, MartyR, LegacyMid);

    public static GameObject FindTamby() => FindFirst(Tamby);

    public static GameObject FindMidTrackGiant() =>
        FindFirst(Tamby, GeneralRook, MartyR, LegacyMid);

    public static GameObject FindTrililing() =>
        FindFirst(BeybiM, Trililing, ETol, LegacyEnd);

    public static GameObject FindJonrem() => FindFirst(Jonrem);

    public static GameObject FindGerbil() => FindFirst(Gerbil);

    public static GameObject FindJoles() => FindFirst(Joles, "Joles");

    public static GameObject FindStone() => FindFirst(Stone, "Stone");

    public static GameObject FindMarkoLekta() => FindFirst(MarkoLekta, "Marko Lekta");

    public static GameObject FindMBilyar() => FindFirst(MBilyar, "M Bilyar");

    public static GameObject FindPiyaya() => FindFirst(Piyaya, "PIYAYA");

    public static GameObject FindLironSinta() => FindFirst(LironSinta, "LIRON SINTA");

    public static GameObject FindBoyIdol() => FindFirst(BoyIdol, "BOY IDOL");

    public static GameObject FindKBilyar() => FindFirst(KBilyar, "K BILYAR");

    public static GameObject FindIAmBaby() => FindFirst(IAmBaby, "I AM BABY", "I Am Baby");

    static GameObject FindFirst(params string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            var go = GameObject.Find(names[i]);
            if (go != null)
                return go;
        }

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null)
                continue;

            for (var i = 0; i < names.Length; i++)
            {
                if (transform.name == names[i])
                    return transform.gameObject;
            }
        }

        return null;
    }
}

/// <summary>
/// Giant hippie boss: hippie body mesh + boss photo billboard locked in front of the head (always faces camera).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
[ExecuteAlways]
public class DutzGiantHippieBossFace : MonoBehaviour
{
    const string HippieMeshName = "SC_Hippie";
    const string GrandmaMeshName = "SC_Grandma";
    const string HeadBoneName = "Head_jnt";
    const string BossFaceObjectName = "BossFace";
    const string EndFaceMaterialResource = "GiantHippieBossFace";
    const string MidFaceMaterialResource = "GiantHippieBossFaceMid";
    const string GrandmaFaceMaterialResource = "GiantHippieBossFaceGrandma";
    const string EndFaceTextureResource = "GiantHippieBossFacePhoto";
    const string MidFaceTextureResource = "GiantHippieBossFacePhotoMid";
    const string GrandmaFaceTextureResource = "GiantHippieBossFacePhotoGrandma";
    const string GongBongFaceMaterialResource = "GongBongBossFace";
    const string GongBongFaceTextureResource = "GongBongBossFacePhoto";
    const string CawetanFaceMaterialResource = "CawetanBossFace";
    const string CawetanFaceTextureResource = "CawetanBossFacePhoto";
    const string TambyFaceMaterialResource = "TambyBossFace";
    const string TambyFaceTextureResource = "TambyBossFacePhoto";
    const string JonremFaceMaterialResource = "JonremBossFace";
    const string JonremFaceTextureResource = "JonremBossFacePhoto";
    const string GerbilFaceMaterialResource = "GerbilBossFace";
    const string GerbilFaceTextureResource = "GerbilBossFacePhoto";
    const string JolesFaceMaterialResource = "JolesBossFace";
    const string JolesFaceTextureResource = "JolesBossFacePhoto";
    const string StoneFaceMaterialResource = "StoneBossFace";
    const string StoneFaceTextureResource = "StoneBossFacePhoto";
    const string MarkoLektaFaceMaterialResource = "MarkoLektaBossFace";
    const string MarkoLektaFaceTextureResource = "MarkoLektaBossFacePhoto";
    const string MBilyarFaceMaterialResource = "MBilyarBossFace";
    const string MBilyarFaceTextureResource = "MBilyarBossFacePhoto";
    const string PiyayaFaceMaterialResource = "PiyayaBossFace";
    const string PiyayaFaceTextureResource = "PiyayaBossFacePhoto";
    const string LironSintaFaceMaterialResource = "LironSintaBossFace";
    const string LironSintaFaceTextureResource = "LironSintaBossFacePhoto";
    const string BoyIdolFaceMaterialResource = "BoyIdolBossFace";
    const string BoyIdolFaceTextureResource = "BoyIdolBossFacePhoto";
    const string KBilyarFaceMaterialResource = "KBilyarBossFace";
    const string KBilyarFaceTextureResource = "KBilyarBossFacePhoto";
    const string IAmBabyFaceMaterialResource = "IAmBabyBossFace";
    const string IAmBabyFaceTextureResource = "IAmBabyBossFacePhoto";
    const string BeybiMFaceMaterialResource = "BeybiMBossFace";
    const string BeybiMFaceTextureResource = "BeybiMBossFacePhoto";
    const string ETolFaceMaterialResource = "ETolBossFace";
    const string ETolFaceTextureResource = "ETolBossFacePhoto";
    const string HontavirusFaceMaterialResource = "HontavirusBossFace";
    const string HontavirusFaceTextureResource = "HontavirusBossFacePhoto";
    const string LengLengLugawFaceMaterialResource = "LengLengLugawBossFace";
    const string LengLengLugawFaceTextureResource = "LengLengLugawBossFacePhoto";
    const string FaceShaderName = "Dutz/BossFaceBillboard";
    const string HippieMaterialResource = "SimpleCitizens_Hippie_Black";

    const string EndFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFace.mat";
    const string MidFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFaceMid.mat";
    const string GrandmaFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFaceGrandma.mat";
    const string GongBongFaceMaterialPath = "Assets/Characters/NPCs/Resources/GongBongBossFace.mat";
    const string CawetanFaceMaterialPath = "Assets/Characters/NPCs/Resources/CawetanBossFace.mat";
    const string TambyFaceMaterialPath = "Assets/Characters/NPCs/Resources/TambyBossFace.mat";
    const string JonremFaceMaterialPath = "Assets/Characters/NPCs/Resources/JonremBossFace.mat";
    const string GerbilFaceMaterialPath = "Assets/Characters/NPCs/Resources/GerbilBossFace.mat";
    const string JolesFaceMaterialPath = "Assets/Characters/NPCs/Resources/JolesBossFace.mat";
    const string StoneFaceMaterialPath = "Assets/Characters/NPCs/Resources/StoneBossFace.mat";
    const string MarkoLektaFaceMaterialPath = "Assets/Characters/NPCs/Resources/MarkoLektaBossFace.mat";
    const string MBilyarFaceMaterialPath = "Assets/Characters/NPCs/Resources/MBilyarBossFace.mat";
    const string PiyayaFaceMaterialPath = "Assets/Characters/NPCs/Resources/PiyayaBossFace.mat";
    const string LironSintaFaceMaterialPath = "Assets/Characters/NPCs/Resources/LironSintaBossFace.mat";
    const string BoyIdolFaceMaterialPath = "Assets/Characters/NPCs/Resources/BoyIdolBossFace.mat";
    const string KBilyarFaceMaterialPath = "Assets/Characters/NPCs/Resources/KBilyarBossFace.mat";
    const string IAmBabyFaceMaterialPath = "Assets/Characters/NPCs/Resources/IAmBabyBossFace.mat";
    const string BeybiMFaceMaterialPath = "Assets/Characters/NPCs/Resources/BeybiMBossFace.mat";
    const string ETolFaceMaterialPath = "Assets/Characters/NPCs/Resources/ETolBossFace.mat";
    const string HontavirusFaceMaterialPath = "Assets/Characters/NPCs/Resources/HontavirusBossFace.mat";
    const string LengLengLugawFaceMaterialPath = "Assets/Characters/NPCs/Resources/LengLengLugawBossFace.mat";

    static bool IsGiantHippie(string objectName) => DutzGiantBossNames.IsAnyGiantBoss(objectName);

    bool IsLevel03TrackGiant() => DutzLevel03TrackGiantFaces.IsTrackGiant(gameObject.name);

    static bool ShouldEnsureBossFace(string objectName) =>
        IsGiantHippie(objectName)
        || (DutzCollectibleProgress.IsLevel03Gameplay && DutzCollectibleProgress.IsLevel03Giant(objectName));

    bool IsMidGiant() => DutzGiantBossNames.IsGeneralRook(gameObject.name);
    bool IsGongBongGiant() => DutzGiantBossNames.IsGongBong(gameObject.name);
    bool IsCawetanGiant() => DutzGiantBossNames.IsCawetan(gameObject.name);
    bool IsTambyGiant() => DutzGiantBossNames.IsTamby(gameObject.name);
    bool IsJonremGiant() => DutzGiantBossNames.IsJonrem(gameObject.name);
    bool IsGerbilGiant() => DutzGiantBossNames.IsGerbil(gameObject.name);
    bool IsJolesGiant() => DutzGiantBossNames.IsJoles(gameObject.name);
    bool IsStoneGiant() => DutzGiantBossNames.IsStone(gameObject.name);
    bool IsMarkoLektaGiant() => DutzGiantBossNames.IsMarkoLekta(gameObject.name);
    bool IsMBilyarGiant() => DutzGiantBossNames.IsMBilyar(gameObject.name);
    bool IsPiyayaGiant() => DutzGiantBossNames.IsPiyaya(gameObject.name);
    bool IsLironSintaGiant() => DutzGiantBossNames.IsLironSinta(gameObject.name);
    bool IsBoyIdolGiant() => DutzGiantBossNames.IsBoyIdol(gameObject.name);
    bool IsKBilyarGiant() => DutzGiantBossNames.IsKBilyar(gameObject.name);
    bool IsIAmBabyGiant() => DutzGiantBossNames.IsIAmBaby(gameObject.name);
    bool IsBeybiMGiant() => DutzGiantBossNames.IsBeybiM(gameObject.name);
    bool IsHontavirusGiant() => DutzGiantBossNames.IsHontavirus(gameObject.name);
    bool IsLengLengLugawGiant() => DutzGiantBossNames.IsLengLengLugaw(gameObject.name);
    bool IsETolGiant() => DutzGiantBossNames.IsETol(gameObject.name);
    bool IsGrandmaGiant() => DutzGiantBossNames.IsPrincessZara(gameObject.name);
    bool UsesGrandmaStylePresentation() => IsGrandmaGiant() || IsCawetanGiant();
    string GetBodyMeshName() =>
        UsesGrandmaStylePresentation() ? GrandmaMeshName : HippieMeshName;

    [SerializeField] Material faceMaterial;
    [SerializeField] Material hippieBodyMaterial;
    [SerializeField] Vector3 faceLocalOffset = new Vector3(0f, 0f, 0.045f);
    [SerializeField] Vector2 faceSize = new Vector2(0.34f, 0.19f);
    [SerializeField] bool applyCaricatureRig = true;
    [SerializeField] bool snapToFaceSurface = true;
    [SerializeField] float surfaceBias = 0.01f;
    [SerializeField] float playFaceForwardBias = 0.18f;
    [Tooltip("Roll around the view axis. -90 = rotate photo 90° clockwise (to the right) when facing the billboard.")]
    [SerializeField] float faceRollDegrees = -90f;
    [SerializeField] float faceWidthStretch = 1.35f;

    Transform headBone;
    Transform faceTransform;
    Material runtimeFaceMaterial;
    Material runtimeBodyMaterial;
    static Mesh quadMesh;
    static bool loggedPlayFaceWarning;

    public static void EnsureFromBoot()
    {
        EnsureBossFaceOn(DutzGiantBossNames.FindPrincessZara());
        EnsureBossFaceOn(DutzGiantBossNames.FindCawetan());
        EnsureBossFaceOn(DutzGiantBossNames.FindMidTrackGiant());
        EnsureBossFaceOn(DutzGiantBossNames.FindTrililing());
        EnsureBossFaceOn(DutzGiantBossNames.FindJonrem());
        EnsureBossFaceOn(DutzGiantBossNames.FindGerbil());
        EnsureBossFaceOn(DutzGiantBossNames.FindJoles());
        EnsureBossFaceOn(DutzGiantBossNames.FindHontavirus());
        EnsureBossFaceOn(DutzGiantBossNames.FindLironSinta());
        EnsureBossFaceOn(DutzGiantBossNames.FindBoyIdol());
        EnsureBossFaceOn(DutzGiantBossNames.FindKBilyar());
        EnsureBossFaceOn(DutzGiantBossNames.FindIAmBaby());
        EnsureBossFaceOn(DutzGiantBossNames.FindLengLengLugaw());

        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var trackRoot = GameObject.Find("DutzLevel03TrackGiants");
        if (trackRoot == null)
            return;

        foreach (Transform child in trackRoot.transform)
            EnsureBossFaceOn(child.gameObject);
    }

    static void EnsureBossFaceOn(GameObject giant)
    {
        if (giant == null || !ShouldEnsureBossFace(giant.name))
            return;

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        face.ApplyFace();
    }

    void Reset()
    {
        if (UsesGrandmaStylePresentation())
            applyCaricatureRig = false;
        AssignDefaultAssets();
    }

    void OnEnable() => ApplyFace();

    void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall += DelayedEditorApply;
#endif
    }

#if UNITY_EDITOR
    void DelayedEditorApply()
    {
        UnityEditor.EditorApplication.delayCall -= DelayedEditorApply;
        if (this == null)
            return;

        if (AssignDefaultAssets())
            ApplyFace();
    }
#endif

    void Awake() => ApplyFace();

    void Start() => ApplyFace();

    void LateUpdate()
    {
        if (Application.isMobilePlatform
            && DutzCollectibleProgress.IsLevel03Gameplay
            && !IsGiantBodyVisible())
        {
            return;
        }

        if (faceTransform == null || headBone == null)
        {
            ApplyFace();
            if (faceTransform == null)
                return;
        }

        var cam = GetViewCamera();
        if (cam == null)
            return;

        var toCamera = cam.transform.position - faceTransform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        var localPos = faceLocalOffset;
        if (Application.isPlaying && playFaceForwardBias > 0f)
        {
            var pushWorld = toCamera.normalized * playFaceForwardBias;
            var pushLocal = headBone.InverseTransformVector(pushWorld);
            localPos += pushLocal;
        }

        faceTransform.localPosition = localPos;
        faceTransform.localScale = new Vector3(faceSize.x * faceWidthStretch, faceSize.y, 1f);

        var toCameraLocal = headBone.InverseTransformDirection(toCamera.normalized);
        if (toCameraLocal.sqrMagnitude < 0.0001f)
            return;

        var faceCamera = Quaternion.LookRotation(-toCameraLocal, Vector3.up);
        faceTransform.localRotation = faceCamera * Quaternion.Euler(0f, 0f, faceRollDegrees);
    }

    public void ApplyFace()
    {
        AssignDefaultAssets();
        ResolveHeadBone();

        if (applyCaricatureRig && !UsesGrandmaStylePresentation())
            DutzGiantHippieCaricatureRig.Apply(gameObject);

        if (snapToFaceSurface)
            SnapBillboardToFaceSurface();

        if (!UsesGrandmaStylePresentation())
            ApplyHippieBodyMaterial();
        EnsureBossFaceBillboard();

        if (DutzGiantHeadTopCollider.UsesGiantHeadColliders(gameObject.name))
            DutzGiantHeadTopCollider.EnsureOnGiant(gameObject);
    }

    void SnapBillboardToFaceSurface()
    {
        faceLocalOffset = ComputeFaceSurfaceLocalOffset();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    Mesh GetSnapSourceMesh(SkinnedMeshRenderer renderer)
    {
        if (renderer == null)
            return null;

        var mesh = renderer.sharedMesh;
        if (mesh != null && mesh.isReadable)
            return mesh;

        var baked = new Mesh { name = "BossFaceSnapBake" };
        renderer.BakeMesh(baked);
        return baked;
    }

    Vector3 ComputeFaceSurfaceLocalOffset()
    {
        if (headBone == null)
            ResolveHeadBone();

        var renderer = FindHippieRenderer();
        if (headBone == null || renderer == null)
            return faceLocalOffset;

        var headIndex = FindHeadBoneIndex(renderer);
        if (headIndex < 0)
            return faceLocalOffset;

        var bakedMesh = GetSnapSourceMesh(renderer);
        if (bakedMesh == null)
            return faceLocalOffset;

        var ownsBakedMesh = bakedMesh != renderer.sharedMesh;
        var vertices = bakedMesh.vertices;
        var normals = bakedMesh.normals;
        var weights = bakedMesh.boneWeights;
        var useBoneWeights = !ownsBakedMesh &&
                             weights != null &&
                             weights.Length == vertices.Length;

        var hasNormals = normals != null && normals.Length == vertices.Length;
        var meshToHead = headBone.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
        var headForward = Vector3.forward;

        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var frontDepth = float.MinValue;
        var frontX = 0f;
        var frontY = 0f;
        var frontCount = 0;

        for (var i = 0; i < vertices.Length; i++)
        {
            var headLocal = meshToHead.MultiplyPoint3x4(vertices[i]);

            if (useBoneWeights)
            {
                if (GetHeadBoneWeight(weights[i], headIndex) < 0.2f)
                    continue;
            }
            else
            {
                if (Mathf.Abs(headLocal.x) > 0.22f || Mathf.Abs(headLocal.y) > 0.16f || headLocal.z < 0.03f)
                    continue;
            }

            if (hasNormals)
            {
                var headNormal = meshToHead.MultiplyVector(normals[i]).normalized;
                if (Vector3.Dot(headNormal, headForward) < 0.2f)
                    continue;
            }

            frontCount++;
            minX = Mathf.Min(minX, headLocal.x);
            maxX = Mathf.Max(maxX, headLocal.x);
            minY = Mathf.Min(minY, headLocal.y);
            maxY = Mathf.Max(maxY, headLocal.y);

            if (headLocal.z > frontDepth)
            {
                frontDepth = headLocal.z;
                frontX = headLocal.x;
                frontY = headLocal.y;
            }
        }

        if (ownsBakedMesh)
        {
            if (Application.isPlaying)
                Destroy(bakedMesh);
            else
                DestroyImmediate(bakedMesh);
        }

        if (frontCount == 0)
            return faceLocalOffset;

        var width = maxX - minX;
        var height = maxY - minY;
        if (width > 0.01f && height > 0.01f)
            faceSize = new Vector2(width * 0.92f, height * 0.92f);

        return new Vector3(
            Mathf.Lerp(frontX, (minX + maxX) * 0.5f, 0.35f),
            Mathf.Lerp(frontY, (minY + maxY) * 0.5f, 0.35f),
            frontDepth + surfaceBias);
    }

    static int FindHeadBoneIndex(SkinnedMeshRenderer renderer)
    {
        var bones = renderer.bones;
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == HeadBoneName)
                return i;
        }

        return -1;
    }

    static float GetHeadBoneWeight(BoneWeight weight, int headIndex)
    {
        var sum = 0f;
        if (weight.boneIndex0 == headIndex)
            sum += weight.weight0;
        if (weight.boneIndex1 == headIndex)
            sum += weight.weight1;
        if (weight.boneIndex2 == headIndex)
            sum += weight.weight2;
        if (weight.boneIndex3 == headIndex)
            sum += weight.weight3;
        return sum;
    }

#if UNITY_EDITOR
    void EnsureCorrectFaceMaterial()
    {
        var materialPath = IsLevel03TrackGiant() && DutzLevel03TrackGiantFaces.TryGetEntry(gameObject.name, out var trackEntry)
            ? trackEntry.materialAssetPath
            : IsGongBongGiant()
            ? GongBongFaceMaterialPath
            : IsCawetanGiant()
                ? CawetanFaceMaterialPath
            : IsTambyGiant()
                ? TambyFaceMaterialPath
                : IsJonremGiant()
                    ? JonremFaceMaterialPath
                : IsGerbilGiant()
                    ? GerbilFaceMaterialPath
                : IsJolesGiant()
                    ? JolesFaceMaterialPath
                : IsStoneGiant()
                    ? StoneFaceMaterialPath
                : IsMarkoLektaGiant()
                    ? MarkoLektaFaceMaterialPath
                : IsMBilyarGiant()
                    ? MBilyarFaceMaterialPath
                : IsPiyayaGiant()
                    ? PiyayaFaceMaterialPath
                : IsLironSintaGiant()
                    ? LironSintaFaceMaterialPath
                : IsBoyIdolGiant()
                    ? BoyIdolFaceMaterialPath
                : IsKBilyarGiant()
                    ? KBilyarFaceMaterialPath
                : IsIAmBabyGiant()
                    ? IAmBabyFaceMaterialPath
                : IsBeybiMGiant()
                    ? BeybiMFaceMaterialPath
                    : IsETolGiant()
                        ? ETolFaceMaterialPath
                        : IsHontavirusGiant()
                            ? HontavirusFaceMaterialPath
                            : IsLengLengLugawGiant()
                                ? LengLengLugawFaceMaterialPath
                        : IsGrandmaGiant()
                        ? GrandmaFaceMaterialPath
                        : IsMidGiant() ? MidFaceMaterialPath : EndFaceMaterialPath;
        var expected = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (expected != null && faceMaterial != expected)
        {
            faceMaterial = expected;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

    string GetFaceMaterialResourceName()
    {
        if (IsLevel03TrackGiant() && DutzLevel03TrackGiantFaces.TryGetEntry(gameObject.name, out var trackEntry))
            return trackEntry.materialResourceName;
        if (IsGongBongGiant())
            return GongBongFaceMaterialResource;
        if (IsCawetanGiant())
            return CawetanFaceMaterialResource;
        if (IsTambyGiant())
            return TambyFaceMaterialResource;
        if (IsJonremGiant())
            return JonremFaceMaterialResource;
        if (IsGerbilGiant())
            return GerbilFaceMaterialResource;
        if (IsJolesGiant())
            return JolesFaceMaterialResource;
        if (IsStoneGiant())
            return StoneFaceMaterialResource;
        if (IsMarkoLektaGiant())
            return MarkoLektaFaceMaterialResource;
        if (IsMBilyarGiant())
            return MBilyarFaceMaterialResource;
        if (IsPiyayaGiant())
            return PiyayaFaceMaterialResource;
        if (IsLironSintaGiant())
            return LironSintaFaceMaterialResource;
        if (IsBoyIdolGiant())
            return BoyIdolFaceMaterialResource;
        if (IsKBilyarGiant())
            return KBilyarFaceMaterialResource;
        if (IsIAmBabyGiant())
            return IAmBabyFaceMaterialResource;
        if (IsBeybiMGiant())
            return BeybiMFaceMaterialResource;
        if (IsETolGiant())
            return ETolFaceMaterialResource;
        if (IsHontavirusGiant())
            return HontavirusFaceMaterialResource;
        if (IsLengLengLugawGiant())
            return LengLengLugawFaceMaterialResource;
        if (IsGrandmaGiant())
            return GrandmaFaceMaterialResource;
        return IsMidGiant() ? MidFaceMaterialResource : EndFaceMaterialResource;
    }

    string GetFaceTextureResourceName()
    {
        if (IsLevel03TrackGiant() && DutzLevel03TrackGiantFaces.TryGetEntry(gameObject.name, out var trackEntry))
            return trackEntry.textureResourceName;
        if (IsGongBongGiant())
            return GongBongFaceTextureResource;
        if (IsCawetanGiant())
            return CawetanFaceTextureResource;
        if (IsTambyGiant())
            return TambyFaceTextureResource;
        if (IsJonremGiant())
            return JonremFaceTextureResource;
        if (IsGerbilGiant())
            return GerbilFaceTextureResource;
        if (IsJolesGiant())
            return JolesFaceTextureResource;
        if (IsStoneGiant())
            return StoneFaceTextureResource;
        if (IsMarkoLektaGiant())
            return MarkoLektaFaceTextureResource;
        if (IsMBilyarGiant())
            return MBilyarFaceTextureResource;
        if (IsPiyayaGiant())
            return PiyayaFaceTextureResource;
        if (IsLironSintaGiant())
            return LironSintaFaceTextureResource;
        if (IsBoyIdolGiant())
            return BoyIdolFaceTextureResource;
        if (IsKBilyarGiant())
            return KBilyarFaceTextureResource;
        if (IsIAmBabyGiant())
            return IAmBabyFaceTextureResource;
        if (IsBeybiMGiant())
            return BeybiMFaceTextureResource;
        if (IsETolGiant())
            return ETolFaceTextureResource;
        if (IsHontavirusGiant())
            return HontavirusFaceTextureResource;
        if (IsLengLengLugawGiant())
            return LengLengLugawFaceTextureResource;
        if (IsGrandmaGiant())
            return GrandmaFaceTextureResource;
        return IsMidGiant() ? MidFaceTextureResource : EndFaceTextureResource;
    }

#if UNITY_EDITOR
    string GetFaceMaterialAssetPath()
    {
        if (IsLevel03TrackGiant() && DutzLevel03TrackGiantFaces.TryGetEntry(gameObject.name, out var trackEntry))
            return trackEntry.materialAssetPath;
        if (IsGongBongGiant())
            return GongBongFaceMaterialPath;
        if (IsCawetanGiant())
            return CawetanFaceMaterialPath;
        if (IsTambyGiant())
            return TambyFaceMaterialPath;
        if (IsJonremGiant())
            return JonremFaceMaterialPath;
        if (IsGerbilGiant())
            return GerbilFaceMaterialPath;
        if (IsJolesGiant())
            return JolesFaceMaterialPath;
        if (IsStoneGiant())
            return StoneFaceMaterialPath;
        if (IsMarkoLektaGiant())
            return MarkoLektaFaceMaterialPath;
        if (IsMBilyarGiant())
            return MBilyarFaceMaterialPath;
        if (IsPiyayaGiant())
            return PiyayaFaceMaterialPath;
        if (IsLironSintaGiant())
            return LironSintaFaceMaterialPath;
        if (IsBoyIdolGiant())
            return BoyIdolFaceMaterialPath;
        if (IsKBilyarGiant())
            return KBilyarFaceMaterialPath;
        if (IsIAmBabyGiant())
            return IAmBabyFaceMaterialPath;
        if (IsBeybiMGiant())
            return BeybiMFaceMaterialPath;
        if (IsETolGiant())
            return ETolFaceMaterialPath;
        if (IsHontavirusGiant())
            return HontavirusFaceMaterialPath;
        if (IsLengLengLugawGiant())
            return LengLengLugawFaceMaterialPath;
        if (IsGrandmaGiant())
            return GrandmaFaceMaterialPath;
        return IsMidGiant() ? MidFaceMaterialPath : EndFaceMaterialPath;
    }
#endif

    void SyncFaceMaterialToGiantType()
    {
#if UNITY_EDITOR
        var expected = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GetFaceMaterialAssetPath());
#else
        var expected = Resources.Load<Material>(GetFaceMaterialResourceName());
#endif
        if (expected != null && faceMaterial != expected)
            faceMaterial = expected;
    }

    bool AssignDefaultAssets()
    {
        var changed = false;
        SyncFaceMaterialToGiantType();

#if UNITY_EDITOR
        EnsureCorrectFaceMaterial();

        if (faceMaterial == null)
        {
            faceMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GetFaceMaterialAssetPath());
            changed |= faceMaterial != null;
        }

        if (!UsesGrandmaStylePresentation() && hippieBodyMaterial == null)
        {
            hippieBodyMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/SimpleCitizens/Materials/SimpleCitizens_Hippie_Black.mat");
            changed |= hippieBodyMaterial != null;
        }

        if (changed)
            UnityEditor.EditorUtility.SetDirty(this);
#else
        if (faceMaterial == null)
            faceMaterial = Resources.Load<Material>(GetFaceMaterialResourceName());

        if (!UsesGrandmaStylePresentation() && hippieBodyMaterial == null)
            hippieBodyMaterial = Resources.Load<Material>(HippieMaterialResource);
#endif

        return changed;
    }

    void ResolveHeadBone()
    {
        headBone = null;
        foreach (var bone in GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != HeadBoneName)
                continue;

            headBone = bone;
            return;
        }
    }

    void ApplyHippieBodyMaterial()
    {
        var hippieRenderer = FindHippieRenderer();
        if (hippieRenderer == null || hippieBodyMaterial == null)
            return;

        var mat = Application.isPlaying
            ? GetOrCreateRuntimeBodyMaterial()
            : hippieBodyMaterial;

        var slots = hippieRenderer.sharedMaterials;
        if (slots == null || slots.Length == 0)
            slots = new Material[2];

        for (var i = 0; i < slots.Length; i++)
            slots[i] = mat;

        if (Application.isPlaying)
            hippieRenderer.materials = slots;
        else
            hippieRenderer.sharedMaterials = slots;
    }

    Material GetOrCreateRuntimeBodyMaterial()
    {
        if (runtimeBodyMaterial != null)
            return runtimeBodyMaterial;

        runtimeBodyMaterial = new Material(hippieBodyMaterial);
        runtimeBodyMaterial.name = "GiantHippieBody_PlayInstance";
        return runtimeBodyMaterial;
    }

    void EnsureBossFaceBillboard()
    {
        if (headBone == null)
            return;

        faceTransform = headBone.Find(BossFaceObjectName);
        if (faceTransform == null)
        {
            var go = new GameObject(BossFaceObjectName);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Boss Face Billboard");
#endif
            faceTransform = go.transform;
            faceTransform.SetParent(headBone, false);
        }

        faceTransform.gameObject.SetActive(true);
        faceTransform.localPosition = faceLocalOffset;
        faceTransform.localRotation = Quaternion.identity;
        faceTransform.localScale = new Vector3(faceSize.x * faceWidthStretch, faceSize.y, 1f);

        var meshFilter = faceTransform.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = faceTransform.gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = GetQuadMesh();

        var meshRenderer = faceTransform.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = faceTransform.gameObject.AddComponent<MeshRenderer>();

        var mat = Application.isPlaying
            ? GetOrCreateRuntimeFaceMaterial()
            : faceMaterial;

        if (mat != null)
        {
            if (Application.isPlaying)
                meshRenderer.material = mat;
            else
                meshRenderer.sharedMaterial = mat;
        }
        else if (Application.isPlaying && !loggedPlayFaceWarning)
        {
            loggedPlayFaceWarning = true;
            Debug.LogWarning("[Dutz] Boss face material missing in Play — assign GiantHippieBossFace or add texture to Resources.");
        }

        meshRenderer.enabled = true;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    Texture2D LoadFaceTexture()
    {
        if (faceMaterial != null && faceMaterial.mainTexture is Texture2D fromMat)
            return fromMat;

        return Resources.Load<Texture2D>(GetFaceTextureResourceName());
    }

    Material GetOrCreateRuntimeFaceMaterial()
    {
        if (runtimeFaceMaterial != null)
        {
            var tex = LoadFaceTexture();
            if (tex != null)
                runtimeFaceMaterial.mainTexture = tex;
            return runtimeFaceMaterial;
        }

        var source = faceMaterial != null
            ? faceMaterial
            : Resources.Load<Material>(GetFaceMaterialResourceName());

        if (source == null)
            return null;

        runtimeFaceMaterial = new Material(source);
        runtimeFaceMaterial.name = "GiantHippieBossFace_PlayInstance";

        var billboardShader = Shader.Find(FaceShaderName);
        if (billboardShader != null)
            runtimeFaceMaterial.shader = billboardShader;

        var texture = LoadFaceTexture();
        if (texture != null)
            runtimeFaceMaterial.mainTexture = texture;

        return runtimeFaceMaterial;
    }

    static Mesh GetQuadMesh()
    {
        if (quadMesh != null)
            return quadMesh;

        quadMesh = new Mesh { name = "BossFaceQuad" };
        quadMesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        quadMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quadMesh.RecalculateNormals();
        quadMesh.RecalculateBounds();
        return quadMesh;
    }

    bool IsGiantBodyVisible()
    {
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer != null && renderer.enabled && renderer.isVisible)
                return true;
        }

        return false;
    }

    static Camera GetViewCamera()
    {
        if (Application.isPlaying)
        {
            var main = Camera.main;
            if (main != null)
                return main;

            return Object.FindObjectOfType<Camera>();
        }

#if UNITY_EDITOR
        if (UnityEditor.SceneView.lastActiveSceneView != null)
            return UnityEditor.SceneView.lastActiveSceneView.camera;
#endif

        return Camera.main;
    }

    SkinnedMeshRenderer FindHippieRenderer()
    {
        var meshName = GetBodyMeshName();
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.gameObject.name == meshName)
                return renderer;
        }

        return null;
    }

    void OnDestroy()
    {
        if (runtimeFaceMaterial != null)
            Destroy(runtimeFaceMaterial);

        if (runtimeBodyMaterial != null)
            Destroy(runtimeBodyMaterial);
    }
}

/// <summary>
/// Shortens limbs by scaling child bone localPosition (bone length axis), not localScale.
/// </summary>
public static class DutzGiantHippieCaricatureRig
{
    public const float HeadScale = 2f;

    struct LimbSegment
    {
        public readonly string BoneName;
        public readonly float LengthScale;

        public LimbSegment(string boneName, float lengthScale)
        {
            BoneName = boneName;
            LengthScale = lengthScale;
        }
    }

    static readonly LimbSegment[] LimbSegments =
    {
        new LimbSegment("Forearm_Left_jnt", 0.55f),
        new LimbSegment("Hand_Left_jnt", 0.5f),
        new LimbSegment("Forearm_Right_jnt", 0.55f),
        new LimbSegment("Hand_Right_jnt", 0.5f),
        new LimbSegment("LowerLeg_Left_jnt", 0.6f),
        new LimbSegment("Foot_Left_jnt", 0.55f),
        new LimbSegment("Toe_Left_jnt", 0.55f),
        new LimbSegment("LowerLeg_Right_jnt", 0.6f),
        new LimbSegment("Foot_Right_jnt", 0.55f),
        new LimbSegment("Toe_Right_jnt", 0.55f)
    };

    static readonly string[] ResetScaleBones =
    {
        "Head_jnt",
        "Arm_Left_jnt", "Arm_Right_jnt",
        "Forearm_Left_jnt", "Forearm_Right_jnt",
        "UpperLeg_Left_jnt", "UpperLeg_Right_jnt",
        "LowerLeg_Left_jnt", "LowerLeg_Right_jnt"
    };

    static readonly System.Collections.Generic.Dictionary<string, Vector3> DefaultLocalPositions =
        new System.Collections.Generic.Dictionary<string, Vector3>
        {
            { "Forearm_Left_jnt", new Vector3(0.471188f, 0f, 0f) },
            { "Hand_Left_jnt", new Vector3(0.4403f, 0f, 0f) },
            { "Forearm_Right_jnt", new Vector3(-0.47118998f, 0f, 0f) },
            { "Hand_Right_jnt", new Vector3(-0.4403f, 0f, 0f) },
            { "LowerLeg_Left_jnt", new Vector3(-0.37452164f, 0f, 0f) },
            { "Foot_Left_jnt", new Vector3(-0.38874373f, 0f, 0f) },
            { "Toe_Left_jnt", new Vector3(0f, -0.1497335f, 0.21969701f) },
            { "LowerLeg_Right_jnt", new Vector3(0.374522f, 0f, 0f) },
            { "Foot_Right_jnt", new Vector3(0.388744f, 0f, 0f) },
            { "Toe_Right_jnt", new Vector3(0f, 0.1497333f, -0.21969701f) }
        };

    public static void Apply(GameObject root)
    {
        foreach (var boneName in ResetScaleBones)
            SetBoneScale(root, boneName, Vector3.one);

        foreach (var segment in LimbSegments)
            ShortenBone(root, segment.BoneName, segment.LengthScale);

        SetBoneScale(root, "Head_jnt", Vector3.one * HeadScale);
    }

    static void ShortenBone(GameObject root, string boneName, float lengthScale)
    {
        if (!DefaultLocalPositions.TryGetValue(boneName, out var defaultPos))
            return;

        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != boneName)
                continue;

            bone.localScale = Vector3.one;
            bone.localPosition = defaultPos * lengthScale;
            return;
        }
    }

    static void SetBoneScale(GameObject root, string boneName, Vector3 scale)
    {
        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != boneName)
                continue;

            bone.localScale = scale;
            return;
        }
    }
}
