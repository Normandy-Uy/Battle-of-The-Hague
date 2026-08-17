/// <summary>Level 3 highway giants (segments 1–5) — display names and face asset paths. End boss BEYBI M is separate.</summary>
public static class DutzLevel03TrackGiantFaces
{
    /// <summary>Subfolder under project public/ for track-giant face source photos.</summary>
    public const string PublicFacesFolder = "TRACK_GIANT_FACES";

    public readonly struct Entry
    {
        public readonly string displayName;
        public readonly string sourcePhotoFile;
        public readonly string textureAssetPath;
        public readonly string materialAssetPath;
        public readonly string resourcesPhotoPath;
        public readonly string materialResourceName;
        public readonly string textureResourceName;

        /// <summary>Path relative to public/ (e.g. TRACK_GIANT_FACES/RAPTOR.png).</summary>
        public string PublicRelativePath =>
            string.IsNullOrEmpty(PublicFacesFolder)
                ? sourcePhotoFile
                : PublicFacesFolder + "/" + sourcePhotoFile;

        public Entry(
            string displayName,
            string sourcePhotoFile,
            string textureAssetPath,
            string materialAssetPath,
            string resourcesPhotoPath,
            string materialResourceName,
            string textureResourceName)
        {
            this.displayName = displayName;
            this.sourcePhotoFile = sourcePhotoFile;
            this.textureAssetPath = textureAssetPath;
            this.materialAssetPath = materialAssetPath;
            this.resourcesPhotoPath = resourcesPhotoPath;
            this.materialResourceName = materialResourceName;
            this.textureResourceName = textureResourceName;
        }
    }

    public const int Count = 5;

    static readonly Entry[] Entries =
    {
        new Entry(
            "RAPTOR",
            "RAPTOR.png",
            "Assets/Characters/Level03/Textures/RaptorBossFace.jpg",
            "Assets/Characters/NPCs/Resources/RaptorBossFace.mat",
            "Assets/Characters/NPCs/Resources/RaptorBossFacePhoto.jpg",
            "RaptorBossFace",
            "RaptorBossFacePhoto"),
        new Entry(
            "BOYOYONG",
            "BOYOYONG.png",
            "Assets/Characters/Level03/Textures/BoyoyongBossFace.jpg",
            "Assets/Characters/NPCs/Resources/BoyoyongBossFace.mat",
            "Assets/Characters/NPCs/Resources/BoyoyongBossFacePhoto.jpg",
            "BoyoyongBossFace",
            "BoyoyongBossFacePhoto"),
        new Entry(
            "KIKAY P",
            "KIKAY P.png",
            "Assets/Characters/Level03/Textures/KikayPBossFace.jpg",
            "Assets/Characters/NPCs/Resources/KikayPBossFace.mat",
            "Assets/Characters/NPCs/Resources/KikayPBossFacePhoto.jpg",
            "KikayPBossFace",
            "KikayPBossFacePhoto"),
        new Entry(
            "Lie Fivex",
            "LIE FIVEX.png",
            "Assets/Characters/Level03/Textures/LieFivexBossFace.jpg",
            "Assets/Characters/NPCs/Resources/LieFivexBossFace.mat",
            "Assets/Characters/NPCs/Resources/LieFivexBossFacePhoto.jpg",
            "LieFivexBossFace",
            "LieFivexBossFacePhoto"),
        new Entry(
            "KLARING",
            "KLARING.png",
            "Assets/Characters/Level03/Textures/KlaringBossFace.jpg",
            "Assets/Characters/NPCs/Resources/KlaringBossFace.mat",
            "Assets/Characters/NPCs/Resources/KlaringBossFacePhoto.jpg",
            "KlaringBossFace",
            "KlaringBossFacePhoto"),
    };

    public static string GetDisplayName(int highwayIndex) =>
        highwayIndex >= 0 && highwayIndex < Count ? Entries[highwayIndex].displayName : null;

    public static bool IsTrackGiant(string objectName)
    {
        for (var i = 0; i < Count; i++)
        {
            if (Entries[i].displayName == objectName)
                return true;
        }

        return false;
    }

    public static bool IsLegacyTrackGiantName(string objectName) =>
        objectName.StartsWith("E-TOL (", System.StringComparison.Ordinal);

    public static bool IsAnyTrackGiant(string objectName) =>
        IsTrackGiant(objectName) || IsLegacyTrackGiantName(objectName);

    public static bool TryGetEntry(string objectName, out Entry entry)
    {
        for (var i = 0; i < Count; i++)
        {
            if (Entries[i].displayName != objectName)
                continue;

            entry = Entries[i];
            return true;
        }

        entry = default;
        return false;
    }

    public static bool TryGetEntry(int highwayIndex, out Entry entry)
    {
        if (highwayIndex < 0 || highwayIndex >= Count)
        {
            entry = default;
            return false;
        }

        entry = Entries[highwayIndex];
        return true;
    }

    public static Entry GetEntry(int highwayIndex) => Entries[highwayIndex];
}
