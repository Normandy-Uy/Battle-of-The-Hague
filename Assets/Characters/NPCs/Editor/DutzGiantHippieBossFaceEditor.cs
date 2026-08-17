using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DutzGiantHippieBossFace))]
public class DutzGiantHippieBossFaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var face = (DutzGiantHippieBossFace)target;
        var giantName = face.gameObject.name;
        var isMid = DutzGiantBossNames.IsGeneralRook(giantName);
        var isGrandma = DutzGiantBossNames.IsPrincessZara(giantName);
        var isCawetan = DutzGiantBossNames.IsCawetan(giantName);
        var isGongBong = DutzGiantBossNames.IsGongBong(giantName);
        var isTamby = DutzGiantBossNames.IsTamby(giantName);
        var isJonrem = DutzGiantBossNames.IsJonrem(giantName);
        var isGerbil = DutzGiantBossNames.IsGerbil(giantName);
        var isJoles = DutzGiantBossNames.IsJoles(giantName);
        var isETol = DutzGiantBossNames.IsETol(giantName);
        var isHontavirus = DutzGiantBossNames.IsHontavirus(giantName);
        var isLengLengLugaw = DutzGiantBossNames.IsLengLengLugaw(giantName);
        var source = isLengLengLugaw
            ? "LENG LENG LUGAW source: public/TRACK_GIANT_FACES/LENGLENG.png"
            : isHontavirus
                ? "HONTAVIRUS source: public/TRACK_GIANT_FACES/HONTAVIRUS.png"
            : isCawetan
            ? "Cawetan source: public/TRACK_GIANT_FACES/CAWETAN.png"
            : isGongBong
            ? "Gong Bong source: public/TRACK_GIANT_FACES/BONGGO.png"
            : isTamby
                ? "Tamby source: public/TRACK_GIANT_FACES/TAMBY.png"
                : isJonrem
                    ? "JONREM source: public/TRACK_GIANT_FACES/JONREM.png"
                : isGerbil
                    ? "Gerbil source: public/TRACK_GIANT_FACES/GERBIL.png"
                : isJoles
                    ? "JOLES source: public/TRACK_GIANT_FACES/JOLES.png"
                : isETol
                    ? "E-TOL source: public/TRACK_GIANT_FACES/ETOL.png"
                    : isGrandma
                        ? "Grandma giant source: public/TRACK_GIANT_FACES/PRINCESS SARA.png"
                        : isMid
                            ? "Mid giant source: public/TRACK_GIANT_FACES/Torre.png"
                            : "End giant source: public/TRACK_GIANT_FACES/TRILILING.png";
        EditorGUILayout.HelpBox(
            "Boss photo billboard on Head_jnt — always faces the camera. " + source,
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply Billboard Face"))
            face.ApplyFace();
    }
}
