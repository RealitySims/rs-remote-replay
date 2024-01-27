using UnityEngine;

public class ReplayViewerStatUI : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text _text;

    public void SetText(string text)
    {
        _text.SetText(text);
    }
}