using UnityEngine;

public class OpenStoreLinks : MonoBehaviour
{
    public GameObject LinkPanel; // ← Inspector でアサイン
    [SerializeField] private GameObject _buttonsRoot;

    public void OpenGooglePlay()
    {
        Application.OpenURL("https://play.google.com/store/apps/developer?id=azld.nomi.com&hl=ja");
    }

    public void OpenAppStore()
    {
        Application.OpenURL("https://apps.apple.com/jp/developer/yoshiki-hamana/id1812892520");
    }

    public void OpenX()
    {
        Application.OpenURL("https://x.com/complaint_ychan");
    }

    public void OpenInstagram()
    {
        Application.OpenURL("https://www.instagram.com/azld_games");
    }
    // ストアパネル表示
    public void OpenStorePanel()
    {
        EnsureReferences();

        if (_buttonsRoot != null)
        {
            _buttonsRoot.SetActive(false);
        }

        if (LinkPanel != null)
        {
            LinkPanel.SetActive(true);
        }
    }
    public void CloseStorePanel()
    {
        EnsureReferences();

        if (LinkPanel != null)
        {
            LinkPanel.SetActive(false);
        }

        if (_buttonsRoot != null)
        {
            _buttonsRoot.SetActive(true);
        }
    }

    private void EnsureReferences()
    {
        if (_buttonsRoot == null)
        {
            _buttonsRoot = FindSceneObject("Bottuns");
        }

        if (LinkPanel == null)
        {
            LinkPanel = FindSceneObject("LinkPanelforGoogle");
            if (LinkPanel == null)
            {
                LinkPanel = FindSceneObject("LinkPanelforApple");
            }
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var current in transforms)
        {
            if (current == null || !current.gameObject.scene.IsValid())
            {
                continue;
            }

            if (current.name == objectName)
            {
                return current.gameObject;
            }
        }

        return null;
    }
}
