using UnityEngine;

/// <summary>
/// One-shot HUD warnings when the player comes within range of track bosses.
/// </summary>
[DisallowMultipleComponent]
public class DutzTrackProximityAnnouncements : MonoBehaviour
{
    const string ManagerName = "DutzTrackProximityAnnouncements";

    [SerializeField] float triggerDistance = 250f;
    [SerializeField] float messageDuration = 2.5f;
    [SerializeField] string earlyGiantMessage = "JONREM IS COMING!";
    [SerializeField] string midGiantMessage = "GENERAL ROOK IS COMING!";
    [SerializeField] string endGiantMessage = "TRILILING IS COMING!";
    [SerializeField] string gerbilGiantMessage = "GERBIL IS COMING!";
    [SerializeField] string jolesGiantMessage = "JOLES IS COMING!";

    DutzPlayerController player;
    Transform earlyGiant;
    Transform midGiant;
    Transform endGiant;
    Transform gerbilGiant;
    Transform jolesGiant;
    bool earlyMessageShown;
    bool midMessageShown;
    bool endMessageShown;
    bool gerbilMessageShown;
    bool jolesMessageShown;
    string activeMessage;
    float messageTimeLeft;

    public static void EnsureFromBoot()
    {
        if (DutzCollectibleProgress.IsLevel03Gameplay)
        {
            foreach (var existing in Object.FindObjectsOfType<DutzTrackProximityAnnouncements>())
                Object.Destroy(existing);

            return;
        }

        if (FindObjectOfType<DutzTrackProximityAnnouncements>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzTrackProximityAnnouncements>();
    }

    void Awake()
    {
        ApplySceneMessages();
        player = FindObjectOfType<DutzPlayerController>();
        CacheGiants();
    }

    void ApplySceneMessages()
    {
        if (DutzCollectibleProgress.IsLevel01)
        {
            earlyGiantMessage = "JONREM IS COMING!";
            midGiantMessage = "TAMBY IS COMING!";
            endGiantMessage = "E-TOL IS COMING!";
            gerbilGiantMessage = string.Empty;
            jolesGiantMessage = string.Empty;
            return;
        }

        if (DutzCollectibleProgress.IsLevel02)
        {
            earlyGiantMessage = string.Empty;
            midGiantMessage = "GENERAL ROOK IS COMING!";
            endGiantMessage = "TRILILING IS COMING!";
            gerbilGiantMessage = "GERBIL IS COMING!";
            jolesGiantMessage = "JOLES IS COMING!";
            return;
        }

        earlyGiantMessage = string.Empty;
        midGiantMessage = "GENERAL ROOK IS COMING!";
        endGiantMessage = "TRILILING IS COMING!";
        gerbilGiantMessage = string.Empty;
        jolesGiantMessage = string.Empty;
    }

    void CacheGiants()
    {
        if (DutzCollectibleProgress.IsLevel01)
        {
            var earlyGo = DutzGiantBossNames.FindJonrem();
            earlyGiant = earlyGo != null ? earlyGo.transform : null;
        }
        else
        {
            earlyGiant = null;
        }

        var midGo = DutzGiantBossNames.FindMidTrackGiant();
        midGiant = midGo != null ? midGo.transform : null;

        var endGo = DutzGiantBossNames.FindTrililing();
        endGiant = endGo != null ? endGo.transform : null;

        if (DutzCollectibleProgress.IsLevel02)
        {
            var gerbilGo = DutzGiantBossNames.FindGerbil();
            gerbilGiant = gerbilGo != null ? gerbilGo.transform : null;

            var jolesGo = DutzGiantBossNames.FindJoles();
            jolesGiant = jolesGo != null ? jolesGo.transform : null;
        }
        else
        {
            gerbilGiant = null;
            jolesGiant = null;
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = FindObjectOfType<DutzPlayerController>();
            if (player == null)
                return;
        }

        if (earlyGiant == null || midGiant == null || endGiant == null)
            CacheGiants();

        var playerPos = player.transform.position;
        var startMessageBlocking = DutzLevelObjective.IsStartMessageActive;

        if (!earlyMessageShown && earlyGiant != null &&
            !string.IsNullOrEmpty(earlyGiantMessage) &&
            HorizontalDistance(playerPos, earlyGiant.position) <= triggerDistance)
        {
            if (TryShowGiantMessage(earlyGiantMessage, startMessageBlocking))
                earlyMessageShown = true;
        }

        if (!midMessageShown && midGiant != null &&
            HorizontalDistance(playerPos, midGiant.position) <= triggerDistance)
        {
            if (TryShowGiantMessage(midGiantMessage, startMessageBlocking))
                midMessageShown = true;
        }

        if (!gerbilMessageShown && gerbilGiant != null &&
            !string.IsNullOrEmpty(gerbilGiantMessage) &&
            HorizontalDistance(playerPos, gerbilGiant.position) <= triggerDistance)
        {
            if (TryShowGiantMessage(gerbilGiantMessage, startMessageBlocking))
                gerbilMessageShown = true;
        }

        if (!jolesMessageShown && jolesGiant != null &&
            !string.IsNullOrEmpty(jolesGiantMessage) &&
            HorizontalDistance(playerPos, jolesGiant.position) <= triggerDistance)
        {
            if (TryShowGiantMessage(jolesGiantMessage, startMessageBlocking))
                jolesMessageShown = true;
        }

        if (!endMessageShown && endGiant != null &&
            HorizontalDistance(playerPos, endGiant.position) <= triggerDistance)
        {
            if (TryShowGiantMessage(endGiantMessage, startMessageBlocking))
                endMessageShown = true;
        }

        if (messageTimeLeft > 0f)
            messageTimeLeft -= Time.deltaTime;
    }

    bool TryShowGiantMessage(string message, bool startMessageBlocking)
    {
        if (startMessageBlocking)
            return false;

        ShowMessage(message);
        return true;
    }

    void ShowMessage(string message)
    {
        activeMessage = message;
        messageTimeLeft = messageDuration;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var delta = b - a;
        delta.y = 0f;
        return delta.magnitude;
    }

    void OnGUI()
    {
        if (messageTimeLeft <= 0f || string.IsNullOrEmpty(activeMessage))
            return;

        DutzAnnouncementHud.DrawFlash(
            activeMessage,
            DutzAnnouncementHud.DefaultFlashColor,
            DutzAnnouncementHud.FlashFontSize,
            DutzAnnouncementHud.TrackGiantLine);
    }
}
