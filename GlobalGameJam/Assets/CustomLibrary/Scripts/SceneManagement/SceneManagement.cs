using System;
using System.Collections;
using System.Collections.Generic;
using CustomLibrary.Scripts.Instance;
using CustomLibrary.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomLibrary.Scripts.SceneManagement
{
    public class SceneManagement : MonoBehaviourInstance<SceneManagement>
    {
        private readonly Stack<string> sceneStack = new();

        public void LoadLevel(int sceneIndex, ISceneManagementLoadingBar sceneManagementLoadingBar = null, float loadDelay = 0f)
        {
            sceneStack.Push(SceneManager.GetActiveScene().name);
            StartCoroutine(LoadSceneAsync(() => SceneManager.LoadSceneAsync(sceneIndex), sceneManagementLoadingBar, loadDelay));
        }

        public void LoadLevel(string sceneName, ISceneManagementLoadingBar sceneManagementLoadingBar = null, float loadDelay = 0f)
        {
            sceneStack.Push(SceneManager.GetActiveScene().name);
            StartCoroutine(LoadSceneAsync(() => SceneManager.LoadSceneAsync(sceneName), sceneManagementLoadingBar, loadDelay));
        }

        public void PopLastScene(ISceneManagementLoadingBar sceneManagementLoadingBar = null, float loadDelay = 0f)
        {
            if(sceneStack.Count == 0) return;
            string sceneName = sceneStack.Pop();
            StartCoroutine(LoadSceneAsync(() => SceneManager.LoadSceneAsync(sceneName), sceneManagementLoadingBar, loadDelay));
        }

        private IEnumerator LoadSceneAsync(Func<AsyncOperation> loadOperationFactory, ISceneManagementLoadingBar sceneManagementLoadingBar = null, float loadDelay = 0f)
        {
            if(loadDelay > 0f)
            {
                yield return new WaitForSeconds(loadDelay);
            }

            AsyncOperation operation = loadOperationFactory.Invoke();
            operation.allowSceneActivation = false;

            float displayedProgress = 0f;

            while(operation.progress < 0.9f)
            {
                float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.deltaTime);
                sceneManagementLoadingBar?.SetValue(displayedProgress);
                yield return null;
            }

            while(displayedProgress < 1f)
            {
                displayedProgress = Mathf.MoveTowards(displayedProgress, 1f, Time.deltaTime);
                sceneManagementLoadingBar?.SetValue(displayedProgress);
                yield return null;
            }

            operation.allowSceneActivation = true;
        }
    }
}
