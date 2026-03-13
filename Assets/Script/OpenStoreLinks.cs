using UnityEngine;

public class OpenStoreLinks : MonoBehaviour
{
    public GameObject LinkPanel; // ← Inspector でアサイン
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
        if (LinkPanel != null)
        {
            LinkPanel.SetActive(true);
        }
    }
    public void CloseStorePanel()
    {
        if (LinkPanel != null)
        {
            LinkPanel.SetActive(false);
        }
    }
}
