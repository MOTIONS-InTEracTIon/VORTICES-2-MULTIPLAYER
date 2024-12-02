using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Vortices
{
    // This base does not create objects, it uses premade objects and it spawns webviews into them
    public class MuseumBase : MuseumSpawnBase
    {

        #region Element Spawn

        public override IEnumerator StartGenerateSpawnElements()
        {
            int fadeCoroutinesRunning = 0;
            // Fade old elements
            if (defaultPicture != null)
            {
                

                Fade defaultElementFader = defaultPicture.GetComponent<Fade>();
                defaultElementFader.lowerAlpha = 0;
                defaultElementFader.upperAlpha = 1;
                TaskCoroutine fadeCoroutine = new TaskCoroutine(defaultElementFader.FadeOutCoroutine());
                fadeCoroutine.Finished += delegate (bool manual)
                {
                    fadeCoroutinesRunning--;
                };
                fadeCoroutinesRunning++;

                while (fadeCoroutinesRunning > 0)
                {
                    yield return null;
                }

                defaultPicture.SetActive(false);
            }

            // Fill every element in the list 
            int spawnCoroutinesRunning = 0;

            foreach (GameObject element in elementList)
            {
                element.SetActive(true);
                MuseumElement elementComponent = element.GetComponent<MuseumElement>();
                elementComponent.Init(elementPaths, browsingMode);
                TaskCoroutine spawnCoroutine = new TaskCoroutine(elementComponent.StartSpawnOperation(globalIndex++));
                spawnCoroutine.Finished += delegate (bool manual)
                {
                    spawnCoroutinesRunning--;
                };
                spawnCoroutinesRunning++;
            }

            while (fadeCoroutinesRunning > 0)
            {
                yield return null;
            }

        }
        #endregion
    }
}

