using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public class AudioManager : MonoBehaviour
    {
        
        public static AudioManager Instance;

        private void Awake()
        {

            if (Instance == null)
            {

                Instance = this;
                DontDestroyOnLoad(gameObject);

            }
            else 
            {
                Destroy(gameObject);
            }
        }
    }
}
