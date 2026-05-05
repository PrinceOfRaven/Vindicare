using UnityEngine;
using UnityEngine.UI;

public class ChangeScene : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private void Start()
    {
        Button btn = GetComponent<Button>();

        if (btn != null ) 
        {
            btn.onClick.AddListener(() =>
            {
                CallTransit.Instance.LoadScene(sceneName);
            });
        }
    }
}
