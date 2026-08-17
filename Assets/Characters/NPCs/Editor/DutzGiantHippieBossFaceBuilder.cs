using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes level_5_boss.jpg onto the SC_Hippie head UV tiles for the giant hippie boss.
/// Head tiles are discovered from the SC_Hippie mesh (Head_jnt bone weights), not guessed from the atlas preview.
/// </summary>
public static class DutzGiantHippieBossFaceBuilder
{
    const string BossPhotoPath = "Assets/Characters/NPCs/Textures/GiantHippieBossFace.jpg";
    const string MidBossPhotoPath = "Assets/Characters/NPCs/Textures/GiantHippieBossFaceMid.jpg";
    const string GrandmaBossPhotoPath = "Assets/Characters/NPCs/Textures/GiantHippieBossFaceGrandma.jpg";
    const string GongBongBossPhotoPath = "Assets/Characters/Level02/Textures/GongBongBossFace.jpg";
    const string CawetanBossPhotoPath = "Assets/Characters/Level02/Textures/CawetanBossFace.jpg";
    const string TambyBossPhotoPath = "Assets/Characters/Level02/Textures/TambyBossFace.jpg";
    const string JonremBossPhotoPath = "Assets/Characters/Level02/Textures/JonremBossFace.jpg";
    const string GerbilBossPhotoPath = "Assets/Characters/Level02/Textures/GerbilBossFace.jpg";
    const string JolesBossPhotoPath = "Assets/Characters/Level02/Textures/JolesBossFace.jpg";
    const string BeybiMBossPhotoPath = "Assets/Characters/Level02/Textures/BeybiMBossFace.jpg";
    const string ETolBossPhotoPath = "Assets/Characters/Level02/Textures/ETolBossFace.jpg";
    const string HontavirusBossPhotoPath = "Assets/Characters/Level02/Textures/HontavirusBossFace.jpg";
    const string LengLengLugawBossPhotoPath = "Assets/Characters/Level03/Textures/LengLengLugawBossFace.jpg";
    const string EndBossPhotoSourceFile = "TRILILING.png";
    const string MidBossPhotoSourceFile = "Torre.png";
    const string GrandmaBossPhotoSourceFile = "PRINCESS SARA.png";
    const string GongBongBossPhotoSourceFile = "BONGGO.png";
    const string CawetanBossPhotoSourceFile = "CAWETAN.png";
    const string TambyBossPhotoSourceFile = "TAMBY.png";
    const string JonremBossPhotoSourceFile = "JONREM.png";
    const string GerbilBossPhotoSourceFile = "GERBIL.png";
    const string JolesBossPhotoSourceFile = "JOLES.png";
    const string BeybiMBossPhotoSourceFile = "BBM.png";
    const string ETolBossPhotoSourceFile = "ETOL.png";
    const string HontavirusBossPhotoSourceFile = "HONTAVIRUS.png";
    const string LengLengLugawBossPhotoSourceFile = "LENGLENG.png";
    public const string EndFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFace.mat";
    public const string MidFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFaceMid.mat";
    public const string GrandmaFaceMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFaceGrandma.mat";
    public const string GongBongFaceMaterialPath = "Assets/Characters/NPCs/Resources/GongBongBossFace.mat";
    public const string CawetanFaceMaterialPath = "Assets/Characters/NPCs/Resources/CawetanBossFace.mat";
    public const string TambyFaceMaterialPath = "Assets/Characters/NPCs/Resources/TambyBossFace.mat";
    public const string JonremFaceMaterialPath = "Assets/Characters/NPCs/Resources/JonremBossFace.mat";
    public const string GerbilFaceMaterialPath = "Assets/Characters/NPCs/Resources/GerbilBossFace.mat";
    public const string JolesFaceMaterialPath = "Assets/Characters/NPCs/Resources/JolesBossFace.mat";
    public const string BeybiMFaceMaterialPath = "Assets/Characters/NPCs/Resources/BeybiMBossFace.mat";
    public const string ETolFaceMaterialPath = "Assets/Characters/NPCs/Resources/ETolBossFace.mat";
    public const string HontavirusFaceMaterialPath = "Assets/Characters/NPCs/Resources/HontavirusBossFace.mat";
    public const string LengLengLugawFaceMaterialPath = "Assets/Characters/NPCs/Resources/LengLengLugawBossFace.mat";
    const string EndFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFacePhoto.jpg";
    const string MidFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFacePhotoMid.jpg";
    const string GrandmaFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/GiantHippieBossFacePhotoGrandma.jpg";
    const string GongBongFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/GongBongBossFacePhoto.jpg";
    const string CawetanFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/CawetanBossFacePhoto.jpg";
    const string TambyFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/TambyBossFacePhoto.jpg";
    const string JonremFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/JonremBossFacePhoto.jpg";
    const string GerbilFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/GerbilBossFacePhoto.jpg";
    const string JolesFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/JolesBossFacePhoto.jpg";
    const string BeybiMFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/BeybiMBossFacePhoto.jpg";
    const string ETolFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/ETolBossFacePhoto.jpg";
    const string HontavirusFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/HontavirusBossFacePhoto.jpg";
    const string LengLengLugawFaceResourcesPhotoPath = "Assets/Characters/NPCs/Resources/LengLengLugawBossFacePhoto.jpg";
    const int BossPhotoMaxEdge = 1024;
    const int BossPhotoJpgQuality = 85;
    const string GrandmaGiantName = "SimpleCitizens_Grandma_White";
    const string HippiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";
    const string HippieBodyPath = "Assets/SimpleCitizens/Textures/SimpleCitizens_Hippie_Black.png";
    const string OutputBodyPath = "Assets/Characters/NPCs/Textures/GiantHippieBossBody.png";
    const string OutputMaterialPath = "Assets/Characters/NPCs/Resources/GiantHippieBossBody.mat";
    const string HippieMaterialPath = "Assets/SimpleCitizens/Materials/SimpleCitizens_Hippie_Black.mat";
    const string HippieMeshName = "SC_Hippie";
    const string HeadBoneName = "Head_jnt";

    const int AtlasSize = 512;
    const int TileSize = 64;
    const int MinVertsPerHeadTile = 2;

    public static void RebuildFromMenu()
    {
        var renderer = GetHippieRendererFromPrefab();
        if (renderer == null)
        {
            Debug.LogError("[Dutz] Failed to rebuild boss face texture — missing SC_Hippie.");
            return;
        }

        var layout = ComputeCompactFaceLayout(renderer.sharedMesh, renderer.bones);
        if (layout.blockWidth <= 0)
        {
            Debug.LogError("[Dutz] Failed to resolve front-facing face verts.");
            return;
        }

        if (BuildFrontFaceTilesTexture(renderer.sharedMesh, renderer.bones))
            Debug.Log("[Dutz] Boss face texture rebuilt on front-only tiles.");
        else
            Debug.LogError("[Dutz] Failed to rebuild boss face texture.");
    }

    public static void FullSetupFromMenu() => DutzGiantHippieBossCaricatureBuilder.FullSetupFromMenu();

