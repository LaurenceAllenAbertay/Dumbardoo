using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MapSelectButton : MonoBehaviour
{
    [SerializeField] private MainMenuController menu;
    [SerializeField] private string sceneName;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(HandleClicked);
    }

    private void HandleClicked()
    {
        if (menu == null)
        {
            Debug.LogWarning("MapSelectButton: No MainMenuController assigned.");
            return;
        }

        menu.StartMatch(sceneName);
    }
}
