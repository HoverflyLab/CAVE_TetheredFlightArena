using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEditor;


namespace TetheredFlight
{
    // The longitudinal Axis needs to be calculated as there is no guarentee that the camera and tethered animal are perfectly aligned.
    // This script provides the functionality for a menu which allows the user to set the Longitudinal Axis manually, this won't always be necessary as DLC can be used to find the longitudinal axis.

    // AN ACCURATE LONGITUDINAL AXIS IS EXTREMELY IMPORTANT - as the longitudinal axis is literally at the centre of all calculations, 
    // if the user sets a poor longitudinal axis it can make it impossible for the tethered animal to fly as intended due to inaccurate wing beat amplitudes being calculated.
    public class LongitudinalAxisMenu : MonoBehaviour
    {
        [SerializeField] private RawImage m_RawImage = null;
        [SerializeField] private RectTransform upperPoint_Img = null;
        [SerializeField] private RectTransform lowerPoint_Img = null;
        [SerializeField] private Vector2Int ImageResolution = new Vector2Int(320,240);
        [SerializeField] private TextMeshProUGUI Instructions_TextMesh = null;
        [SerializeField] private TextMeshProUGUI Hint_TextMesh = null;
        [SerializeField] private TextMeshProUGUI ReturnToMenu_TextMesh = null;
        [SerializeField] private UnityEngine.UI.Extensions.UILineRenderer lineRenderer = null; //LineRenderer script created by jack.sydorenko, firagon

        private Vector2 editorResolution = new Vector2(0f,0f);
        private Vector2 rawImagePosition = new Vector2(0f,0f);
        private Vector2 rawImageSize = new Vector2(0f,0f);
        private Vector2 toScale = new Vector2(0f,0f);
        private Vector2 upperPoint = new Vector2(0f,0f);
        private Vector2 lowerPoint = new Vector2(0f,0f);

        private bool isFirstPoint = true;
        private bool isImageCaptured = false;
        private bool arePointsSet = false;

        void Start()
        {
            editorResolution = GetMainGameViewSize();
            rawImagePosition = RectTransformToScreenSpace(m_RawImage.rectTransform).position;
            rawImageSize = RectTransformToScreenSpace(m_RawImage.rectTransform).size;
            toScale.x = rawImageSize.x / ImageResolution.x;
            toScale.y = rawImageSize.y / ImageResolution.y;
            lineRenderer.LineThickness = editorResolution.x / 100;
        }

        void Update()
        {
            if(isImageCaptured == false)
            {
                return;
            }

            //If both points are active and the S key has been pressed, skip to the next point
            if(lowerPoint_Img.gameObject.activeSelf == true && Input.GetKeyDown(KeyCode.S))
            {
                if(isFirstPoint == true)
                {
                    isFirstPoint = false;
                    Instructions_TextMesh.text = "Place - Lower Point";
                }
                else
                {
                    isFirstPoint = true;
                    Instructions_TextMesh.text = "Place - Upper Point";
                }
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Input.mousePosition;

                //Check to see if click is within the bounds of the canvas
                if(mousePos.x >= rawImagePosition.x && mousePos.x <= rawImagePosition.x + rawImageSize.x && mousePos.y >= rawImagePosition.y && mousePos.y <= rawImagePosition.y + rawImageSize.y)
                {
                    if(isFirstPoint == true)
                    {
                        isFirstPoint = false;
                        upperPoint_Img.gameObject.SetActive(true);
                        upperPoint = new Vector2(Mathf.Round((mousePos.x - rawImagePosition.x) / toScale.x), Mathf.Round((mousePos.y - rawImagePosition.y) / toScale.y));
                        upperPoint_Img.position = new Vector2(mousePos.x, mousePos.y);
                        DataProcessor.Instance.Set_LongitudinalAxis_Upper_Points(upperPoint);
                        Instructions_TextMesh.text = "Place - Lower Point";
                    }
                    else
                    {
                        //Should run only once, just before second point becomes active.
                        if(lowerPoint_Img.gameObject.activeSelf == false)
                        {
                            arePointsSet = true;
                            ReturnToMenu_TextMesh.text = "Confirm Points";
                            Hint_TextMesh.gameObject.SetActive(true);
                        }

                        isFirstPoint = true;
                        lowerPoint_Img.gameObject.SetActive(true);
                        lowerPoint = new Vector2(Mathf.Round((mousePos.x - rawImagePosition.x) / toScale.x), Mathf.Round((mousePos.y - rawImagePosition.y) / toScale.y));
                        lowerPoint_Img.position = new Vector2(mousePos.x, mousePos.y);
                        DataProcessor.Instance.Set_LongitudinalAxis_Lower_Points(lowerPoint);
                        Instructions_TextMesh.text = "Place - Upper Point";
                    }

                    if(lowerPoint_Img.gameObject.activeSelf == true)
                    {
                        lineRenderer.Points[0] = new Vector2(upperPoint.x * toScale.x, upperPoint.y * toScale.y);
                        lineRenderer.Points[1] = new Vector2(lowerPoint.x * toScale.x, lowerPoint.y * toScale.y);
                        lineRenderer.SetAllDirty(); //This commnad redraws the line
                    }
                }
            }
        }

        public void WebcamCapture() 
        {
            captureimg();
        }

        private void captureimg() 
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            WebCamTexture texure = new WebCamTexture(devices[0].name);
            texure.requestedWidth = ImageResolution.x;
            //tex.height = 240;
            texure.Play();
            StartCoroutine(CaptureImage(texure));
        }

        IEnumerator CaptureImage(WebCamTexture texure)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            Texture2D texture = new Texture2D(texure.width, texure.height);
            texture.SetPixels(texure.GetPixels());
            texture.Apply();
            m_RawImage.texture = texture;
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/flycapture.png", bytes);
            texure.Stop();
            isImageCaptured = true;
            Instructions_TextMesh.text = "Place - Upper Point";
            Instructions_TextMesh.gameObject.SetActive(true);
        }

        //https://answers.unity.com/questions/179775/game-window-size-from-editor-window-in-editor-mode.html?_ga=2.220515928.1900753828.1636602035-1950483416.1619668567
        //Returns resolution of the editor window
        private static Vector2 GetMainGameViewSize()
        {
            System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            System.Object Res = GetSizeOfMainGameView.Invoke(null,null);
            return (Vector2)Res;
        }

        //https://answers.unity.com/questions/1013011/convert-recttransform-rect-to-screen-space.html
        //Returns bottom left coordinates of the RectTransform
        private static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            return new Rect((Vector2)transform.position - (size * transform.pivot), size);
        }

        public bool ArePointsSet()
        {
            return arePointsSet;
        }
    }
}   