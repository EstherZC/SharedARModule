using UnityEngine;
/// <summary>
/// Last update: 5/10/2021
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    public class HighlighterManager : MonoBehaviour
    {
        private GameObject prefabSelected;

        void Update()
        {
            if (!prefabSelected)
            {
                Destroy(gameObject);
            }
        }

        public void SetPrefabSelected(GameObject prefabSelected)
        {
            this.prefabSelected = prefabSelected;
        }

    }
}