using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMove : MonoBehaviour 
{
    // 這裡一定要寫 public，否則按鈕選單會找不到它！
    public void ChangeScene(string sceneName) 
    {
        SceneManager.LoadScene(sceneName);
    }
}
