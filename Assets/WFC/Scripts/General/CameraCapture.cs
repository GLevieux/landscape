using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraCapture : MonoBehaviour
{
    public KeyCode screenshotKey;

    //Custom camera vue top
    public Camera screenshotCamera;

    private Camera currentCamera
    {
        get
        {
            if (!screenshotCamera)
            {
                return Camera.main;
            }
            else
            {
                return screenshotCamera;
            }
        }
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot("screenshot.png", true);
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

    public void TakeScreenshot(string name, bool createDirectory)
    {
        if(createDirectory)
        {
            DirectoryInfo di = Directory.CreateDirectory(Logger.getPath());
        }

        bool active = currentCamera.gameObject.activeSelf;
        Camera prevMainCam = Camera.main;
        if (!active)
        {
            prevMainCam.gameObject.SetActive(false);
            currentCamera.gameObject.SetActive(true);
        }

        Capture(currentCamera, new Vector2Int(1920, 1080), 2, true);
        File.WriteAllBytes(Logger.getPath(name), screenshotBytes);

        if (!active)
        {
            prevMainCam.gameObject.SetActive(true);
            currentCamera.gameObject.SetActive(false);
        }
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
