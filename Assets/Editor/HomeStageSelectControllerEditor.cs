using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(HomeStageSelectController))]
public class HomeStageSelectControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Coin Cheat", EditorStyles.boldLabel);

        HomeStageSelectController controller = (HomeStageSelectController)target;
        if (GUILayout.Button("코인 0원으로 초기화"))
        {
            controller.ResetTotalCoinCheat();
            MarkControllerDirty(controller);
        }

        if (GUILayout.Button("코인 100원 추가"))
        {
            controller.AddTotalCoinCheat();
            MarkControllerDirty(controller);
        }
    }

    private static void MarkControllerDirty(HomeStageSelectController controller)
    {
        EditorUtility.SetDirty(controller);
        if (controller.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
    }
}