    public static void SetupGrandmaGiantBossFaceFromMenu()
    {
        SyncGrandmaBossPhotoFromPublic();
        EnsureGrandmaBossFaceMaterial();

        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Princess Zara giant not found in scene.");
            return;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, true);
        face.ApplyFace();
        DutzGiantWorldDialogBuilder.SetupGrandmaDialog(saveScene: false);
        DutzGiantWorldDialogBuilder.ApplyGrandmaStationary(giant);
        EditorUtility.SetDirty(giant);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Dutz] Grandma giant boss face + dialog applied (PRINCESS SARA photo + plea billboard).");
    }

    public const float FacePortraitFill = 0.9f;
    /// <summary>Skip top of source photo (black hair) when building face atlas slices.</summary>
    public const float FaceCropTopTrim = 0.26f;
    /// <summary>Skip bottom of source photo (suit/collar) when building face atlas slices.</summary>
    public const float FaceCropBottomTrim = 0.20f;
    public const float FrontNormalThreshold = 0.25f;
    public const int MaxFrontFaceColumnX = 320;
    public const int MinFrontVertsForPortrait = 18;

    public readonly struct CompactFaceLayout
    {
        public readonly int destX;
        public readonly int destY;
        public readonly int blockWidth;
        public readonly int blockHeight;
        public readonly List<int> xKeys;
        public readonly List<int> yKeys;

        public CompactFaceLayout(int destX, int destY, int blockWidth, int blockHeight, List<int> xKeys, List<int> yKeys)
        {
            this.destX = destX;
            this.destY = destY;
            this.blockWidth = blockWidth;
            this.blockHeight = blockHeight;
            this.xKeys = xKeys;
            this.yKeys = yKeys;
        }
    }

    public static void AnalyzeAtlasOverlapFromMenu()
    {
        var renderer = GetHippieRendererFromPrefab();
        if (renderer == null)
            return;

        var mesh = renderer.sharedMesh;
        var bones = renderer.bones;
        var frontIndices = CollectFrontFacingHeadVertices(mesh, bones);
        CacheHeadNeckBoneIndices(bones);
        var headExclusive = GetHeadExclusiveAtlasTiles(mesh, bones);
        var paintTiles = GetFacePaintTiles(mesh, bones);
        var shared = frontIndices.Count > 0 ? CountSharedFaceTiles(mesh, frontIndices) : 0;
        Debug.Log("[Dutz] === Atlas UV Analysis ===");
        Debug.Log("[Dutz] Front verts: " + frontIndices.Count + ", head-only atlas tiles: " + headExclusive.Count +
                  ", paint tiles: " + paintTiles.Length + ", front/body shared: " + shared);
        if (paintTiles.Length > 0)
            Debug.Log("[Dutz] Paint tiles: " + string.Join(", ", paintTiles.Select(t => "(" + t.x + "," + t.y + ")")));
        if (headExclusive.Count > 0)
            Debug.Log("[Dutz] Head-only tiles: " + string.Join(", ", headExclusive.OrderBy(t => t.y).ThenBy(t => t.x).Select(t => "(" + t.x + "," + t.y + ")")));

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        if (headTiles != null && headTiles.Length > 0)
        {
            var expanded = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
            if (expanded != null && TryFindUnusedAtlasBlock(mesh, expanded, out var origin, out var blockTiles))
                Debug.Log("[Dutz] Unused face block at (" + origin.x + "," + origin.y + ") with " + blockTiles.Length + " tiles.");
            else
                Debug.Log("[Dutz] No unused atlas block large enough for full face sheet.");
        }
    }

    static int CountSharedFaceTiles(Mesh mesh, List<int> frontIndices)
    {
        var frontSet = new HashSet<int>(frontIndices);
        var tileFront = new HashSet<Vector2Int>();
        var tileBody = new HashSet<Vector2Int>();
        var uvs = mesh.uv;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            var tile = UvToTile(uvs[i]);
            if (frontSet.Contains(i))
                tileFront.Add(tile);
            else
                tileBody.Add(tile);
        }

        return tileFront.Count(tile => tileBody.Contains(tile));
    }

    static int CountRectTileOverlap(HashSet<Vector2Int> occupied, int destX, int destY, int blockWidth, int blockHeight)
    {
        var count = 0;
        for (var py = destY; py < destY + blockHeight; py += TileSize)
        for (var px = destX; px < destX + blockWidth; px += TileSize)
        {
            if (occupied.Contains(new Vector2Int(px, py)))
                count++;
        }

        return count;
    }

    static HashSet<Vector2Int> GetOccupiedAtlasTiles(Mesh mesh, HashSet<int> frontIndexSet)
    {
        var occupied = new HashSet<Vector2Int>();
        var uvs = mesh.uv;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (frontIndexSet.Contains(i))
                continue;

            occupied.Add(UvToTile(uvs[i]));
        }

        return occupied;
    }

    static bool IsAtlasRectFree(HashSet<Vector2Int> occupied, int destX, int destY, int blockWidth, int blockHeight)
    {
        for (var py = destY; py < destY + blockHeight; py += TileSize)
        for (var px = destX; px < destX + blockWidth; px += TileSize)
        {
            if (occupied.Contains(new Vector2Int(px, py)))
                return false;
        }

        return true;
    }

    static Vector2Int FindSafeAtlasRect(HashSet<Vector2Int> occupied, int blockWidth, int blockHeight)
    {
        for (var destY = AtlasSize - blockHeight; destY >= 0; destY -= TileSize)
        {
            for (var destX = 0; destX <= AtlasSize - blockWidth; destX += TileSize)
            {
                if (IsAtlasRectFree(occupied, destX, destY, blockWidth, blockHeight))
                    return new Vector2Int(destX, destY);
            }
        }

        return new Vector2Int(AtlasSize - blockWidth, AtlasSize - blockHeight);
    }

    public static Vector2Int[] ResolveExpandedFrontFaceTiles(Mesh mesh, Transform[] bones)
    {
        var headTiles = GetHeadTileOrigins(mesh, bones, MinVertsPerHeadTile);
        if (headTiles == null || headTiles.Length == 0)
            return null;

        return GetExpandedFrontFaceTiles(mesh, bones, headTiles);
    }

    public static List<int> CollectFrontFacingHeadVertices(Mesh mesh, Transform[] bones)
    {
        var headBone = FindHeadBoneIndex(bones);
        if (headBone < 0)
            return new List<int>();

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        var frontTiles = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        var tileSet = frontTiles != null && frontTiles.Length > 0
            ? new HashSet<Vector2Int>(frontTiles)
            : null;

        var weights = mesh.boneWeights;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var frontDir = DetectFrontFaceDirection(mesh, headBone, weights, normals);
        var indices = new List<int>();

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(weights[i], headBone))
                continue;

            var tile = UvToTile(uvs[i]);
            if (tile.x > MaxFrontFaceColumnX)
                continue;

            if (tileSet != null && !tileSet.Contains(tile))
                continue;

            if (Vector3.Dot(normals[i].normalized, frontDir) >= FrontNormalThreshold)
                indices.Add(i);
        }

        if (indices.Count >= MinFrontVertsForPortrait || tileSet == null)
            return indices;

        indices.Clear();
        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(weights[i], headBone))
                continue;

            var tile = UvToTile(uvs[i]);
            if (tile.x > MaxFrontFaceColumnX || !tileSet.Contains(tile))
                continue;

            indices.Add(i);
        }

        return indices;
    }

    /// <summary>
    /// Every head-weighted vert on the front face UV sheet — used for boss portrait remap (not normal culling).
    /// </summary>
    public static List<int> CollectFaceSheetHeadVertices(Mesh mesh, Transform[] bones)
    {
        var headBone = FindHeadBoneIndex(bones);
        if (headBone < 0)
            return new List<int>();

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        var faceTiles = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        if (faceTiles == null || faceTiles.Length == 0)
            return new List<int>();

        var tileSet = new HashSet<Vector2Int>(faceTiles);
        var weights = mesh.boneWeights;
        var uvs = mesh.uv;
        var indices = new List<int>();

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(weights[i], headBone))
                continue;

            var tile = UvToTile(uvs[i]);
            if (tile.x > MaxFrontFaceColumnX || !tileSet.Contains(tile))
                continue;

            indices.Add(i);
        }

        return indices;
    }

    static Vector3 DetectFrontFaceDirection(Mesh mesh, int headBone, BoneWeight[] weights, Vector3[] normals)
    {
        var candidates = new[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right, Vector3.up, Vector3.down
        };

        Vector3 bestDir = Vector3.forward;
        var bestCount = 0;

        foreach (var dir in candidates)
        {
            var count = 0;
            for (var i = 0; i < mesh.vertexCount; i++)
            {
                if (!IsWeightedToBone(weights[i], headBone))
                    continue;

                if (Vector3.Dot(normals[i].normalized, dir) >= FrontNormalThreshold)
                    count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                bestDir = dir;
            }
        }

        return bestDir;
    }

    public static CompactFaceLayout ComputeCompactFaceLayout(Mesh mesh, Transform[] bones)
    {
        var frontIndices = CollectFrontFacingHeadVertices(mesh, bones);
        if (frontIndices.Count == 0)
            return default;

        if (!TryResolveFaceAtlasBlock(mesh, bones, out var destOrigin, out var destTiles, out var sourceTiles))
            return default;

        var xKeys = destTiles.Select(tile => tile.x).Distinct().OrderBy(x => x).ToList();
        var yKeys = destTiles.Select(tile => tile.y).Distinct().OrderByDescending(y => y).ToList();
        var blockWidth = xKeys.Count * TileSize;
        var blockHeight = yKeys.Count * TileSize;

        return new CompactFaceLayout(destOrigin.x, destOrigin.y, blockWidth, blockHeight, xKeys, yKeys);
    }

    public static bool TryResolveFaceAtlasBlock(
        Mesh mesh,
        Transform[] bones,
        out Vector2Int destOrigin,
        out Vector2Int[] destTiles,
        out Vector2Int[] sourceTiles)
    {
        destOrigin = default;
        destTiles = null;
        sourceTiles = null;

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        if (headTiles == null || headTiles.Length == 0)
            return false;

        var expanded = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        if (expanded == null || expanded.Length == 0)
            return false;

        sourceTiles = expanded.OrderBy(t => t.y).ThenBy(t => t.x).ToArray();

        if (TryFindUnusedAtlasBlock(mesh, expanded, out destOrigin, out destTiles))
            return true;

        // Paint on the real face UV sheet (multi-row). Face verts stay on these tiles; body overlap avoided via crop.
        destTiles = expanded;
        destOrigin = new Vector2Int(destTiles.Min(t => t.x), destTiles.Max(t => t.y));
        return true;
    }

    /// <summary>
    /// Head-only tiles on the lowest atlas row — safe strip for a full-width portrait without touching torso UVs.
    /// </summary>
    public static Vector2Int[] GetHeadExclusiveBottomRow(Mesh mesh, Transform[] bones)
    {
        var headExclusive = GetHeadExclusiveAtlasTiles(mesh, bones);
        if (headExclusive.Count == 0)
            return System.Array.Empty<Vector2Int>();

        var rowY = headExclusive.Max(t => t.y);
        return headExclusive
            .Where(t => t.y == rowY)
            .OrderBy(t => t.x)
            .ToArray();
    }

    public static bool TryFindUnusedAtlasBlock(
        Mesh mesh,
        Vector2Int[] sourceTiles,
        out Vector2Int destOrigin,
        out Vector2Int[] destTiles)
    {
        destOrigin = default;
        destTiles = null;

        var xKeys = sourceTiles.Select(t => t.x).Distinct().OrderBy(x => x).ToList();
        var yKeys = sourceTiles.Select(t => t.y).Distinct().OrderBy(y => y).ToList();
        var cols = xKeys.Count;
        var rows = yKeys.Count;
        var used = GetAllUsedAtlasTiles(mesh);

        for (var baseY = 0; baseY <= AtlasSize - rows * TileSize; baseY += TileSize)
        {
            for (var baseX = 0; baseX <= AtlasSize - cols * TileSize; baseX += TileSize)
            {
                if (!IsAtlasRectUnused(used, baseX, baseY, cols, rows))
                    continue;

                destOrigin = new Vector2Int(baseX, baseY + (rows - 1) * TileSize);
                destTiles = BuildTileGrid(baseX, baseY, xKeys, yKeys);
                return true;
            }
        }

        return false;
    }

    static bool IsAtlasRectUnused(HashSet<Vector2Int> used, int baseX, int baseY, int cols, int rows)
    {
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                if (used.Contains(new Vector2Int(baseX + col * TileSize, baseY + row * TileSize)))
                    return false;
            }
        }

        return true;
    }

    static HashSet<Vector2Int> GetAllUsedAtlasTiles(Mesh mesh)
    {
        var used = new HashSet<Vector2Int>();
        foreach (var uv in mesh.uv)
            used.Add(UvToTile(uv));
        return used;
    }

    static Vector2Int[] BuildTileGrid(int baseX, int baseY, List<int> xKeys, List<int> yKeys)
    {
        var tiles = new List<Vector2Int>();
        for (var row = 0; row < yKeys.Count; row++)
        {
            for (var col = 0; col < xKeys.Count; col++)
            {
                tiles.Add(new Vector2Int(
                    baseX + col * TileSize,
                    baseY + row * TileSize));
            }
        }

        return tiles.ToArray();
    }

    static Dictionary<Vector2Int, Vector2Int> BuildSourceToDestTileMap(
        Vector2Int[] sourceTiles,
        Vector2Int[] destTiles)
    {
        var xKeys = sourceTiles.Select(t => t.x).Distinct().OrderBy(x => x).ToList();
        var yKeys = sourceTiles.Select(t => t.y).Distinct().OrderBy(y => y).ToList();
        var map = new Dictionary<Vector2Int, Vector2Int>();

        for (var row = 0; row < yKeys.Count; row++)
        {
            for (var col = 0; col < xKeys.Count; col++)
            {
                var source = new Vector2Int(xKeys[col], yKeys[row]);
                var index = row * xKeys.Count + col;
                if (index < destTiles.Length)
                    map[source] = destTiles[index];
            }
        }

        return map;
    }

    static Dictionary<Vector2Int, Vector2Int> BuildCollapsedRowTileMap(
        Vector2Int[] sourceTiles,
        Vector2Int[] destRowTiles)
    {
        var map = new Dictionary<Vector2Int, Vector2Int>();
        if (sourceTiles == null || destRowTiles == null || destRowTiles.Length == 0)
            return map;

        var destXs = destRowTiles.Select(t => t.x).OrderBy(x => x).ToArray();
        var rowY = destRowTiles[0].y;

        foreach (var source in sourceTiles.Distinct())
        {
            var nearestX = destXs.OrderBy(x => Mathf.Abs(x - source.x)).First();
            map[source] = new Vector2Int(nearestX, rowY);
        }

        return map;
    }

    public static void RemapFaceVertsToCompactPortrait(
        Mesh mesh,
        Transform[] bones,
        List<int> frontIndices,
        CompactFaceLayout layout)
    {
        if (frontIndices == null || frontIndices.Count == 0 || layout.xKeys == null || layout.yKeys == null)
            return;

        if (!TryResolveFaceAtlasBlock(mesh, bones, out _, out var destTiles, out _))
            return;

        var paintTiles = new HashSet<Vector2Int>(destTiles);
        var uvs = mesh.uv;
        var inset = (1f - FacePortraitFill) * 0.5f;

        foreach (var index in frontIndices)
        {
            var tile = UvToTile(uvs[index]);
            if (!paintTiles.Contains(tile))
                continue;

            var localU = Mathf.Clamp01((uvs[index].x * AtlasSize - tile.x) / TileSize);
            var localV = Mathf.Clamp01((uvs[index].y * AtlasSize - tile.y) / TileSize);
            localU = inset + localU * FacePortraitFill;
            localV = inset + localV * FacePortraitFill;

            uvs[index] = new Vector2(
                (tile.x + localU * TileSize) / AtlasSize,
                (tile.y + localV * TileSize) / AtlasSize);
        }

        mesh.uv = uvs;
    }

    public static bool BuildFrontFaceTilesTexture(Mesh mesh, Transform[] bones)
    {
        if (!SyncBossPhotoFromPublic())
            return false;

        var frontIndices = CollectFrontFacingHeadVertices(mesh, bones);
        if (frontIndices.Count == 0)
            return false;

        if (!TryResolveFaceAtlasBlock(mesh, bones, out var destOrigin, out var destTiles, out var sourceTiles))
        {
            Debug.LogError("[Dutz] Could not resolve face atlas block for boss portrait.");
            return false;
        }

        var exclusiveTiles = destTiles;

        var hippie = LoadReadableTexture(HippieBodyPath);
        var boss = LoadBossPhotoFromPublic();
        if (hippie == null || boss == null)
            return false;

        var output = new Texture2D(hippie.width, hippie.height, TextureFormat.RGBA32, false);
        output.SetPixels(hippie.GetPixels());

        BlitPortraitOnFaceTiles(output, boss, exclusiveTiles);

        var sampleTile = exclusiveTiles[0];
        var sample = output.GetPixel(sampleTile.x + 32, sampleTile.y + 32);
        var hippieSample = hippie.GetPixel(sampleTile.x + 32, sampleTile.y + 32);
        Debug.Log("[Dutz] Post-blit sample @" + sampleTile + " baked=" + sample + " hippie=" + hippieSample);

        Object.DestroyImmediate(boss);

        output.Apply();
        var png = output.EncodeToPNG();
        Object.DestroyImmediate(output);
        Object.DestroyImmediate(hippie);

        var outputFullPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            OutputBodyPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(outputFullPath, png);

        if (!VerifyBossFaceBakedOnDisk(outputFullPath, exclusiveTiles))
        {
            Debug.LogError("[Dutz] Boss face did not write to face tiles on disk.");
            return false;
        }

        AssetDatabase.ImportAsset(OutputBodyPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh();
        EnsureBodyMaterial();
        AssetDatabase.SaveAssets();

        Debug.Log("[Dutz] Boss portrait on " + exclusiveTiles.Length + " tiles at (" + destOrigin.x + "," +
                  destOrigin.y + ") — " + frontIndices.Count + " front verts remapped; body UVs unchanged.");
        return true;
    }

    public static bool BuildFrontFaceBlockTexture(int tileX, int tileY, int blockWidth, int blockHeight)
    {
        if (!SyncBossPhotoFromPublic())
            return false;

        var hippie = LoadReadableTexture(HippieBodyPath);
        var boss = LoadBossPhotoFromPublic();
        if (hippie == null || boss == null)
            return false;

        var output = new Texture2D(hippie.width, hippie.height, TextureFormat.RGBA32, false);
        output.SetPixels(hippie.GetPixels());

        var faceBlock = CropPortraitForFaceBlock(boss, blockWidth, blockHeight);
        BlitFaceBlock(output, faceBlock, tileX, tileY);
        Object.DestroyImmediate(faceBlock);

        output.Apply();
        var png = output.EncodeToPNG();
        Object.DestroyImmediate(output);
        Object.DestroyImmediate(hippie);
        Object.DestroyImmediate(boss);

        var outputFullPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            OutputBodyPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(outputFullPath, png);

        if (!VerifyBlockBakedOnDisk(outputFullPath, tileX, tileY, blockWidth, blockHeight))
        {
            Debug.LogError("[Dutz] Boss face did not write to PNG block at (" + tileX + "," + tileY + ").");
            return false;
        }

        AssetDatabase.ImportAsset(OutputBodyPath, ImportAssetOptions.ForceUpdate);
        EnsureBodyMaterial();
        AssetDatabase.SaveAssets();

        Debug.Log("[Dutz] level_5_boss face baked on front block (" + tileX + "," + tileY + ") size " +
                  blockWidth + "x" + blockHeight + ".");
        return true;
    }

    public static bool BuildSingleTileBossTexture(int tileX, int tileY) =>
        BuildFrontFaceBlockTexture(tileX, tileY, TileSize, TileSize);

    static bool VerifyBlockBakedOnDisk(string bakedPath, int tileX, int tileY, int blockWidth, int blockHeight)
    {
        var hippie = LoadReadableTexture(HippieBodyPath);
        if (hippie == null || !File.Exists(bakedPath))
            return false;

        var bytes = File.ReadAllBytes(bakedPath);
        var baked = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!baked.LoadImage(bytes))
        {
            Object.DestroyImmediate(hippie);
            Object.DestroyImmediate(baked);
            return false;
        }

        var diff = 0;
        for (var py = 0; py < blockHeight; py++)
        for (var px = 0; px < blockWidth; px++)
        {
            if (baked.GetPixel(tileX + px, tileY + py) != hippie.GetPixel(tileX + px, tileY + py))
                diff++;
        }

        Object.DestroyImmediate(hippie);
        Object.DestroyImmediate(baked);
        return diff > 100;
    }

    public static bool BuildBossBodyTexture()
    {
        if (!SyncBossPhotoFromPublic())
            return false;

        var hippieRenderer = GetHippieRendererFromPrefab();
        if (hippieRenderer == null)
            return false;

        var mesh = hippieRenderer.sharedMesh;
        var bones = hippieRenderer.bones;

        var headTiles = GetHeadTileOrigins(mesh, bones, MinVertsPerHeadTile);
        if (headTiles == null || headTiles.Length == 0)
        {
            Debug.LogError("[Dutz] Could not resolve SC_Hippie head UV tiles.");
            return false;
        }

        var faceTiles = GetFrontFaceTiles(mesh, bones, headTiles);
        if (faceTiles == null || faceTiles.Length == 0)
        {
            Debug.LogError("[Dutz] Could not resolve SC_Hippie front face UV tiles.");
            return false;
        }

        var hippie = LoadReadableTexture(HippieBodyPath);
        var boss = LoadBossPhotoFromPublic();
        if (hippie == null || boss == null)
            return false;

        var minX = headTiles.Min(tile => tile.x);
        var minY = headTiles.Min(tile => tile.y);
        var maxX = headTiles.Max(tile => tile.x) + TileSize;
        var maxY = headTiles.Max(tile => tile.y) + TileSize;
        var masterWidth = maxX - minX;
        var masterHeight = maxY - minY;

        var output = new Texture2D(hippie.width, hippie.height, TextureFormat.RGBA32, false);
        output.SetPixels(hippie.GetPixels());

        // One portrait stretched over the head UV footprint; each tile gets its slice (no repeated mini-faces).
        var masterFace = CropPortraitForFaceBlock(boss, masterWidth, masterHeight);
        foreach (var tile in headTiles)
        {
            var slice = ExtractRegion(masterFace, tile.x - minX, tile.y - minY, TileSize, TileSize);
            BlitFaceBlock(output, slice, tile.x, tile.y);
            Object.DestroyImmediate(slice);
        }

        Object.DestroyImmediate(masterFace);

        output.Apply();

        var png = output.EncodeToPNG();

        var outputFullPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            OutputBodyPath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(outputFullPath, png);

        Object.DestroyImmediate(output);
        Object.DestroyImmediate(hippie);
        Object.DestroyImmediate(boss);

        if (!VerifyBossFaceBakedOnDisk(outputFullPath, headTiles))
        {
            Debug.LogError("[Dutz] Boss face did not write to PNG — check public/level_5_boss.jpg");
            return false;
        }

        AssetDatabase.ImportAsset(OutputBodyPath, ImportAssetOptions.ForceUpdate);
        EnsureBodyMaterial();
        AssetDatabase.SaveAssets();

        Debug.Log("[Dutz] level_5_boss face baked as one portrait sliced across " + headTiles.Length +
                  " head UV tiles; footprint (" + minX + "," + minY + ") size " + masterWidth + "x" + masterHeight +
                  "; front anchor tiles: " + string.Join(", ", faceTiles.Select(t => "(" + t.x + "," + t.y + ")")));
        return true;
    }

    public static Vector2Int[] GetHeadTileOriginsFromMesh()
    {
        var hippieRenderer = GetHippieRendererFromPrefab();
        if (hippieRenderer == null)
            return null;

        return GetHeadTileOrigins(hippieRenderer.sharedMesh, hippieRenderer.bones, MinVertsPerHeadTile);
    }

    public static Vector2Int[] GetFrontFaceTilesFromMesh()
    {
        var hippieRenderer = GetHippieRendererFromPrefab();
        if (hippieRenderer == null)
            return null;

        var headTiles = GetHeadTileOrigins(hippieRenderer.sharedMesh, hippieRenderer.bones, MinVertsPerHeadTile);
        if (headTiles == null || headTiles.Length == 0)
            return null;

        return GetFrontFaceTiles(hippieRenderer.sharedMesh, hippieRenderer.bones, headTiles);
    }

    static SkinnedMeshRenderer GetHippieRendererFromPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return null;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.gameObject.name == HippieMeshName && renderer.sharedMesh != null)
                return renderer;
        }

        Debug.LogError("[Dutz] Missing SC_Hippie mesh on hippie prefab.");
        return null;
    }

    public static Vector2Int[] GetFrontFaceTiles(Mesh mesh, Transform[] bones, Vector2Int[] headTiles)
    {
        var headBone = FindHeadBoneIndex(bones);
        if (headBone < 0)
            return null;

        var tileCounts = new Dictionary<Vector2Int, int>();
        var boneWeights = mesh.boneWeights;
        var uvs = mesh.uv;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(boneWeights[i], headBone))
                continue;

            var tile = UvToTile(uvs[i]);
            if (tileCounts.ContainsKey(tile))
                tileCounts[tile]++;
            else
                tileCounts[tile] = 1;
        }

        var ranked = headTiles
            .Where(tile => tileCounts.ContainsKey(tile))
            .OrderByDescending(tile => tileCounts[tile])
            .ToList();

        if (ranked.Count == 0)
            return null;

        var primary = ranked[0];
        var frontTiles = new List<Vector2Int> { primary };

        foreach (var tile in ranked.Skip(1))
        {
            if (tile.x != primary.x)
                continue;

            if (Mathf.Abs(tile.y - primary.y) == TileSize)
            {
                frontTiles.Add(tile);
                break;
            }
        }

        if (frontTiles.Count == 1 && ranked.Count > 1)
            frontTiles.Add(ranked[1]);

        return frontTiles.ToArray();
    }

    /// <summary>
    /// Expands the seed front tiles into the full vertical face sheet (forehead through jaw).
    /// </summary>
    public static Vector2Int[] GetExpandedFrontFaceTiles(Mesh mesh, Transform[] bones, Vector2Int[] headTiles)
    {
        if (headTiles == null || headTiles.Length == 0)
            return null;

        var seed = GetFrontFaceTiles(mesh, bones, headTiles);
        if (seed == null || seed.Length == 0)
            return null;

        var minY = seed.Min(tile => tile.y);
        var maxY = seed.Max(tile => tile.y) + TileSize;
        const int lowerFaceBandMinY = 320;

        var coreFaceColumns = new HashSet<int> { 64, 128, 256, 320 };
        foreach (var tile in seed)
            coreFaceColumns.Add(tile.x);

        return headTiles
            .Where(tile =>
                coreFaceColumns.Contains(tile.x) &&
                (tile.y >= minY && tile.y <= maxY + TileSize * 2 || tile.y >= lowerFaceBandMinY))
            .Distinct()
            .ToArray();
    }

    static int FindHeadBoneIndex(Transform[] bones)
    {
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == HeadBoneName)
                return i;
        }

        return -1;
    }

    const string NeckBoneName = "Neck_jnt";
    static int _cachedHeadBoneIndex = -1;
    static int _cachedNeckBoneIndex = -1;

    static void CacheHeadNeckBoneIndices(Transform[] bones)
    {
        _cachedHeadBoneIndex = FindHeadBoneIndex(bones);
        _cachedNeckBoneIndex = -1;
        if (bones == null)
            return;

        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == NeckBoneName)
            {
                _cachedNeckBoneIndex = i;
                return;
            }
        }
    }

    /// <summary>
    /// Head-only tiles in the expanded front face sheet — full forehead-to-jaw grid, no torso overlap.
    /// </summary>
    public static Vector2Int[] GetFacePaintTiles(Mesh mesh, Transform[] bones)
    {
        CacheHeadNeckBoneIndices(bones);
        var headExclusive = GetHeadExclusiveAtlasTiles(mesh, bones);
        if (headExclusive.Count == 0)
            return System.Array.Empty<Vector2Int>();

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        if (headTiles == null || headTiles.Length == 0)
            return System.Array.Empty<Vector2Int>();

        var expanded = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        if (expanded == null || expanded.Length == 0)
            return System.Array.Empty<Vector2Int>();

        return expanded
            .Where(tile => headExclusive.Contains(tile))
            .OrderBy(t => t.y)
            .ThenBy(t => t.x)
            .ToArray();
    }

    public static Vector2Int[] GetFrontExclusiveFaceTiles(Mesh mesh, Transform[] bones, List<int> frontIndices)
    {
        var paint = GetFacePaintTiles(mesh, bones);
        if (paint.Length == 0 || frontIndices == null || frontIndices.Count == 0)
            return paint;

        var uvs = mesh.uv;
        var frontTiles = new HashSet<Vector2Int>();
        foreach (var index in frontIndices)
            frontTiles.Add(UvToTile(uvs[index]));

        return paint.Where(tile => frontTiles.Contains(tile)).ToArray();
    }

    /// <summary>Front, left, and right head UV columns — one tile group per visible side; rear excluded.</summary>
    public static List<Vector2Int[]> GetThreeSideHeadFaceTileGroups(Mesh mesh, Transform[] bones)
    {
        var groups = new List<Vector2Int[]>();
        if (mesh == null || bones == null)
            return groups;

        var headTiles = GetHeadTileOrigins(mesh, bones, 1);
        if (headTiles == null || headTiles.Length == 0)
            return groups;

        var expanded = GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        if (expanded == null || expanded.Length == 0)
            return groups;

        var headExclusive = GetHeadExclusiveAtlasTiles(mesh, bones);
        var paintTiles = expanded.Where(tile => headExclusive.Contains(tile)).ToArray();
        if (paintTiles.Length == 0)
            paintTiles = expanded;

        var headBone = FindHeadBoneIndex(bones);
        if (headBone < 0)
            return groups;

        var weights = mesh.boneWeights;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var frontDir = DetectFrontFaceDirection(mesh, headBone, weights, normals);

        foreach (var columnX in paintTiles.Select(tile => tile.x).Distinct().OrderBy(x => x))
        {
            var columnTiles = paintTiles.Where(tile => tile.x == columnX).ToArray();
            var avgNormal = AverageHeadNormalOnTiles(mesh, headBone, weights, normals, uvs, columnTiles);
            if (avgNormal.sqrMagnitude < 0.0001f)
                continue;

            avgNormal.Normalize();
            if (Vector3.Dot(avgNormal, -frontDir) > 0.45f)
                continue;

            groups.Add(columnTiles);
        }

        return groups;
    }

    static Vector3 AverageHeadNormalOnTiles(
        Mesh mesh,
        int headBone,
        BoneWeight[] weights,
        Vector3[] normals,
        Vector2[] uvs,
        Vector2Int[] tiles)
    {
        var tileSet = new HashSet<Vector2Int>(tiles);
        var sum = Vector3.zero;
        var count = 0;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(weights[i], headBone))
                continue;

            if (!tileSet.Contains(UvToTile(uvs[i])))
                continue;

            sum += normals[i];
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    /// <summary>
    /// Atlas tiles used only by head/neck bones — safe to replace without touching torso or limbs.
    /// </summary>
    public static HashSet<Vector2Int> GetHeadExclusiveAtlasTiles(Mesh mesh, Transform[] bones)
    {
        CacheHeadNeckBoneIndices(bones);
        var tileHead = new HashSet<Vector2Int>();
        var tileOther = new HashSet<Vector2Int>();
        var uvs = mesh.uv;
        var weights = mesh.boneWeights;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            var tile = UvToTile(uvs[i]);
            if (IsPrimarilyHeadOrNeck(weights[i]))
                tileHead.Add(tile);
            else
                tileOther.Add(tile);
        }

        tileHead.ExceptWith(tileOther);
        return tileHead;
    }

    static bool IsPrimarilyHeadOrNeck(BoneWeight weight)
    {
        var headNeck = GetBoneWeightSum(weight, _cachedHeadBoneIndex);
        if (_cachedNeckBoneIndex >= 0)
            headNeck += GetBoneWeightSum(weight, _cachedNeckBoneIndex);

        var total = weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3;
        return total > 0.01f && headNeck >= total * 0.85f;
    }

    static float GetBoneWeightSum(BoneWeight weight, int boneIndex)
    {
        if (boneIndex < 0)
            return 0f;

        var sum = 0f;
        if (weight.boneIndex0 == boneIndex)
            sum += weight.weight0;
        if (weight.boneIndex1 == boneIndex)
            sum += weight.weight1;
        if (weight.boneIndex2 == boneIndex)
            sum += weight.weight2;
        if (weight.boneIndex3 == boneIndex)
            sum += weight.weight3;
        return sum;
    }

    public static void BlitPortraitOnFaceTiles(Texture2D target, Texture2D boss, Vector2Int[] tiles)
    {
        if (tiles == null || tiles.Length == 0)
            return;

        var xKeys = tiles.Select(tile => tile.x).Distinct().OrderBy(x => x).ToList();
        var yKeys = tiles.Select(tile => tile.y).Distinct().OrderByDescending(y => y).ToList();
        var masterW = xKeys.Count * TileSize;
        var masterH = yKeys.Count * TileSize;
        var master = CropPortraitForFaceBlock(boss, masterW, masterH, FacePortraitFill);

        foreach (var tile in tiles)
        {
            var col = xKeys.IndexOf(tile.x);
            var row = yKeys.IndexOf(tile.y);
            var srcRow = (yKeys.Count - 1) - row;
            var slice = ExtractRegion(master, col * TileSize, srcRow * TileSize, TileSize, TileSize);
            BlitFaceBlock(target, slice, tile.x, tile.y);
            Object.DestroyImmediate(slice);
        }

        Object.DestroyImmediate(master);
    }

    public static Texture2D CropPortraitForFaceBlockPublic(Texture2D portrait, int width, int height, float fill = 0.88f) =>
        CropPortraitForFaceBlock(portrait, width, height, fill);

    static void BlitFaceBlock(Texture2D target, Texture2D face, int destX, int destY)
    {
        for (var py = 0; py < face.height; py++)
        for (var px = 0; px < face.width; px++)
            target.SetPixel(destX + px, destY + py, face.GetPixel(px, py));
    }

    static Texture2D ExtractRegion(Texture2D source, int x, int y, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
            result.SetPixel(px, py, source.GetPixel(x + px, y + py));

        result.Apply();
        return result;
    }

    static bool VerifyBossFaceBakedOnDisk(string bakedPath, Vector2Int[] headTiles)
    {
        var hippie = LoadReadableTexture(HippieBodyPath);
        if (hippie == null || !File.Exists(bakedPath))
            return false;

        var bytes = File.ReadAllBytes(bakedPath);
        var baked = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!baked.LoadImage(bytes))
        {
            Object.DestroyImmediate(hippie);
            Object.DestroyImmediate(baked);
            return false;
        }

        var diff = 0;
        foreach (var tile in headTiles)
        {
            for (var py = 0; py < TileSize; py++)
            for (var px = 0; px < TileSize; px++)
            {
                if (baked.GetPixel(tile.x + px, tile.y + py) != hippie.GetPixel(tile.x + px, tile.y + py))
                    diff++;
            }
        }

        Object.DestroyImmediate(hippie);
        Object.DestroyImmediate(baked);
        var ok = diff > headTiles.Length * 64;
        if (!ok)
            Debug.LogWarning("[Dutz] Boss bake verify diff=" + diff + " on " + headTiles.Length + " tiles (need > " +
                             (headTiles.Length * 64) + ").");
        return ok;
    }

    static Texture2D LoadBossPhotoFromPublic() => LoadBossPhotoFromPublicFile(EndBossPhotoSourceFile);

    static Texture2D LoadMidBossPhotoFromPublic() => LoadBossPhotoFromPublicFile(MidBossPhotoSourceFile);

    static Texture2D LoadBossPhotoFromPublicFile(string sourceFile)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        if (!TryResolvePublicBossPhotoPath(projectRoot, sourceFile, out var path))
        {
            Debug.LogError("[Dutz] Missing boss photo: public/" + sourceFile);
            return null;
        }

        var bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Debug.LogError("[Dutz] Failed to decode " + path);
            Object.DestroyImmediate(texture);
            return null;
        }

        return texture;
    }

    static readonly string[] PublicBossPhotoExtensions = { ".png", ".PNG", ".jpg", ".JPG", ".jpeg", ".JPEG" };

    static IEnumerable<string> GetPublicBossPhotoAliases(string baseName)
    {
        if (string.Equals(baseName, "BBB", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "BeybiM", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "Beybi M", System.StringComparison.OrdinalIgnoreCase))
        {
            yield return "BBM.png";
            yield return "BBB.png";
        }

        if (string.Equals(baseName, "Cawetan", System.StringComparison.OrdinalIgnoreCase))
            yield return "CAWETAN.png";

        if (string.Equals(baseName, "Gerbil", System.StringComparison.OrdinalIgnoreCase))
            yield return "GERBIL.png";

        if (string.Equals(baseName, "Lie Fivex", System.StringComparison.OrdinalIgnoreCase))
            yield return "LIE FIVEX.png";

        if (string.Equals(baseName, "level_5_boss", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "Trililing", System.StringComparison.OrdinalIgnoreCase))
        {
            yield return "TRILILING.png";
            yield return "level_5_boss.jpg";
        }

        if (string.Equals(baseName, "Boy Idol", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "BoyIdol", System.StringComparison.OrdinalIgnoreCase))
            yield return "BOY_IDOL.png";

        if (string.Equals(baseName, "I am baby", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "IAmBaby", System.StringComparison.OrdinalIgnoreCase))
            yield return "I_AM_BABY.png";

        if (string.Equals(baseName, "K Bilyar", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "KBilyar", System.StringComparison.OrdinalIgnoreCase))
            yield return "K BILYAR.png";

        if (string.Equals(baseName, "M BILYAR", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "MBilyar", System.StringComparison.OrdinalIgnoreCase))
            yield return "M BILYAR.png";

        if (string.Equals(baseName, "MARKO LEKTA", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "MarkoLekta", System.StringComparison.OrdinalIgnoreCase))
            yield return "MARKO LEKTA.png";

        if (string.Equals(baseName, "Piyaya", System.StringComparison.OrdinalIgnoreCase))
            yield return "PIYAYA.png";

        if (string.Equals(baseName, "STONE", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "Stone", System.StringComparison.OrdinalIgnoreCase))
            yield return "STONE.jpg";

        if (string.Equals(baseName, "Liron Sinta", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(baseName, "LironSinta", System.StringComparison.OrdinalIgnoreCase))
            yield return "LIRON_SINTA.jpg";
    }

    static void AddPublicBossPhotoCandidate(List<string> candidates, string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i], path, System.StringComparison.OrdinalIgnoreCase))
                return;
        }

        candidates.Add(path);
    }

    static void AddPublicBossPhotoCandidatesInDir(List<string> candidates, string directory, string fileName)
    {
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName) || !Directory.Exists(directory))
            return;

        AddPublicBossPhotoCandidate(candidates, Path.Combine(directory, fileName));

        try
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                if (string.Equals(Path.GetFileName(path), fileName, System.StringComparison.OrdinalIgnoreCase))
                    AddPublicBossPhotoCandidate(candidates, path);
            }
        }
        catch (IOException)
        {
            // Ignore transient filesystem errors while resolving photos.
        }
    }

    static bool TryResolvePublicBossPhotoPath(string projectRoot, string sourceFile, out string resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrEmpty(projectRoot) || string.IsNullOrEmpty(sourceFile))
            return false;

        var publicRoot = Path.Combine(projectRoot, "public");
        var facesDir = Path.Combine(publicRoot, DutzLevel03TrackGiantFaces.PublicFacesFolder);
        var relative = sourceFile.Replace('/', Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(relative);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var candidates = new List<string>();

        // Prefer TRACK_GIANT_FACES (new uploads), then public root / exact relative.
        AddPublicBossPhotoCandidatesInDir(candidates, facesDir, fileName);
        foreach (var ext in PublicBossPhotoExtensions)
            AddPublicBossPhotoCandidatesInDir(candidates, facesDir, baseName + ext);
        foreach (var alias in GetPublicBossPhotoAliases(baseName))
            AddPublicBossPhotoCandidatesInDir(candidates, facesDir, alias);

        AddPublicBossPhotoCandidate(candidates, Path.Combine(publicRoot, relative));
        AddPublicBossPhotoCandidatesInDir(candidates, publicRoot, fileName);
        foreach (var ext in PublicBossPhotoExtensions)
            AddPublicBossPhotoCandidatesInDir(candidates, publicRoot, baseName + ext);
        foreach (var alias in GetPublicBossPhotoAliases(baseName))
            AddPublicBossPhotoCandidatesInDir(candidates, publicRoot, alias);

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!File.Exists(candidates[i]))
                continue;

            resolvedPath = candidates[i];
            return true;
        }

        return false;
    }

    public static Vector2Int[] GetHeadTileOrigins(Mesh mesh, Transform[] bones, int minVertsPerTile)
    {
        var headBone = FindHeadBoneIndex(bones);
        if (headBone < 0)
        {
            Debug.LogError("[Dutz] Head_jnt bone not found on SC_Hippie renderer.");
            return null;
        }

        var tileCounts = new Dictionary<Vector2Int, int>();
        var boneWeights = mesh.boneWeights;
        var uvs = mesh.uv;

        for (var i = 0; i < mesh.vertexCount; i++)
        {
            if (!IsWeightedToBone(boneWeights[i], headBone))
                continue;

            var tile = UvToTile(uvs[i]);
            if (tileCounts.ContainsKey(tile))
                tileCounts[tile]++;
            else
                tileCounts[tile] = 1;
        }

        return tileCounts
            .Where(pair => pair.Value >= minVertsPerTile)
            .OrderByDescending(pair => pair.Value)
            .Select(pair => pair.Key)
            .ToArray();
    }

    static Vector2Int UvToTile(Vector2 uv)
    {
        var x = Mathf.Clamp(Mathf.FloorToInt(uv.x * AtlasSize / TileSize), 0, (AtlasSize / TileSize) - 1);
        var y = Mathf.Clamp(Mathf.FloorToInt(uv.y * AtlasSize / TileSize), 0, (AtlasSize / TileSize) - 1);
        return new Vector2Int(x * TileSize, y * TileSize);
    }

    static bool IsWeightedToBone(BoneWeight weight, int boneIndex) =>
        weight.boneIndex0 == boneIndex || weight.boneIndex1 == boneIndex ||
        weight.boneIndex2 == boneIndex || weight.boneIndex3 == boneIndex;

    public static void SyncBossPhotoFromPublicMenu()
    {
        if (SyncBossPhotoFromPublic())
            Debug.Log("[Dutz] Synced public/level_5_boss.jpg -> GiantHippieBossFace.jpg (end giant).");
    }

    public static void SyncMidBossPhotoFromPublicMenu()
    {
        if (SyncMidBossPhotoFromPublic())
            Debug.Log("[Dutz] Synced public/Torre.png -> GiantHippieBossFaceMid.jpg (mid giant).");
    }

    public static void SyncGrandmaBossPhotoFromPublicMenu()
    {
        if (SyncGrandmaBossPhotoFromPublic())
            Debug.Log("[Dutz] Synced public/PRINCESS SARA.png -> GiantHippieBossFaceGrandma.jpg (grandma giant).");
    }

    public static void SyncAllBossPhotosFromMenu()
    {
        var synced = 0;
        if (SyncBossPhotoFromPublic(force: true)) synced++;
        if (SyncMidBossPhotoFromPublic(force: true)) synced++;
        if (SyncGrandmaBossPhotoFromPublic(force: true)) synced++;
        if (SyncGongBongPhotoFromPublic(force: true)) synced++;
        if (SyncCawetanPhotoFromPublic(force: true)) synced++;
        if (SyncTambyPhotoFromPublic(force: true)) synced++;
        if (SyncJonremPhotoFromPublic(force: true)) synced++;
        if (SyncGerbilPhotoFromPublic(force: true)) synced++;
        if (SyncJolesPhotoFromPublic(force: true)) synced++;
        if (SyncBeybiMPhotoFromPublic(force: true)) synced++;
        if (SyncETolPhotoFromPublic(force: true)) synced++;
        if (SyncHontavirusPhotoFromPublic(force: true)) synced++;
        if (SyncLengLengLugawPhotoFromPublic(force: true)) synced++;
        SyncAllTrackGiantPhotosFromPublic(force: true);
        synced += DutzLevel03TrackGiantFaces.Count;
        synced += SyncAllLevel07BossPhotosFromPublic(force: true);
        ReapplyAllBossPhotoImportSettings();
        Debug.Log(
            $"[Dutz] Synced {synced} giant boss photo(s) from public/ " +
            $"(optimized JPG, max {BossPhotoMaxEdge}px, compressed, no mips).");
    }

    [MenuItem("Assets/Dutz Authoring/Refresh All Giant Faces From Public")]
    public static void RefreshAllGiantFacesFromPublicFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Refresh All Giant Faces requires Edit Mode.");
            return;
        }

        RefreshAllGiantFacesFromPublic(log: true);
    }

    [MenuItem("Assets/Dutz Authoring/Apply Synced Giant Faces On All Levels")]
    public static void ApplySyncedGiantFacesOnAllLevelsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Apply Synced Giant Faces requires Edit Mode.");
            return;
        }

        ApplySyncedGiantFacesOnAllLevels(log: true);
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.RefreshAllGiantFacesFromPublicBatch</summary>
    public static void RefreshAllGiantFacesFromPublicBatch() => RefreshAllGiantFacesFromPublic(log: true);

    public static void RefreshAllGiantFacesFromPublic(bool log)
    {
        SyncAllBossPhotosFromMenu();
        // Asset imports can trigger a domain reload that aborts the rest of this call —
        // schedule scene apply on the next editor tick.
        EditorApplication.delayCall += () => ApplySyncedGiantFacesOnAllLevels(log);
    }

    public static void ApplySyncedGiantFacesOnAllLevels(bool log)
    {
        ReapplyAllBossPhotoImportSettings();

        ApplyGongBongFaceOnLevel02(log: false);
        ApplyTambyFaceOnLevel02(log: false);
        ApplyETolFaceOnLevel02(log: false);
        ApplyGerbilFaceOnLevel02(log: false);
        ApplyJolesFaceOnLevel02(log: false);
        ApplyCawetanFaceOnLevel02(log: false);

        ApplyTrackGiantFacesOnLevel03(log: false);
        ApplyBeybiMFaceOnLevel03(log: false);

        EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        EnsureHontavirusBossFaceOnOpenScene(log: false, persistScene: true);
        EnsureLengLengLugawBossFaceOnOpenScene(log: false, persistScene: true);

        DutzLevel07BoyIdolFaceApplier.ApplySilent(log: false);
        DutzLevel07IAmBabyFaceApplier.ApplySilent(log: false);
        DutzLevel07KBilyarFaceApplier.ApplySilent(log: false);
        DutzLevel07MBilyarFaceApplier.ApplySilent(log: false);
        DutzLevel07MarkoLektaFaceApplier.ApplySilent(log: false);
        DutzLevel07PiyayaFaceApplier.ApplySilent(log: false);
        DutzLevel07StoneFaceApplier.ApplySilent(log: false);
        DutzLevel07LironSintaFaceApplier.ApplySilent(log: false);

        // Re-apply shared Level01/02/03 materials onto Level07 giants that reuse them.
        var level07 = EditorSceneManager.OpenScene(
            "Assets/Scenes/Dutz_Level07.unity", OpenSceneMode.Single);
        EnsureBossFacesOnOpenSceneGiants();
        EditorSceneManager.MarkSceneDirty(level07);
        EditorSceneManager.SaveScene(level07);

        var level01 = EditorSceneManager.OpenScene(
            DutzLevel02Setup.Level01ScenePath, OpenSceneMode.Single);
        EnsureBossFacesOnOpenSceneGiants();
        EditorSceneManager.MarkSceneDirty(level01);
        EditorSceneManager.SaveScene(level01);

        var level02 = EditorSceneManager.OpenScene(
            DutzLevel02Setup.Level02ScenePath, OpenSceneMode.Single);
        EnsureBossFacesOnOpenSceneGiants();
        EnsureCawetanBossFaceOnOpenScene(log: false, persistScene: true);
        EditorSceneManager.MarkSceneDirty(level02);
        EditorSceneManager.SaveScene(level02);

        if (log)
            Debug.Log("[Dutz] Applied synced giant faces on Levels 01–03 and 07.");
    }

    static void EnsureBossFacesOnOpenSceneGiants()
    {
        EnsureMidBossFaceMaterial();
        EnsureGrandmaBossFaceMaterial();
        EnsureGongBongBossFaceMaterial();
        EnsureCawetanBossFaceMaterial();
        EnsureTambyBossFaceMaterial();
        EnsureJonremBossFaceMaterial();
        EnsureGerbilBossFaceMaterial();
        EnsureJolesBossFaceMaterial();
        EnsureETolBossFaceMaterial();
        EnsureHontavirusBossFaceMaterial();
        EnsureLengLengLugawBossFaceMaterial();
        EnsureBeybiMBossFaceMaterial();
        EnsureAllTrackGiantFaceMaterials();

        var faces = Object.FindObjectsByType<DutzGiantHippieBossFace>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < faces.Length; i++)
        {
            if (faces[i] == null)
                continue;

            faces[i].ApplyFace();
            EditorUtility.SetDirty(faces[i]);
            EditorUtility.SetDirty(faces[i].gameObject);
        }
    }

    /// <summary>Re-applies GPU-friendly import settings on every boss-face texture (no re-export from public/).</summary>
    public static void ReapplyAllBossPhotoImportSettings()
    {
        ReapplyBossPhotoImportSettings(BossPhotoPath, EndFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(MidBossPhotoPath, MidFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(GrandmaBossPhotoPath, GrandmaFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(GongBongBossPhotoPath, GongBongFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(CawetanBossPhotoPath, CawetanFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(TambyBossPhotoPath, TambyFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(JonremBossPhotoPath, JonremFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(GerbilBossPhotoPath, GerbilFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(JolesBossPhotoPath, JolesFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(BeybiMBossPhotoPath, BeybiMFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(ETolBossPhotoPath, ETolFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(HontavirusBossPhotoPath, HontavirusFaceResourcesPhotoPath);
        ReapplyBossPhotoImportSettings(LengLengLugawBossPhotoPath, LengLengLugawFaceResourcesPhotoPath);

        for (var i = 0; i < DutzLevel03TrackGiantFaces.Count; i++)
        {
            var entry = DutzLevel03TrackGiantFaces.GetEntry(i);
            ReapplyBossPhotoImportSettings(entry.textureAssetPath, entry.resourcesPhotoPath);
        }

        for (var i = 0; i < Level07BossPhotoEntries.Length; i++)
        {
            var entry = Level07BossPhotoEntries[i];
            ReapplyBossPhotoImportSettings(entry.textureAssetPath, entry.resourcesPhotoPath);
        }
    }

    static void ReapplyBossPhotoImportSettings(params string[] assetPaths)
    {
        for (var i = 0; i < assetPaths.Length; i++)
            ApplyBossBillboardImportSettings(assetPaths[i]);
    }

    /// <summary>
    /// One-shot Level 3 photo pass — downscale/compress boss faces, posterize Hague murals, sync jail mural.
    /// Batch: -executeMethod DutzGiantHippieBossFaceBuilder.OptimizeAllLevel03PhotosBatch
    /// </summary>
    public static void OptimizeAllLevel03PhotosBatch() => OptimizeAllLevel03Photos(log: true);

    public static void OptimizeAllLevel03PhotosFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Optimize Photos", "Exit Play mode first.", "OK");
            return;
        }

        OptimizeAllLevel03Photos(log: true);
        EditorUtility.DisplayDialog(
            "Optimize Photos",
            "Level 3 photos optimized.\n\n" +
            $"Boss faces: max {BossPhotoMaxEdge}px JPG, GPU compressed, no mipmaps.\n" +
            "Hague murals: posterized + capped via DutzHighwayPhotoBillboardSettings.\n" +
            "Jail mural: synced from public/DUTZJAIL.png.",
            "OK");
    }

    public static void OptimizeAllLevel03Photos(bool log)
    {
        SyncAllBossPhotosFromMenu();
        DutzHaguePhotoBillboardBuilder.SyncPhotos(log: false);
        DutzJailMuralPlacer.SyncTexture();

        if (log)
        {
            Debug.Log(
                "[Dutz] Level 3 photo optimization complete — boss faces, Hague murals, and jail mural.");
        }
    }

    public static void FixTrililingColliderFromMenu()
    {
        var trililing = DutzGiantBossNames.FindTrililing();
        if (trililing == null)
        {
            Debug.LogError("[Dutz] Trililing not found in the active scene.");
            return;
        }

        DutzHippieBiteCollider.EnsureTrililingSolidCollider(trililing);
        UnityEditor.EditorUtility.SetDirty(trililing);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(trililing.scene);
        Debug.Log("[Dutz] Applied Trililing solid collider (final boss only).");
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.FixTrililingColliderBatch</summary>
    public static void FixTrililingColliderBatch() => FixTrililingColliderFromMenu();

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.SyncAllBossPhotosFromMenu</summary>
    public static void SyncAllBossPhotosBatch() => SyncAllBossPhotosFromMenu();

    public static bool SyncBossPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(EndBossPhotoSourceFile, BossPhotoPath, EndFaceResourcesPhotoPath, force);

    public static bool SyncMidBossPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(MidBossPhotoSourceFile, MidBossPhotoPath, MidFaceResourcesPhotoPath, force);

    public static bool SyncGrandmaBossPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(GrandmaBossPhotoSourceFile, GrandmaBossPhotoPath, GrandmaFaceResourcesPhotoPath, force);

    public static bool SyncGongBongPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(GongBongBossPhotoSourceFile, GongBongBossPhotoPath, GongBongFaceResourcesPhotoPath, force);

    public static bool SyncCawetanPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(CawetanBossPhotoSourceFile, CawetanBossPhotoPath, CawetanFaceResourcesPhotoPath, force);

    public static bool SyncTambyPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(TambyBossPhotoSourceFile, TambyBossPhotoPath, TambyFaceResourcesPhotoPath, force);

    public static bool SyncJonremPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(JonremBossPhotoSourceFile, JonremBossPhotoPath, JonremFaceResourcesPhotoPath, force);

    public static bool SyncGerbilPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(GerbilBossPhotoSourceFile, GerbilBossPhotoPath, GerbilFaceResourcesPhotoPath, force);

    public static bool SyncJolesPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(JolesBossPhotoSourceFile, JolesBossPhotoPath, JolesFaceResourcesPhotoPath, force);

    public static bool SyncETolPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(ETolBossPhotoSourceFile, ETolBossPhotoPath, ETolFaceResourcesPhotoPath, force);

    public static bool SyncHontavirusPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(
            HontavirusBossPhotoSourceFile, HontavirusBossPhotoPath, HontavirusFaceResourcesPhotoPath, force);

    public static bool SyncLengLengLugawPhotoFromPublic(bool force = false)
    {
        EnsureLevel03TextureFolder();
        return SyncBossPhotoFromPublicFile(
            LengLengLugawBossPhotoSourceFile, LengLengLugawBossPhotoPath, LengLengLugawFaceResourcesPhotoPath, force);
    }

    public static bool SyncBeybiMPhotoFromPublic(bool force = false) =>
        SyncBossPhotoFromPublicFile(BeybiMBossPhotoSourceFile, BeybiMBossPhotoPath, BeybiMFaceResourcesPhotoPath, force);

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyBeybiMFaceOnLevel03Batch</summary>
    public static void ApplyBeybiMFaceOnLevel03Batch() => ApplyBeybiMFaceOnLevel03(log: true);

    public static bool ApplyBeybiMFaceOnLevel03(bool log)
    {
        SyncBeybiMPhotoFromPublic();
        EnsureBeybiMBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level03ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = DutzGiantBossNames.FindTrililing();
        if (giant == null)
        {
            Debug.LogError("[Dutz] BEYBI M end boss not found in Dutz_Level03.");
            return false;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] BEYBI M boss face applied on Level 3 (BBM.png).");

        return true;
    }

    public static void SyncAllTrackGiantPhotosFromPublic(bool force = false)
    {
        EnsureLevel03TextureFolder();
        for (var i = 0; i < DutzLevel03TrackGiantFaces.Count; i++)
        {
            var entry = DutzLevel03TrackGiantFaces.GetEntry(i);
            SyncBossPhotoFromPublicFile(entry.PublicRelativePath, entry.textureAssetPath, entry.resourcesPhotoPath, force);
        }
    }

    static readonly (string sourceFile, string textureAssetPath, string resourcesPhotoPath)[] Level07BossPhotoEntries =
    {
        ("BOY_IDOL.png", "Assets/Characters/Level07/Textures/BoyIdolBossFace.jpg", "Assets/Characters/NPCs/Resources/BoyIdolBossFacePhoto.jpg"),
        ("I_AM_BABY.png", "Assets/Characters/Level07/Textures/IAmBabyBossFace.jpg", "Assets/Characters/NPCs/Resources/IAmBabyBossFacePhoto.jpg"),
        ("K BILYAR.png", "Assets/Characters/Level07/Textures/KBilyarBossFace.jpg", "Assets/Characters/NPCs/Resources/KBilyarBossFacePhoto.jpg"),
        ("M BILYAR.png", "Assets/Characters/Level07/Textures/MBilyarBossFace.jpg", "Assets/Characters/NPCs/Resources/MBilyarBossFacePhoto.jpg"),
        ("MARKO LEKTA.png", "Assets/Characters/Level07/Textures/MarkoLektaBossFace.jpg", "Assets/Characters/NPCs/Resources/MarkoLektaBossFacePhoto.jpg"),
        ("PIYAYA.png", "Assets/Characters/Level07/Textures/PiyayaBossFace.jpg", "Assets/Characters/NPCs/Resources/PiyayaBossFacePhoto.jpg"),
        ("STONE.jpg", "Assets/Characters/Level07/Textures/StoneBossFace.jpg", "Assets/Characters/NPCs/Resources/StoneBossFacePhoto.jpg"),
        ("LIRON_SINTA.jpg", "Assets/Characters/Level07/Textures/LironSintaBossFace.jpg", "Assets/Characters/NPCs/Resources/LironSintaBossFacePhoto.jpg"),
    };

    public static int SyncAllLevel07BossPhotosFromPublic(bool force = false)
    {
        EnsureLevel07TextureFolder();
        var synced = 0;
        for (var i = 0; i < Level07BossPhotoEntries.Length; i++)
        {
            var entry = Level07BossPhotoEntries[i];
            if (SyncBossPhotoFromPublicFile(entry.sourceFile, entry.textureAssetPath, entry.resourcesPhotoPath, force))
                synced++;
        }

        return synced;
    }

    static void EnsureLevel07TextureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Characters/Level07"))
            AssetDatabase.CreateFolder("Assets/Characters", "Level07");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/Level07/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/Level07", "Textures");
    }

    public static void EnsureAllTrackGiantFaceMaterials()
    {
        for (var i = 0; i < DutzLevel03TrackGiantFaces.Count; i++)
        {
            var entry = DutzLevel03TrackGiantFaces.GetEntry(i);
            EnsureNamedBossFaceMaterial(
                entry.materialAssetPath, entry.textureAssetPath, entry.resourcesPhotoPath, entry.materialResourceName);
        }
    }

    static void EnsureLevel03TextureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Characters/Level03"))
            AssetDatabase.CreateFolder("Assets/Characters", "Level03");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/Level03/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/Level03", "Textures");
    }

    static void EnsureNamedBossFaceMaterial(
        string materialAssetPath, string textureAssetPath, string resourcesTexturePath, string materialName)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        if (texture == null)
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcesTexturePath);

        if (existing != null)
        {
            if (texture != null && existing.mainTexture != texture)
            {
                existing.mainTexture = texture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || texture == null)
            return;

        var material = new Material(template) { name = materialName };
        material.mainTexture = texture;
        AssetDatabase.CreateAsset(material, materialAssetPath);
        AssetDatabase.SaveAssets();
        ApplyBossBillboardImportSettings(textureAssetPath);
        ApplyBossBillboardImportSettings(resourcesTexturePath);
    }

    public static bool AssignTrackGiantFaceMaterial(DutzGiantHippieBossFace face, int highwayIndex)
    {
        if (face == null || !DutzLevel03TrackGiantFaces.TryGetEntry(highwayIndex, out var entry))
            return false;

        var entryRef = entry;
        EnsureNamedBossFaceMaterial(
            entryRef.materialAssetPath, entryRef.textureAssetPath, entryRef.resourcesPhotoPath, entryRef.materialResourceName);

        var material = AssetDatabase.LoadAssetAtPath<Material>(entryRef.materialAssetPath);
        if (material == null)
        {
            Debug.LogError($"[Dutz] Missing track giant face material: {entryRef.materialAssetPath}");
            return false;
        }

        var so = new SerializedObject(face);
        so.FindProperty("faceMaterial").objectReferenceValue = material;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(face);
        return true;
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyTrackGiantFacesOnLevel03Batch</summary>
    public static void ApplyTrackGiantFacesOnLevel03Batch() => ApplyTrackGiantFacesOnLevel03(log: true);

    public static bool ApplyTrackGiantFacesOnLevel03(bool log)
    {
        SyncAllTrackGiantPhotosFromPublic();
        EnsureAllTrackGiantFaceMaterials();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level03ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giants = CollectLevel03TrackGiants();
        if (giants.Count == 0)
        {
            Debug.LogError("[Dutz] No Level 3 track giants found in Dutz_Level03.");
            return false;
        }

        var applied = 0;
        for (var i = 0; i < giants.Count && i < DutzLevel03TrackGiantFaces.Count; i++)
        {
            var giant = giants[i];
            var displayName = DutzLevel03TrackGiantFaces.GetDisplayName(i);

            if (giant.name != displayName)
            {
                Undo.RecordObject(giant, "Rename Level 3 Track Giant");
                giant.name = displayName;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(giant))
                PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var face = giant.GetComponent<DutzGiantHippieBossFace>();
            if (face == null)
                face = giant.AddComponent<DutzGiantHippieBossFace>();

            if (!AssignTrackGiantFaceMaterial(face, i))
            {
                Debug.LogWarning($"[Dutz] Could not assign face for track giant {displayName}.");
                continue;
            }

            face.ApplyFace();
            DutzLevel03Setup.SnapTrackGiantToRoad(giant);
            EditorUtility.SetDirty(giant);
            applied++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 3 track giant faces applied on {applied} giant(s) " +
                "(RAPTOR, BOYOYONG, KIKAY P, Lie Fivex, KLARING).");
        }

        return applied > 0;
    }

    static List<GameObject> CollectLevel03TrackGiants()
    {
        var giants = new List<GameObject>();
        var root = GameObject.Find("DutzLevel03TrackGiants");
        if (root != null)
        {
            for (var i = 0; i < root.transform.childCount; i++)
                giants.Add(root.transform.GetChild(i).gameObject);
        }
        else
        {
            foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
            {
                if (!DutzLevel03TrackGiantFaces.IsAnyTrackGiant(hunter.gameObject.name)
                    || DutzGiantBossNames.IsLevel03EndBoss(hunter.gameObject.name))
                    continue;

                giants.Add(hunter.gameObject);
            }
        }

        giants.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        return giants;
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyGongBongFaceOnLevel02Batch</summary>
    public static void ApplyGongBongFaceOnLevel02Batch() => ApplyGongBongFaceOnLevel02(log: true);

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyTambyFaceOnLevel02Batch</summary>
    public static void ApplyTambyFaceOnLevel02Batch() => ApplyTambyFaceOnLevel02(log: true);

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyETolFaceOnLevel02Batch</summary>
    public static void ApplyETolFaceOnLevel02Batch() => ApplyETolFaceOnLevel02(log: true);

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyCawetanFaceOnLevel02Batch</summary>
    public static void ApplyCawetanFaceOnLevel02Batch() => ApplyCawetanFaceOnLevel02(log: true);

    public static bool EnsureCawetanBossFaceOnOpenScene(bool log, bool persistScene = true)
    {
        if (EditorApplication.isPlaying)
            return true;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != DutzMobileRuntime.Level02SceneName)
            return false;

        SyncCawetanPhotoFromPublic();
        EnsureCawetanBossFaceMaterial();

        var giant = DutzGiantBossNames.FindCawetan();
        if (giant == null)
        {
            if (log)
                Debug.LogWarning("[Dutz] Cawetan giant not found in open Level 2 scene.");
            return false;
        }

        if (giant.name != DutzGiantBossNames.Cawetan)
            giant.name = DutzGiantBossNames.Cawetan;

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        DutzGiantWorldDialogBuilder.EnsureCawetanDialogOnOpenScene();

        if (persistScene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
            Debug.Log("[Dutz] Cawetan boss face ensured on Level 2 (Cawetan.png, permanent).");

        return true;
    }

    public static bool ApplyCawetanFaceOnLevel02(bool log)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level02ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level02ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        return EnsureCawetanBossFaceOnOpenScene(log);
    }

    public static bool EnsureHontavirusBossFaceOnOpenScene(bool log, bool persistScene = true)
    {
        if (EditorApplication.isPlaying)
            return true;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != DutzMobileRuntime.Level02SceneName
            && scene.name != DutzMobileRuntime.Level03SceneName)
            return false;

        SyncHontavirusPhotoFromPublic();
        EnsureHontavirusBossFaceMaterial();

        if (!ApplyBossFaceToNamedGiant(DutzGiantBossNames.Hontavirus, false, false))
        {
            if (log)
                Debug.LogWarning($"[Dutz] HONTAVIRUS giant not found in open scene ({scene.name}).");
            return false;
        }

        if (persistScene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
            Debug.Log("[Dutz] HONTAVIRUS boss face ensured (HONTAVIRUS.png).");

        return true;
    }

    public static bool EnsureLengLengLugawBossFaceOnOpenScene(bool log, bool persistScene = true)
    {
        if (EditorApplication.isPlaying)
            return true;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != DutzMobileRuntime.Level03SceneName)
            return false;

        SyncLengLengLugawPhotoFromPublic();
        EnsureLengLengLugawBossFaceMaterial();

        if (!ApplyBossFaceToNamedGiant(DutzGiantBossNames.LengLengLugaw, false, false))
        {
            if (log)
                Debug.LogWarning("[Dutz] LENG LENG LUGAW giant not found in open Level 3 scene.");
            return false;
        }

        if (persistScene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
            Debug.Log("[Dutz] LENG LENG LUGAW boss face ensured (LENGLENG.png).");

        return true;
    }

    static bool ApplyBossFaceToNamedGiant(string giantName, bool isMidGiant, bool isGrandmaGiant)
    {
        var giant = GameObject.Find(giantName);
        if (giant == null)
            return false;

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, isMidGiant, isGrandmaGiant);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        return true;
    }

    public static bool ApplyGongBongFaceOnLevel02(bool log)
    {
        SyncGongBongPhotoFromPublic();
        EnsureGongBongBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level01ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level01ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = GameObject.Find(DutzGiantBossNames.GongBong);
        if (giant == null)
        {
            Debug.LogError("[Dutz] Gong Bong giant not found in Dutz_Level01.");
            return false;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, true);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Gong Bong boss face applied on Level 2 (BONGGO.png).");

        return true;
    }

    public static bool ApplyTambyFaceOnLevel02(bool log)
    {
        SyncTambyPhotoFromPublic();
        EnsureTambyBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level01ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level01ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = GameObject.Find(DutzGiantBossNames.Tamby);
        if (giant == null)
            giant = GameObject.Find(DutzGiantBossNames.MartyR);

        if (giant == null)
        {
            Debug.LogError("[Dutz] Tamby giant not found in Dutz_Level01.");
            return false;
        }

        if (giant.name != DutzGiantBossNames.Tamby)
        {
            Undo.RecordObject(giant, "Rename Marty R to Tamby");
            giant.name = DutzGiantBossNames.Tamby;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Tamby boss face applied on Level 2 (TAMBY.png).");

        return true;
    }

    public static bool ApplyETolFaceOnLevel02(bool log)
    {
        SyncETolPhotoFromPublic();
        EnsureETolBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level01ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level01ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = GameObject.Find(DutzGiantBossNames.ETol);
        if (giant == null)
        {
            Debug.LogError("[Dutz] E-TOL giant not found in Dutz_Level01.");
            return false;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] E-TOL boss face applied on Level 2 (ETOL.png).");

        return true;
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyGerbilFaceOnLevel02Batch</summary>
    public static void ApplyGerbilFaceOnLevel02Batch() => ApplyGerbilFaceOnLevel02(log: true);

    public static bool ApplyGerbilFaceOnLevel02(bool log)
    {
        SyncGerbilPhotoFromPublic();
        EnsureGerbilBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level02ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level02ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = DutzGiantBossNames.FindGerbil();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Gerbil giant not found in Dutz_Level02.");
            return false;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Gerbil boss face applied on Level 2 (Gerbil.png).");

        return true;
    }

    /// <summary>Batch: -executeMethod DutzGiantHippieBossFaceBuilder.ApplyJolesFaceOnLevel02Batch</summary>
    public static void ApplyJolesFaceOnLevel02Batch() => ApplyJolesFaceOnLevel02(log: true);

    public static bool ApplyJolesFaceOnLevel02(bool log)
    {
        SyncJolesPhotoFromPublic();
        EnsureJolesBossFaceMaterial();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level02ScenePath)
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                DutzLevel02Setup.Level02ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        var giant = DutzGiantBossNames.FindJoles();
        if (giant == null)
        {
            Debug.LogError("[Dutz] JOLES giant not found in Dutz_Level02.");
            return false;
        }

        if (PrefabUtility.IsPartOfAnyPrefab(giant))
            PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
        EditorUtility.SetDirty(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] JOLES boss face applied on Level 2 (JOLES.png).");

        return true;
    }

    static bool SyncBossPhotoFromPublicFile(
        string sourceFile, string textureAssetPath, string resourcesTexturePath, bool force = false)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[Dutz] Could not resolve project root for boss photo sync.");
            return false;
        }

        if (!TryResolvePublicBossPhotoPath(projectRoot, sourceFile, out var sourcePath))
        {
            Debug.LogError("[Dutz] Missing boss photo source: public/" + sourceFile);
            return false;
        }

        var assetFullPath = Path.Combine(projectRoot, textureAssetPath.Replace('/', Path.DirectorySeparatorChar));
        var resourcesFullPath = Path.Combine(
            projectRoot, resourcesTexturePath.Replace('/', Path.DirectorySeparatorChar));

        if (!force
            && IsBossPhotoDestUpToDate(sourcePath, assetFullPath)
            && IsBossPhotoDestUpToDate(sourcePath, resourcesFullPath))
            return false;

        if (!PrepareBossPhotoForImport(sourcePath, assetFullPath)
            || !PrepareBossPhotoForImport(sourcePath, resourcesFullPath))
        {
            Debug.LogError("[Dutz] Failed to prepare boss photo: " + sourceFile + " <- " + sourcePath);
            return false;
        }

        AssetDatabase.ImportAsset(textureAssetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(resourcesTexturePath, ImportAssetOptions.ForceUpdate);
        ApplyBossBillboardImportSettings(textureAssetPath);
        ApplyBossBillboardImportSettings(resourcesTexturePath);
        EnsureMidBossFaceMaterial();
        EnsureGrandmaBossFaceMaterial();
        EnsureGongBongBossFaceMaterial();
        EnsureCawetanBossFaceMaterial();
        EnsureTambyBossFaceMaterial();
        EnsureJonremBossFaceMaterial();
        EnsureGerbilBossFaceMaterial();
        EnsureJolesBossFaceMaterial();
        EnsureETolBossFaceMaterial();
        EnsureHontavirusBossFaceMaterial();
        EnsureLengLengLugawBossFaceMaterial();
        return true;
    }

    static bool IsBossPhotoDestUpToDate(string sourcePath, string destPath)
    {
        if (!File.Exists(destPath))
            return false;

        return File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destPath);
    }

    static bool PrepareBossPhotoForImport(string sourcePath, string destJpgPath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var longest = Mathf.Max(loaded.width, loaded.height);
        var output = loaded;
        if (longest > BossPhotoMaxEdge)
        {
            var scale = BossPhotoMaxEdge / (float)longest;
            var newW = Mathf.Max(1, Mathf.RoundToInt(loaded.width * scale));
            var newH = Mathf.Max(1, Mathf.RoundToInt(loaded.height * scale));
            output = ResizeTextureBilinear(loaded, newW, newH);
            Object.DestroyImmediate(loaded);
        }

        var jpg = output.EncodeToJPG(BossPhotoJpgQuality);
        Object.DestroyImmediate(output);

        var dir = Path.GetDirectoryName(destJpgPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(destJpgPath, jpg);
        return true;
    }

    static void ApplyBossBillboardImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.maxTextureSize = BossPhotoMaxEdge;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    public static void EnsureGrandmaBossFaceMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(GrandmaFaceMaterialPath) != null)
            return;

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        var grandmaTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrandmaBossPhotoPath);
        if (template == null || grandmaTexture == null)
            return;

        var grandmaMaterial = new Material(template) { name = "GiantHippieBossFaceGrandma" };
        grandmaMaterial.mainTexture = grandmaTexture;
        AssetDatabase.CreateAsset(grandmaMaterial, GrandmaFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureGongBongBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(GongBongFaceMaterialPath);
        var gongBongTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GongBongBossPhotoPath);
        if (gongBongTexture == null)
            gongBongTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GongBongFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (gongBongTexture != null && existing.mainTexture != gongBongTexture)
            {
                existing.mainTexture = gongBongTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || gongBongTexture == null)
            return;

        var gongBongMaterial = new Material(template) { name = "GongBongBossFace" };
        gongBongMaterial.mainTexture = gongBongTexture;
        AssetDatabase.CreateAsset(gongBongMaterial, GongBongFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureCawetanBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(CawetanFaceMaterialPath);
        var cawetanTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CawetanBossPhotoPath);
        if (cawetanTexture == null)
            cawetanTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CawetanFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (cawetanTexture != null && existing.mainTexture != cawetanTexture)
            {
                existing.mainTexture = cawetanTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(GrandmaFaceMaterialPath);
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || cawetanTexture == null)
            return;

        var cawetanMaterial = new Material(template) { name = "CawetanBossFace" };
        cawetanMaterial.mainTexture = cawetanTexture;
        AssetDatabase.CreateAsset(cawetanMaterial, CawetanFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureTambyBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(TambyFaceMaterialPath);
        var tambyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TambyBossPhotoPath);
        if (tambyTexture == null)
            tambyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TambyFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (tambyTexture != null && existing.mainTexture != tambyTexture)
            {
                existing.mainTexture = tambyTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(MidFaceMaterialPath);
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || tambyTexture == null)
            return;

        var tambyMaterial = new Material(template) { name = "TambyBossFace" };
        tambyMaterial.mainTexture = tambyTexture;
        AssetDatabase.CreateAsset(tambyMaterial, TambyFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureJonremBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(JonremFaceMaterialPath);
        var jonremTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(JonremBossPhotoPath);
        if (jonremTexture == null)
            jonremTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(JonremFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (jonremTexture != null && existing.mainTexture != jonremTexture)
            {
                existing.mainTexture = jonremTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(TambyFaceMaterialPath);
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(MidFaceMaterialPath);
        if (template == null || jonremTexture == null)
            return;

        var jonremMaterial = new Material(template) { name = "JonremBossFace" };
        jonremMaterial.mainTexture = jonremTexture;
        AssetDatabase.CreateAsset(jonremMaterial, JonremFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureGerbilBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(GerbilFaceMaterialPath);
        var gerbilTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GerbilBossPhotoPath);
        if (gerbilTexture == null)
            gerbilTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GerbilFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (gerbilTexture != null && existing.mainTexture != gerbilTexture)
            {
                existing.mainTexture = gerbilTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(MidFaceMaterialPath);
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(JonremFaceMaterialPath);
        if (template == null || gerbilTexture == null)
            return;

        var gerbilMaterial = new Material(template) { name = "GerbilBossFace" };
        gerbilMaterial.mainTexture = gerbilTexture;
        AssetDatabase.CreateAsset(gerbilMaterial, GerbilFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureJolesBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(JolesFaceMaterialPath);
        var jolesTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(JolesBossPhotoPath);
        if (jolesTexture == null)
            jolesTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(JolesFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (jolesTexture != null && existing.mainTexture != jolesTexture)
            {
                existing.mainTexture = jolesTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(MidFaceMaterialPath);
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(GerbilFaceMaterialPath);
        if (template == null || jolesTexture == null)
            return;

        var jolesMaterial = new Material(template) { name = "JolesBossFace" };
        jolesMaterial.mainTexture = jolesTexture;
        AssetDatabase.CreateAsset(jolesMaterial, JolesFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureBeybiMBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(BeybiMFaceMaterialPath);
        var beybiTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BeybiMBossPhotoPath);
        if (beybiTexture == null)
            beybiTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BeybiMFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (beybiTexture != null && existing.mainTexture != beybiTexture)
            {
                existing.mainTexture = beybiTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || beybiTexture == null)
            return;

        var beybiMaterial = new Material(template) { name = "BeybiMBossFace" };
        beybiMaterial.mainTexture = beybiTexture;
        AssetDatabase.CreateAsset(beybiMaterial, BeybiMFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureETolBossFaceMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(ETolFaceMaterialPath);
        var etolTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ETolBossPhotoPath);
        if (etolTexture == null)
            etolTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ETolFaceResourcesPhotoPath);

        if (existing != null)
        {
            if (etolTexture != null && existing.mainTexture != etolTexture)
            {
                existing.mainTexture = etolTexture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        if (template == null || etolTexture == null)
            return;

        var etolMaterial = new Material(template) { name = "ETolBossFace" };
        etolMaterial.mainTexture = etolTexture;
        AssetDatabase.CreateAsset(etolMaterial, ETolFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureHontavirusBossFaceMaterial() =>
        EnsureNamedBossFaceMaterial(
            HontavirusFaceMaterialPath,
            HontavirusBossPhotoPath,
            HontavirusFaceResourcesPhotoPath,
            "HontavirusBossFace");

    public static void EnsureLengLengLugawBossFaceMaterial() =>
        EnsureNamedBossFaceMaterial(
            LengLengLugawFaceMaterialPath,
            LengLengLugawBossPhotoPath,
            LengLengLugawFaceResourcesPhotoPath,
            "LengLengLugawBossFace");

    public static void EnsureMidBossFaceMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(MidFaceMaterialPath) != null)
            return;

        var template = AssetDatabase.LoadAssetAtPath<Material>(EndFaceMaterialPath);
        var midTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MidBossPhotoPath);
        if (template == null || midTexture == null)
            return;

        var midMaterial = new Material(template) { name = "GiantHippieBossFaceMid" };
        midMaterial.mainTexture = midTexture;
        AssetDatabase.CreateAsset(midMaterial, MidFaceMaterialPath);
        AssetDatabase.SaveAssets();
    }

    static Texture2D LoadReadableTexture(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[Dutz] Missing texture: " + assetPath);
            return null;
        }

        var previousReadable = importer.isReadable;
        if (!previousReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return null;

        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        copy.SetPixels(texture.GetPixels());
        copy.Apply();

        if (!previousReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return copy;
    }

    static Texture2D CropPortraitForFaceBlock(Texture2D boss, int width, int height, float fill = 0.88f)
    {
        var targetAspect = width / (float)Mathf.Max(height, 1);
        var srcW = boss.width;
        var srcH = boss.height;
        var srcAspect = srcW / (float)Mathf.Max(srcH, 1);

        var topTrim = Mathf.RoundToInt(srcH * FaceCropTopTrim);
        var bottomTrim = Mathf.RoundToInt(srcH * FaceCropBottomTrim);
        var faceBandH = Mathf.Max(1, srcH - topTrim - bottomTrim);

        int cropW;
        int cropH;
        if (srcAspect > targetAspect)
        {
            cropH = faceBandH;
            cropW = Mathf.Clamp(Mathf.RoundToInt(cropH * targetAspect), 1, srcW);
        }
        else
        {
            cropW = srcW;
            cropH = Mathf.Clamp(Mathf.RoundToInt(cropW / targetAspect), 1, faceBandH);
        }

        cropH = Mathf.Min(cropH, faceBandH);
        cropW = Mathf.Min(cropW, srcW);

        var left = Mathf.Clamp((srcW - cropW) / 2, 0, srcW - cropW);
        var srcY = Mathf.Clamp(bottomTrim, 0, Mathf.Max(0, srcH - cropH));

        var cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        cropped.SetPixels(boss.GetPixels(left, srcY, cropW, cropH));
        cropped.Apply();

        var fitW = Mathf.Max(1, Mathf.RoundToInt(width * fill));
        var fitH = Mathf.Max(1, Mathf.RoundToInt(height * fill));
        var portrait = ResizeTextureBilinear(cropped, fitW, fitH);
        Object.DestroyImmediate(cropped);

        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var offX = (width - fitW) / 2;
        var offY = (height - fitH) / 2;
        for (var py = 0; py < fitH; py++)
        for (var px = 0; px < fitW; px++)
            result.SetPixel(offX + px, offY + py, portrait.GetPixel(px, py));

        Object.DestroyImmediate(portrait);
        result.Apply();
        return result;
    }

    static Texture2D ResizeTextureBilinear(Texture2D source, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var srcW = source.width;
        var srcH = source.height;

        for (var y = 0; y < height; y++)
        {
            var v = (y + 0.5f) / height;
            var srcY = v * srcH - 0.5f;
            var y0 = Mathf.Clamp(Mathf.FloorToInt(srcY), 0, srcH - 1);
            var y1 = Mathf.Min(y0 + 1, srcH - 1);
            var fy = srcY - y0;

            for (var x = 0; x < width; x++)
            {
                var u = (x + 0.5f) / width;
                var srcX = u * srcW - 0.5f;
                var x0 = Mathf.Clamp(Mathf.FloorToInt(srcX), 0, srcW - 1);
                var x1 = Mathf.Min(x0 + 1, srcW - 1);
                var fx = srcX - x0;

                var c00 = source.GetPixel(x0, y0);
                var c10 = source.GetPixel(x1, y0);
                var c01 = source.GetPixel(x0, y1);
                var c11 = source.GetPixel(x1, y1);
                result.SetPixel(x, y, Color.Lerp(Color.Lerp(c00, c10, fx), Color.Lerp(c01, c11, fx), fy));
            }
        }

        result.Apply();
        return result;
    }

    static void EnsureBodyMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(OutputMaterialPath);
        var template = AssetDatabase.LoadAssetAtPath<Material>(HippieMaterialPath);
        var bodyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputBodyPath);

        if (material == null)
        {
            material = template != null ? new Material(template) : new Material(Shader.Find("Diffuse"));
            AssetDatabase.CreateAsset(material, OutputMaterialPath);
        }

        if (bodyTexture != null)
            material.mainTexture = bodyTexture;

        EditorUtility.SetDirty(material);
    }
}

/// <summary>
/// Caricature mesh (single face UV tile), bone proportions, and scene apply for the giant hippie boss.
/// </summary>
public static class DutzGiantHippieBossCaricatureBuilder
{
    const string GiantName = "SimpleCitizens_Hippie_Giant";
    const string MidGiantName = "SimpleCitizens_Hippie_Giant_Mid";
    const string HippieMeshName = "SC_Hippie";
    const string HeadBoneName = "Head_jnt";
    const string HippiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";
    const string CaricatureMeshPath = "Assets/Characters/NPCs/Meshes/GiantHippieBossCaricature.asset";

    const int AtlasSize = 512;
    const int TileSize = 64;

    public static void MeshOnlyFromMenu()
    {
        var sourceRenderer = GetHippieRendererFromPrefab();
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
        {
            Debug.LogError("[Dutz] Missing SC_Hippie for caricature mesh.");
            return;
        }

        var faceIndices = DutzGiantHippieBossFaceBuilder.CollectFaceSheetHeadVertices(
            sourceRenderer.sharedMesh, sourceRenderer.bones);
        var layout = DutzGiantHippieBossFaceBuilder.ComputeCompactFaceLayout(
            sourceRenderer.sharedMesh, sourceRenderer.bones);

        if (!BuildCaricatureMeshAsset(sourceRenderer, faceIndices, layout))
        {
            Debug.LogError("[Dutz] Caricature mesh build failed.");
            return;
        }

        foreach (var giant in FindAllGiantHippies())
        {
            ApplyCaricatureToGiant(giant);
            giant.GetComponent<DutzGiantHippieBossFace>()?.ApplyFace();
        }

        if (FindAllGiantHippies().Count > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Dutz] Caricature mesh rebuilt (in-place face UVs).");
    }

    public static void FullSetupFromMenu()
    {
        DutzGiantHippieBossFaceBuilder.SyncBossPhotoFromPublic();
        DutzGiantHippieBossFaceBuilder.SyncMidBossPhotoFromPublic();
        DutzGiantHippieBossFaceBuilder.EnsureMidBossFaceMaterial();

        var giants = FindAllGiantHippies();
        if (giants.Count == 0)
        {
            Debug.LogError("[Dutz] No giant hippie found in scene.");
            return;
        }

        foreach (var giant in giants)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(giant))
                PrefabUtility.UnpackPrefabInstance(giant, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            ApplyCaricatureToGiant(giant);

            var face = giant.GetComponent<DutzGiantHippieBossFace>();
            if (face == null)
                face = giant.AddComponent<DutzGiantHippieBossFace>();

            face.ApplyFace();
            EditorUtility.SetDirty(giant);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Dutz] Giant hippie boss setup complete on " + giants.Count +
                  " giant(s) (end=level_5_boss, mid=Torre billboards + caricature).");
    }

    public static bool BuildCaricatureMeshAsset(
        SkinnedMeshRenderer sourceRenderer,
        List<int> frontIndices,
        DutzGiantHippieBossFaceBuilder.CompactFaceLayout layout)
    {
        if (sourceRenderer == null || sourceRenderer.sharedMesh == null)
            return false;

        var sourceMesh = sourceRenderer.sharedMesh;
        var mesh = Object.Instantiate(sourceMesh);
        mesh.name = "GiantHippieBossCaricature";

        DutzGiantHippieBossFaceBuilder.RemapFaceVertsToCompactPortrait(
            mesh, sourceRenderer.bones, frontIndices, layout);

        EnsureMeshFolder();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(CaricatureMeshPath);
        if (existing == null)
            AssetDatabase.CreateAsset(mesh, CaricatureMeshPath);
        else
        {
            existing.Clear(false);
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            mesh = existing;
        }

        EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        Debug.Log("[Dutz] Caricature mesh: " + frontIndices.Count + " face-sheet verts -> portrait (" + layout.destX +
                  "," + layout.destY + ") " + layout.blockWidth + "x" + layout.blockHeight + ".");
        return true;
    }

    public static bool BuildCaricatureMeshAsset()
    {
        var sourceRenderer = GetHippieRendererFromPrefab();
        if (sourceRenderer == null)
            return false;

        var frontIndices = DutzGiantHippieBossFaceBuilder.CollectFrontFacingHeadVertices(
            sourceRenderer.sharedMesh, sourceRenderer.bones);
        var faceIndices = DutzGiantHippieBossFaceBuilder.CollectFaceSheetHeadVertices(
            sourceRenderer.sharedMesh, sourceRenderer.bones);
        if (faceIndices.Count == 0)
            return false;

        var layout = DutzGiantHippieBossFaceBuilder.ComputeCompactFaceLayout(
            sourceRenderer.sharedMesh, sourceRenderer.bones);
        return BuildCaricatureMeshAsset(sourceRenderer, faceIndices, layout);
    }

    public static bool BuildCaricatureMeshAsset(out HeadPortraitBlock faceBlock)
    {
        faceBlock = default;
        return BuildCaricatureMeshAsset();
    }

    public readonly struct HeadPortraitBlock
    {
        public readonly int x;
        public readonly int y;
        public readonly int width;
        public readonly int height;

        public HeadPortraitBlock(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }

    public static void ApplyCaricatureToGiant(GameObject giant)
    {
        ApplyCaricatureBoneScales(giant);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        AssignBossFaceMaterial(face, DutzGiantBossNames.IsGeneralRook(giant.name));
        face.ApplyFace();
    }

    public static void AssignBossFaceMaterial(DutzGiantHippieBossFace face, bool isMidGiant, bool isGrandmaGiant = false)
    {
        if (face == null)
            return;

        DutzGiantHippieBossFaceBuilder.EnsureMidBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureGrandmaBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureGongBongBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureCawetanBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureTambyBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureJonremBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureGerbilBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureJolesBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureBeybiMBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureETolBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureHontavirusBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureLengLengLugawBossFaceMaterial();
        DutzGiantHippieBossFaceBuilder.EnsureAllTrackGiantFaceMaterials();

        string path = null;
        if (DutzLevel03TrackGiantFaces.TryGetEntry(face.gameObject.name, out var trackEntry))
            path = trackEntry.materialAssetPath;
        else
            path = DutzGiantBossNames.IsGongBong(face.gameObject.name)
            ? DutzGiantHippieBossFaceBuilder.GongBongFaceMaterialPath
            : DutzGiantBossNames.IsCawetan(face.gameObject.name)
                ? DutzGiantHippieBossFaceBuilder.CawetanFaceMaterialPath
            : DutzGiantBossNames.IsTamby(face.gameObject.name)
                ? DutzGiantHippieBossFaceBuilder.TambyFaceMaterialPath
                : DutzGiantBossNames.IsJonrem(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.JonremFaceMaterialPath
                : DutzGiantBossNames.IsGerbil(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.GerbilFaceMaterialPath
                : DutzGiantBossNames.IsJoles(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.JolesFaceMaterialPath
                : DutzGiantBossNames.IsBeybiM(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.BeybiMFaceMaterialPath
                : DutzGiantBossNames.IsETol(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.ETolFaceMaterialPath
                : DutzGiantBossNames.IsHontavirus(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.HontavirusFaceMaterialPath
                : DutzGiantBossNames.IsLengLengLugaw(face.gameObject.name)
                    ? DutzGiantHippieBossFaceBuilder.LengLengLugawFaceMaterialPath
                    : isGrandmaGiant
                        ? DutzGiantHippieBossFaceBuilder.GrandmaFaceMaterialPath
                        : isMidGiant
                            ? DutzGiantHippieBossFaceBuilder.MidFaceMaterialPath
                            : DutzGiantHippieBossFaceBuilder.EndFaceMaterialPath;
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            return;

        var so = new SerializedObject(face);
        so.FindProperty("faceMaterial").objectReferenceValue = material;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void ApplyCaricatureBoneScales(GameObject giant) =>
        DutzGiantHippieCaricatureRig.Apply(giant);

    static void RemoveBossFaceBillboard(GameObject giant) { }

    static void EnsureMeshFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/Characters/NPCs/Meshes"))
            return;

        AssetDatabase.CreateFolder("Assets/Characters/NPCs", "Meshes");
    }

    static SkinnedMeshRenderer GetHippieRendererFromPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return null;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.gameObject.name == HippieMeshName && renderer.sharedMesh != null)
                return renderer;
        }

        Debug.LogError("[Dutz] Missing SC_Hippie on hippie prefab.");
        return null;
    }

    static int FindHeadBoneIndex(Transform[] bones)
    {
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == HeadBoneName)
                return i;
        }

        return -1;
    }

    static bool IsWeightedToBone(BoneWeight weight, int boneIndex) =>
        weight.boneIndex0 == boneIndex || weight.boneIndex1 == boneIndex ||
        weight.boneIndex2 == boneIndex || weight.boneIndex3 == boneIndex;

    static List<GameObject> FindAllGiantHippies()
    {
        var list = new List<GameObject>();
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            CollectGiantHippiesInHierarchy(root, list);

        return list;
    }

    static void CollectGiantHippiesInHierarchy(GameObject go, List<GameObject> list)
    {
        if (DutzGiantBossNames.IsTrililing(go.name) || DutzGiantBossNames.IsMidTrackGiant(go.name))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectGiantHippiesInHierarchy(child.gameObject, list);
    }
}

public static class DutzGiantWorldDialogBuilder
{
    const string GrandmaGiantName = "SimpleCitizens_Grandma_White";
    const string GrandmaDialogObjectName = "GrandmaGiantDialog";
    const string CawetanDialogObjectName = "CawetanGiantDialog";
    const string DialogText = "PLEASE BRING HIM HOME.\nFREE DUTZ.";

    public static void SetupGrandmaDialogFromMenu() => SetupGrandmaDialog(saveScene: true);

    public static void FreezeGrandmaGiantFromMenu()
    {
        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Princess Zara giant not found in scene.");
            return;
        }

        ApplyGrandmaStationary(giant);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Dutz] Grandma giant locked stationary (no movement).");
    }

    public static void ApplyGrandmaStationary(GameObject giant)
    {
        if (giant == null
            || (!DutzGiantBossNames.IsPrincessZara(giant.name) && !DutzGiantBossNames.IsCawetan(giant.name)))
        {
            Debug.LogError("[Dutz] ApplyGrandmaStationary called on wrong object: " +
                           (giant != null ? giant.name : "null"));
            return;
        }

        var animator = giant.GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        var stationary = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (stationary == null)
            stationary = giant.AddComponent<DutzGrandmaGiantStationary>();

        stationary.ApplyStationary();
        EditorUtility.SetDirty(giant);
    }

    public static void SetupGrandmaDialog(bool saveScene)
    {
        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Princess Zara giant not found in scene.");
            return;
        }

        var dialog = DutzGiantWorldDialog.CreateDialogObject(GrandmaDialogObjectName, giant.transform);
        Undo.RegisterCreatedObjectUndo(dialog.gameObject, "Create Grandma Giant Dialog");

        var so = new SerializedObject(dialog);
        so.FindProperty("dialogText").stringValue = DialogText;
        so.FindProperty("anchor").objectReferenceValue = giant.transform;
        so.FindProperty("anchorOffset").vector3Value = new Vector3(0f, 22f, 8f);
        so.FindProperty("fontSize").intValue = 48;
        so.FindProperty("characterSize").floatValue = 0.12f;
        so.ApplyModifiedPropertiesWithoutUndo();

        dialog.ApplyDialog();
        EditorUtility.SetDirty(dialog.gameObject);

        if (saveScene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Dutz] Grandma giant dialog placed near " + GrandmaGiantName + ": \"" +
                  DialogText.Replace('\n', ' ') + "\"");
    }

    public static void SetupCawetanDialog(bool saveScene)
    {
        var giant = DutzGiantBossNames.FindCawetan();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Cawetan giant not found in scene.");
            return;
        }

        ApplyGrandmaStationary(giant);

        var dialog = DutzGiantWorldDialog.CreateDialogObject(CawetanDialogObjectName, giant.transform);
        Undo.RegisterCreatedObjectUndo(dialog.gameObject, "Create Cawetan Giant Dialog");

        var so = new SerializedObject(dialog);
        so.FindProperty("dialogText").stringValue = DialogText;
        so.FindProperty("anchor").objectReferenceValue = giant.transform;
        so.FindProperty("anchorOffset").vector3Value = new Vector3(0f, 22f, 8f);
        so.FindProperty("fontSize").intValue = 48;
        so.FindProperty("characterSize").floatValue = 0.12f;
        so.ApplyModifiedPropertiesWithoutUndo();

        dialog.ApplyDialog();
        EditorUtility.SetDirty(dialog.gameObject);

        if (saveScene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Dutz] Cawetan giant dialog placed near " + giant.name + ": \"" +
                  DialogText.Replace('\n', ' ') + "\"");
    }

    public static void EnsureCawetanDialogOnOpenScene()
    {
        if (EditorApplication.isPlaying)
            return;

        if (SceneManager.GetActiveScene().name != DutzMobileRuntime.Level02SceneName)
            return;

        if (DutzGiantBossNames.FindCawetan() == null)
            return;

        if (GameObject.Find(CawetanDialogObjectName) != null)
            return;

        SetupCawetanDialog(saveScene: true);
    }
}

/// <summary>
/// Duplicates Tamby on Level 1 Highway Straight 2 as JONREM with public/JONREM.png face.
/// </summary>
public static class DutzJonremGiantPlacer
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string SegmentName = "Highway Straight 2";
    const float AlongSegment = 0.42f;
    const float LaneZ = -9f;

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("JONREM Giant", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnLevel01(log: true))
        {
            EditorUtility.DisplayDialog(
                "JONREM Giant",
                "Could not place JONREM.\n\nEnsure Tamby exists on Dutz_Level01 and public/JONREM.png is present.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "JONREM Giant",
                "JONREM placed on Highway Straight 2 with JONREM.png face.",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzJonremGiantPlacer.PlaceOnLevel01Batch</summary>
    public static void PlaceOnLevel01Batch() => PlaceOnLevel01(log: true);

    public static bool PlaceOnLevel01(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level01.unity not found.");
            return false;
        }

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[Dutz] Could not resolve project root for JONREM photo.");
            return false;
        }

        var sourcePath = Path.Combine(projectRoot, "public", "JONREM.png");
        if (!File.Exists(sourcePath))
        {
            Debug.LogError("[Dutz] Missing public/JONREM.png — add the image and run again.");
            return false;
        }

        DutzGiantHippieBossFaceBuilder.SyncJonremPhotoFromPublic();
        DutzGiantHippieBossFaceBuilder.EnsureJonremBossFaceMaterial();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
        {
            scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                Level01ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var template = GameObject.Find(DutzGiantBossNames.Tamby);
        if (template == null)
        {
            Debug.LogError("[Dutz] Tamby giant not found in Dutz_Level01.");
            return false;
        }

        if (!TryGetHighwayTwoPose(template.transform.rotation, out var position, out var rotation))
        {
            Debug.LogError("[Dutz] Could not resolve Highway Straight 2 pose for JONREM.");
            return false;
        }

        RemoveExistingJonrem();

        var copy = Object.Instantiate(template);
        Undo.RegisterCreatedObjectUndo(copy, "Place JONREM Giant");
        copy.name = DutzGiantBossNames.Jonrem;
        copy.transform.SetPositionAndRotation(position, rotation);

        if (PrefabUtility.IsPartOfAnyPrefab(copy))
        {
            PrefabUtility.UnpackPrefabInstance(
                copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        EnableMarchForward(copy);
        ApplyJonremFace(copy);
        SnapGiantToRoad(copy);

        var respawn = copy.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn != null)
            respawn.RecordSpawnPoint();

        EditorUtility.SetDirty(copy);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed JONREM on {SegmentName} at {copy.transform.position} " +
                $"(Tamby duplicate, march forward, JONREM.png face).");
        }

        return true;
    }

    static void RemoveExistingJonrem()
    {
        var existing = GameObject.Find(DutzGiantBossNames.Jonrem);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static bool TryGetHighwayTwoPose(Quaternion facing, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = facing;

        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out var travelForward)
            || travelForward.sqrMagnitude < 0.0001f)
        {
            travelForward = Vector3.right;
        }
        else
        {
            travelForward.y = 0f;
            travelForward.Normalize();
        }

        var segment = GameObject.Find(SegmentName);
        if (segment == null)
            return false;

        var path = DutzHighwayDeckSampler.BuildSegmentPath(segment, SegmentName, spawn, travelForward);
        if (path.Samples == null
            || path.Samples.Count == 0
            || !DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, AlongSegment, out var sample))
        {
            return false;
        }

        position = DutzHighwayDeckSampler.PlaceOnLane(sample, LaneZ, spawn);
        rotation = facing;
        return true;
    }

    static void EnableMarchForward(GameObject giant)
    {
        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            return;

        var so = new SerializedObject(physics);
        so.FindProperty("walkForward").boolValue = true;
        so.FindProperty("lockForwardToHighway").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        physics.Apply();
    }

    static void ApplyJonremFace(GameObject giant)
    {
        DutzGiantHippieBossCaricatureBuilder.ApplyCaricatureBoneScales(giant);

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = giant.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.AssignBossFaceMaterial(face, false, false);
        face.ApplyFace();
    }

    static void SnapGiantToRoad(GameObject giant)
    {
        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.SnapFeetToRoad();
            return;
        }

        Physics.SyncTransforms();
    }
}
