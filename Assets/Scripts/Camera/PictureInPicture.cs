using UnityEngine;
using UnityEngine.UI;

public class PictureInPicture : MonoBehaviour
{
    [Header("Camera to show in the window")]
    [Tooltip("The secondary camera whose view you want in the little window.")]
    [SerializeField] private Camera pipCamera;

    [Header("UI display")]
    [Tooltip("A UI RawImage to draw the picture-in-picture into.")]
    [SerializeField] private RawImage pipImage;

    [Header("Render Texture Size")]
    [SerializeField] private Vector2Int renderSize = new Vector2Int(512, 288);

    private RenderTexture renderTexture;

    private void Start()
    {
        if (pipCamera == null || pipImage == null)
        {
            Debug.LogWarning("PictureInPicture: assign pipCamera and pipImage.", this);
            enabled = false;
            return;
        }

        renderTexture = new RenderTexture(renderSize.x, renderSize.y, 24);
        renderTexture.Create();

        pipCamera.targetTexture = renderTexture;
        pipImage.texture = renderTexture;
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            pipCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    public void Show() => pipImage.gameObject.SetActive(true);
    public void Hide() => pipImage.gameObject.SetActive(false);
    public void Toggle() => pipImage.gameObject.SetActive(!pipImage.gameObject.activeSelf);
}
