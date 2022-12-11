using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GPUVerb
{
    public class DemoManager : SingletonBehavior<DemoManager>
    {
        string[] m_scenes = null;
        bool m_hidden = false;
        List<DSPUploader> m_uploaders = new List<DSPUploader>();
        bool m_disableDry = false;


        // Start is called before the first frame update
        void Start()
        {
            m_scenes = new string[SceneManager.sceneCountInBuildSettings];
            for(int i=0; i<m_scenes.Length; ++i)
            {
                m_scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            }


            var gameObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach(var gameObj in gameObjs)
            {
                var uploaders = gameObj.GetComponentsInChildren<DSPUploader>();
                foreach(var uploader in uploaders)
                {
                    m_uploaders.Add(uploader);
                }
            }

            Cursor.visible = true;
        }

        // Update is called once per frame
        void Update()
        {
        }

        void OnGUI()
        {
            using var layout = new GUILayout.VerticalScope(!m_hidden ? "Demo Menu" : "", "window");
            if (!m_hidden)
            {
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    m_hidden = true;
                }
            }
            else
            {
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    m_hidden = false;
                }
            }
            if (m_hidden) return;

            GUILayout.Label("Scenes");
            foreach (var scene in m_scenes)
            {
                if(GUILayout.Button(scene))
                {
                    SceneManager.LoadScene(scene);
                }
            }
            GUILayout.Label("Audio Control");

            if(!m_disableDry)
            {
                if (GUILayout.Button("Disable Dry"))
                {
                    m_disableDry = true;
                    foreach (var uploader in m_uploaders)
                    {
                        uploader.SUPPRESS_DRY_SOUND = true;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Enable Dry"))
                {
                    m_disableDry = false;
                    foreach (var uploader in m_uploaders)
                    {
                        uploader.SUPPRESS_DRY_SOUND = false;
                    }
                }
            }
        }
    }
}