using UnityEngine;

public class DestroyAfterIntro : MonoBehaviour
{
    private void Start()
    {
        GameSys gameSys = GameSys.Instance;
        if (gameSys == null)
        {
            Debug.LogWarning("DestroyAfterIntro: no GameSys instance found in the scene.");
            return;
        }

        if (gameSys.CurrentState != GameState.Intro)
        {
            Destroy(gameObject);
            return;
        }

        gameSys.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameSys gameSys = GameSys.Instance;
        if (gameSys != null)
            gameSys.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState != GameState.Intro)
            Destroy(gameObject);
    }
}
