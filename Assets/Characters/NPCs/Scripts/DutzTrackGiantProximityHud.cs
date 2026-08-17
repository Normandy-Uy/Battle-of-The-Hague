using System.Collections.Generic;
using UnityEngine;

/// <summary>Level 3 / Level 7 chase giants — HP in upper-right when nearby.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public class DutzTrackGiantProximityHud : MonoBehaviour
{
    const string ManagerName = "DutzTrackGiantProximityHud";
    const string TrackGiantRootName = "DutzLevel03TrackGiants";
    const float ShowRange = 280f;
    const float Margin = 16f;
    const float PanelWidth = 260f;
    const float BarHeight = 22f;
    const float NameHeight = 28f;
    const float PanelSpacing = 8f;
    const float HpTextHeight = 22f;
    const int MobileCollectInterval = 3;

    static DutzTrackGiantProximityHud instance;

    readonly List<(string name, DutzNpcHitPoints hitPoints, float distance, float aheadDot)> inRangeBuffer =
        new List<(string, DutzNpcHitPoints, float, float)>(8);

    readonly List<SimpleCitizensGiantHippieHunter> cachedHunters =
        new List<SimpleCitizensGiantHippieHunter>(12);

    DutzPlayerController player;
    bool huntersCached;
    GUIStyle nameStyle;
    GUIStyle hpStyle;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay && !DutzCollectibleProgress.IsLevel02)
            return;

        if (instance != null)
            return;

        var existing = GameObject.Find(ManagerName);
        if (existing != null)
        {
            instance = existing.GetComponent<DutzTrackGiantProximityHud>();
            if (instance != null)
                return;
        }

        var go = new GameObject(ManagerName);
        instance = go.AddComponent<DutzTrackGiantProximityHud>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CacheTrackGiantsOnce();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void OnGUI()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay && !DutzCollectibleProgress.IsLevel02)
            return;

        if (player == null)
            player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();

        if (player == null || player.ControlsLocked)
            return;

        if (!Application.isMobilePlatform || Time.frameCount % MobileCollectInterval == 0)
            CollectInRangeTrackGiants();

        if (inRangeBuffer.Count == 0)
            return;

        EnsurePanelStyles();

        var y = DutzCollectibleHudDraw.BelowTopRightRowY;
        for (var i = 0; i < inRangeBuffer.Count; i++)
        {
            var entry = inRangeBuffer[i];
            DrawPanel(entry.name, entry.hitPoints, ref y);
            y += PanelSpacing;
        }
    }

    void CacheTrackGiantsOnce()
    {
        if (huntersCached)
            return;

        cachedHunters.Clear();

        var root = GameObject.Find(TrackGiantRootName);
        if (root != null)
        {
            for (var i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i);
                if (child == null)
                    continue;

                var hunter = child.GetComponent<SimpleCitizensGiantHippieHunter>();
                if (hunter != null)
                    cachedHunters.Add(hunter);
            }
        }

        // Level02 / Level07: also include hunters not parented under the track root.
        if (DutzCollectibleProgress.IsLevel02 || DutzCollectibleProgress.IsLevel07)
        {
            foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
            {
                if (hunter != null && !cachedHunters.Contains(hunter))
                    cachedHunters.Add(hunter);
            }
        }

        huntersCached = true;
    }

    void CollectInRangeTrackGiants()
    {
        if (!huntersCached)
            CacheTrackGiantsOnce();

        inRangeBuffer.Clear();

        var playerPos = player.transform.position;
        var forward = player.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            forward.Normalize();
        else
            forward = Vector3.right;

        for (var i = 0; i < cachedHunters.Count; i++)
        {
            var hunter = cachedHunters[i];
            if (hunter == null || !hunter.gameObject.activeInHierarchy)
                continue;

            if (!DutzCollectibleProgress.ShowsProximityHitPoints(hunter.gameObject.name))
                continue;

            var hp = hunter.GetComponent<DutzNpcHitPoints>();
            if (hp == null)
                hp = DutzNpcHitPoints.EnsureOn(hunter.gameObject, DutzNpcHitPoints.TrackEtOlHitPoints);

            if (hp == null || hp.IsDead)
                continue;

            var delta = hunter.transform.position - playerPos;
            delta.y = 0f;
            var distance = delta.magnitude;
            if (distance > ShowRange)
                continue;

            var aheadDot = delta.sqrMagnitude > 0.0001f ? Vector3.Dot(forward, delta / distance) : 1f;
            inRangeBuffer.Add((hunter.gameObject.name, hp, distance, aheadDot));
        }

        inRangeBuffer.Sort((a, b) =>
        {
            var aAhead = a.aheadDot >= -0.15f;
            var bAhead = b.aheadDot >= -0.15f;
            if (aAhead != bAhead)
                return aAhead ? -1 : 1;

            return a.distance.CompareTo(b.distance);
        });
    }

    void EnsurePanelStyles()
    {
        if (nameStyle != null)
            return;

        nameStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(1f, 0.15f, 0.12f) }
        };

        hpStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.9f, 0.95f, 0.85f) }
        };
    }

    void DrawPanel(string giantName, DutzNpcHitPoints hitPoints, ref float y)
    {
        var x = Screen.width - PanelWidth - Margin;
        var nameRect = new Rect(x, y, PanelWidth, NameHeight);
        var barRect = new Rect(x, nameRect.yMax + 2f, PanelWidth, BarHeight);
        var hpRect = new Rect(x, barRect.yMax + 4f, PanelWidth, HpTextHeight);

        var fill = Mathf.Clamp01((float)hitPoints.CurrentHitPoints / hitPoints.MaxHitPoints);
        var fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * fill, barRect.height - 4f);
        var barColor = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.35f, 0.9f, 0.25f), fill);

        GUI.Label(nameRect, giantName.ToUpperInvariant(), nameStyle);
        GUI.Box(barRect, GUIContent.none, GUI.skin.box);
        GUI.color = barColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(hpRect, $"HP {hitPoints.CurrentHitPoints} / {hitPoints.MaxHitPoints}", hpStyle);

        y = hpRect.yMax;
    }
}
