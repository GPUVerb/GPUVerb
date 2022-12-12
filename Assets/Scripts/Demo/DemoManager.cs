using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace GPUVerb
{
    class GUIColorScope : IDisposable
    {
        Color originalBackground;
        public GUIColorScope(Color backgroundColor)
        {
            originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = backgroundColor;
        }
        public void Dispose()
        {
            GUI.color = originalBackground;
        }
    }


    public class DemoManager : SingletonBehavior<DemoManager>
    {
        [SerializeField]
        Color m_highlightColor = Color.white;

        string[] m_scenes = null;
        bool m_hidden = false;
        
        List<DSPUploader> m_uploaders = null;
        List<ReverbWriter> m_reverbs = null;

        FirstPersonLook m_look = null;
        FirstPersonMovement m_move = null;

        bool m_disableDry = false;
        bool m_disableReverb = false;


        bool m_objHighlight = false;

        Vector2 m_scrollPos;

        GameObject m_curObj = null;

        bool m_usingObjMenu = false;

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
            m_reverbs = Gather<ReverbWriter>();
            m_look = Gather<FirstPersonLook>()[0];
            m_move = Gather<FirstPersonMovement>()[0];

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

        bool IsValidObj(GameObject obj, out FDTDGeometry geom, out DSPUploader uploader, out Bounds bounds)
        {
            geom = obj.GetComponentInChildren<FDTDGeometry>();
            uploader = obj.GetComponent<DSPUploader>();
            if (geom) bounds = geom.GetBounds();
            else if (uploader) bounds = obj.GetComponent<Collider>().bounds;
            else bounds = new Bounds();
            return geom || uploader;
        }

        // Update is called once per frame
        void Update()
        {
            if(m_objHighlight)
            {
                // only allow switching selection if:
                // mouse not over the object menu
                // and left mouse button pressed
                if(Input.GetMouseButtonDown(0) && !m_usingObjMenu)
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var info, 20f))
                    {
                        GameObject obj = info.collider.gameObject;
                        if (IsValidObj(obj, out var _, out var _, out var _))
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
                    // do nothing, keep current selection
                }
            }
            else
            {
                SwitchHighlight(null);
            }

            if(Input.GetKeyDown(KeyCode.LeftAlt))
            {
                m_objHighlight = !m_objHighlight;
                m_move.EnableControl = m_look.EnableControl = !m_objHighlight;

                if(m_look.EnableControl)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                }
            }
        }

        void DrawGeomMenu(GameObject obj, FDTDGeometry geom)
        {
            GUIStyle style = new GUIStyle() { fontSize = 15 };
            style.normal.textColor = Color.red;
            GUILayout.Label("Current: " + Enum.GetName(typeof(AbsorptionCoefficient), geom.Absorption), style);

            if (GUILayout.Button("Delete Geometry"))
            {
                Destroy(obj);
            }

            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos, GUILayout.Width(200), GUILayout.Height(200));
            {
                for (int i = 0; i < (int)AbsorptionCoefficient.Count; ++i)
                {
                    if (GUILayout.Button(Enum.GetName(typeof(AbsorptionCoefficient), i)))
                    {
                        geom.Absorption = (AbsorptionCoefficient)i;
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        void DrawUploaderMenu(GameObject obj, DSPUploader uploader)
        {
            GUIStyle style = new GUIStyle() { fontSize = 15 };
            style.normal.textColor = Color.red;
            GUILayout.Label("Current: " + Enum.GetName(typeof(SourceDirectivityPattern), uploader.sourcePattern), style);

            if (GUILayout.Button("Delete Source"))
            {
                Destroy(obj);
            }
            
            foreach (SourceDirectivityPattern pattern in Enum.GetValues(typeof(SourceDirectivityPattern)))
            {
                if (GUILayout.Button(Enum.GetName(typeof(SourceDirectivityPattern), pattern)))
                {
                    uploader.sourcePattern = pattern;
                }
            }

            using var _ = new GUILayout.HorizontalScope();
            GUILayout.Label("Wet Gain Ratio");
            uploader.WET_GAIN_RATIO = GUILayout.HorizontalSlider(uploader.WET_GAIN_RATIO, 0, 2);
        }

        void DrawObjInfoMenu(GameObject obj)
        {
            if(obj == null)
            {
                m_usingObjMenu = false;
                return;
            }


            if(!IsValidObj(obj, out FDTDGeometry geom, out DSPUploader uploader, out Bounds bounds))
            {
                m_usingObjMenu = false;
                return;
            }

            var worldPos = bounds.center;
            var position = Camera.main.WorldToScreenPoint(worldPos);

            const float areaSizeX = 200, areaSizeY = 400;
            position.x = Mathf.Clamp(position.x, areaSizeX / 2 + 15, Screen.width - areaSizeX / 2 - 15);
            position.y = Mathf.Clamp(position.y, areaSizeY / 2 + 15, Screen.height - areaSizeY / 2 - 15);

            // var textSize = GUI.skin.label.CalcSize(new GUIContent(text));

            var areaRect = new Rect(position.x, Screen.height - position.y, areaSizeX, areaSizeY);
            GUILayout.BeginArea(areaRect);
            {
                using var _ = new GUILayout.VerticalScope();

                if(geom)
                {
                    DrawGeomMenu(obj, geom);
                }
                if(uploader)
                {
                    DrawUploaderMenu(obj, uploader);
                }
            }
            GUILayout.EndArea();

            if (Event.current.type == EventType.Repaint)
            {
                m_usingObjMenu = areaRect.Contains(Event.current.mousePosition);
            }
        }


        void OnGUI()
        {
            using var __ = new GUIColorScope(Color.black);
            using var _ = new GUILayout.VerticalScope(!m_hidden ? "Demo Menu" : "", "window");

            if (!m_hidden)
            {
                if (GUILayout.Button("Hide"))
                {
                    m_hidden = true;
                }
            }
            else
            {
                if (GUILayout.Button("Unhide"))
                {
                    m_hidden = false;
                }
            }
            if (m_hidden) return;


            GUILayout.Label("Press Alt to unlock cursor");
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

            if(!m_disableReverb)
            {
                if (GUILayout.Button("Disable Reverb"))
                {
                    m_disableReverb = true;
                    foreach (var reverb in m_reverbs)
                    {
                        reverb.enabled = false;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Enable Reverb"))
                {
                    m_disableReverb = false;
                    foreach (var reverb in m_reverbs)
                    {
                        reverb.enabled = true;
                    }
                }
            }

            if (GUILayout.Button("Quit Application"))
            {
                Application.Quit();
            }

            DrawObjInfoMenu(m_curObj);
        }
    }
}