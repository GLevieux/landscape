using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraCapture : MonoBehaviour
{
    public KeyCode screenshotKey;

    //Custom camera vue top
    public Camera [] screenshotCameras;

    /*private Camera currentCamera
    {
        get
        {
            if (screenshotCamera == null)
            {
                return Camera.main;
            }
            else
            {
                return screenshotCamera;
            }
        }
    }*/

    private void LateUpdate()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshots(true);
            //Capture(currentCamera, new Vector2Int(1920, 1080), 2, true);
            //Capture();
        }
    }

    //public void Capture()
    //{
    //    RenderTexture activeRenderTexture = RenderTexture.active;
    //    RenderTexture.active = Camera.targetTexture;

    //    Camera.Render();

    //    Texture2D image = new Texture2D(Camera.targetTexture.width, Camera.targetTexture.height);
    //    image.ReadPixels(new Rect(0, 0, Camera.targetTexture.width, Camera.targetTexture.height), 0, 0);
    //    image.Apply();
    //    RenderTexture.active = activeRenderTexture;

    //    byte[] bytes = image.EncodeToPNG();
    //    Destroy(image);

    //    File.WriteAllBytes(Application.dataPath + "/Log/" + fileCounter + ".png", bytes);
    //    fileCounter++;
    //}

    private byte[] screenshotBytes;

    public void TakeScreenshots(bool createDirectory)
    {
        if(createDirectory)
        {
            DirectoryInfo di = Directory.CreateDirectory(Logger.getPath());
        }

        Camera prevMainCam = Camera.main;
        prevMainCam.gameObject.SetActive(false);

        int i = 0;
        foreach (Camera c in screenshotCameras)
        {
            i++;
            c.gameObject.SetActive(true);
            Capture(c, new Vector2Int(1920, 1080), 2, true);
            File.WriteAllBytes(Logger.getPath("ss_"+Mathf.Round(Time.time*1000)+"_cam"+i+".png"), screenshotBytes);
            c.gameObject.SetActive(false);
        }

        prevMainCam.gameObject.SetActive(true);
    }

    private void Capture(Camera camera, Vector2Int resolution, int scale, bool isTransparent)
    {
        var finalResolution = resolution * scale;
        RenderTexture renderTexture = new RenderTexture(finalResolution.x, finalResolution.y, 24);
        camera.targetTexture = renderTexture;

        var tFormat = isTransparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;

        var screenShot = new Texture2D(finalResolution.x, finalResolution.y, tFormat, false);
        camera.Render();
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(new Rect(0, 0, finalResolution.x, finalResolution.y), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        screenshotBytes = screenShot.EncodeToPNG();

        Destroy(screenShot);

        //fix to switch to main camera
        //camera.gameObject.SetActive(false);
        //Camera.main.gameObject.SetActive(true);
    }
}
