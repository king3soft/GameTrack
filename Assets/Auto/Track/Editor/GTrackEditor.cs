using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameTrackEditor : MonoBehaviour
{
   [MenuItem("GameTrack/Open/PersistentDataPath")]
   public static void OpenPersistentDataPath()
   {
       EditorUtility.RevealInFinder(Application.persistentDataPath);
   }
}
