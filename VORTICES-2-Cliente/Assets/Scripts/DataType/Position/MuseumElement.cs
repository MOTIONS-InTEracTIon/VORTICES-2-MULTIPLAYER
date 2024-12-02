using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vortices
{
    public class MuseumElement : MonoBehaviour
    {
        // Other references
        private GameObject elementFrame;

        // Data variables
        [HideInInspector] public List<string> elementPaths;

        // Utility
        public int globalIndex;
        private List<string> loadPaths;
        private List<GameObject> unloadObjects;
        private List<GameObject> loadObjects;
        private Fade fader;
        private int spawnedHandlingCoroutinesRunning;

        // Auxiliary Task Class
        [SerializeField] private GameObject renderManager;

        // Settings
        public string browsingMode { get; set; }



        #region Multimedia Spawn
        public void Init(List<string> elementPaths, string browsingMode)
        {
            elementFrame = transform.GetChild(0).gameObject;
            this.elementPaths = elementPaths;
            this.browsingMode = browsingMode;
        }

        public IEnumerator StartSpawnOperation(int offsetGlobalIndex)
        {
            // Startup
            globalIndex = offsetGlobalIndex;
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            loadObjects = new List<GameObject>();
            fader = GetComponent<Fade>();

            int startingLoad = 1;

            // Execution
            yield return StartCoroutine(ObjectSpawn(0, startingLoad, true));
        }

        public IEnumerator ChangeElement(bool forwards)
        {
            // Startup
            loadPaths = new List<string>();
            unloadObjects = new List<GameObject>();
            loadObjects = new List<GameObject>();

            yield return StartCoroutine(ObjectSpawn(1, 1, forwards));
        }

        // Spawns files using overriden GenerateExitObjects and GenerateEnterObjects
        protected IEnumerator ObjectSpawn(int unloadNumber, int loadNumber, bool forwards)
        {
            ObjectPreparing(unloadNumber, loadNumber);
            yield return StartCoroutine(DestroyObjectHandling());
            yield return StartCoroutine(ObjectLoad(forwards));
            yield return StartCoroutine(SpawnedObjectHandling());
        }

        // Destroys placement objects not needed and insert new ones at the same time
        protected void ObjectPreparing(int unloadNumber, int loadNumber)
        {
            // Generate list of child objects to leave the scene
            GenerateExitObjects(unloadNumber);

            // Generate list of child objects to spawn into the scene
            GenerateEnterObjects(loadNumber);
        }

        public void GenerateExitObjects(int unloadNumber)
        {
            unloadObjects = new List<GameObject>();

            for (int i = 0; i < unloadNumber ; i++)
            {
                unloadObjects.Add(transform.GetChild(0).GetChild(0).gameObject);
            }
        }

        public void GenerateEnterObjects(int loadNumber)
        {
            loadObjects = new List<GameObject>();

            for (int i = 0; i < loadNumber ; i++)
            {
                loadObjects.Add(elementFrame);
            }

        }

        protected IEnumerator ObjectLoad(bool forwards)
        {
            // Generate selection path to get via render
            yield return StartCoroutine(GenerateLoadPaths(forwards));

            // Make them appear in the scene
            RenderController render = Instantiate(renderManager).GetComponent<RenderController>();
            yield return StartCoroutine(render.PlaceMultimedia(loadPaths, loadObjects, browsingMode, "Plane"));

            Destroy(render.gameObject);
        }

        protected IEnumerator DestroyObjectHandling()
        {
            foreach (GameObject go in unloadObjects)
            {
                Destroy(go.gameObject);
            }

            yield return null;
        }

        protected IEnumerator SpawnedObjectHandling()
        {
            spawnedHandlingCoroutinesRunning = 0;

            TaskCoroutine fadeCoroutine = new TaskCoroutine(fader.FadeInCoroutine());
            fadeCoroutine.Finished += delegate (bool manual)
            {
                spawnedHandlingCoroutinesRunning--;
            };
            spawnedHandlingCoroutinesRunning++;

            while (spawnedHandlingCoroutinesRunning > 0)
            {
                yield return null;
            }

        }

        protected IEnumerator GenerateLoadPaths(bool forwards)
        {
            int index = 0;


            if (forwards)
            {
                globalIndex++;
            }
            else
            {
                globalIndex--;
            }
                
            string actualPath = "";

            actualPath = CircularList.GetElement<string>(elementPaths, globalIndex);

            loadPaths.Add(actualPath);

            index++;
            yield return null;

        }

        public void DestroyWebView()
        {
            Destroy(elementFrame.transform.GetChild(0).gameObject);
        }

        #endregion
    }
}

