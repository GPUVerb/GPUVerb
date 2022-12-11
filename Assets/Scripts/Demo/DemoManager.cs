using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace GPUVerb
{
    public class DemoManager : SingletonBehavior<DemoManager>
    {
        [SerializeField]
        Color m_highlightColor = Color.white;

        string[] m_scenes = null;
        bool m_hidden = false;
        
        List<DSPUploader> m_uploaders = null;
        List<Renderer> m_renderers = null;

        bool m_disableDry = false;
        bool m_objHighlight = false;

        GameObject m_curObj = null;

        List<T> Gather<T>()
        {
            List<T> ret = new List<T>();
            var gameObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var gameObj in gameObjs)
            {
                var comps = gameObj.GetComponentsInChildren<T>();
                foreach (var comp in comps)
                {
                    ret.Add(comp);
                }
            }
            return ret;
        }

        // Start is called before the first frame update
        void Start()
        {
            m_scenes = new string[SceneManager.sceneCountInBuildSettings];
            for(int i=0; i<m_scenes.Length; ++i)
            {
                m_scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            }
            m_uploaders = Gather<DSPUploader>();
            m_renderers = Gather<Renderer>();
            Cursor.visible = true;
        }

        // credit: https://www.sunnyvalleystudio.com/blog/unity-3d-selection-highlight-using-emission
        public void ToggleHighlight(bool val, GameObject gameObj)
        {
            List<Material> mats = new List<Material>();
            
            var renderers = gameObj.GetComponentsInChildren<Renderer>();
            foreach(var renderer in renderers)
            {
                mats.AddRange(renderer.materials);
            }

            if (val)
            {
                foreach (var material in mats)
                {
                    //We need to enable the EMISSION
                    material.EnableKeyword("_EMISSION");
                    //before we can set the color
                    material.SetColor("_EmissionColor", m_highlightColor);
                }
            }
            else
            {
                foreach (var material in mats)
                {
                    //we can just disable the EMISSION
                    //if we don't use emission color anywhere else
                    material.DisableKeyword("_EMISSION");
                }
            }

        }

        void SwitchHighlight(GameObject gameObj)
        {
            if(m_curObj != null)
            {
                ToggleHighlight(false, m_curObj);
            }
            m_curObj = gameObj;
            if(m_curObj != null)
            {
                ToggleHighlight(true, m_curObj);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(m_objHighlight)
            {
                if(Physics.Raycast(new Ray(Listener.Position + new Vector3(0,0.2f,0), Listener.Forward), out var info, 4f))
                {
                    GameObject obj = info.collider.gameObject;
                    if (obj.GetComponentInChildren<FDTDGeometry>())
                    {
                        SwitchHighlight(obj);
                    }
                }
                else
                {
                    SwitchHighlight(null);
                }
            }
            else
            {
                SwitchHighlight(null);
            }
        }

        void DrawObjInfoMenu(GameObject obj)
        {
            if(obj == null)
            {
                return;
            }
            FDTDGeometry geom = obj.GetComponent<FDTDGeometry>();
            if(geom == null)
            {
                return;
            }
            var worldPos = geom.GetComponent<Collider>().bounds.center;

            var position = Camera.main.WorldToScreenPoint(worldPos);
            var text = Enum.GetName(typeof(AbsorptionCoefficient), geom.Absorption);
            var textSize = GUI.skin.label.CalcSize(new GUIContent(text));

            var save = GUI.color;
            GUI.color = Color.red;

            GUILayout.BeginArea(new Rect(position.x, Screen.height - position.y, textSize.x, textSize.y));
            {
                GUILayout.Label(text);
            }
            GUILayout.EndArea();

            GUI.color = save;
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

            m_objHighlight = GUILayout.Toggle(m_objHighlight, "obj info");
            DrawObjInfoMenu(m_curObj);
        }
    }
}