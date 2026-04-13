using CheckersGame.Core;
using CheckersGame.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheckersGame.Bootstrap
{
    public sealed class RuntimeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartRuntime()
        {
            RuntimeBootstrap existing = Object.FindObjectOfType<RuntimeBootstrap>();
            if (existing != null)
            {
                return;
            }

            GameObject root = new GameObject("CheckersRuntime");
            DontDestroyOnLoad(root);
            root.AddComponent<RuntimeBootstrap>();
        }

        private void Awake()
        {
            EnsureEventSystem();

            CheckersGameController controller = new CheckersGameController();
            RuntimeBoardView view = gameObject.AddComponent<RuntimeBoardView>();
            view.Initialize(controller);
            controller.StartNewGame();
        }

        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                return;
            }

            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemGo);
        }
    }
}
