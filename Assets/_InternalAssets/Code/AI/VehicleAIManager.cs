using System.Collections.Generic;
using UnityEngine;    
public class VehicleAIManager : MonoBehaviour
    {
        static public VehicleAIManager instance;
        
        public List<AI> aiOnScene = new List<AI>();
        public Complexity currentComplexity = Complexity.Hard;
        public void AddVehicle(AI ai)
        {
            if(ai!= null)
                aiOnScene.Add(ai);
        }

        public void SetComplexity(int complexity)
        {
            for (int i = 0; i < aiOnScene.Count; i++)
            {
                aiOnScene[i].SetComplexity((Complexity)complexity);
            }
            currentComplexity = (Complexity)complexity;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                Debug.Log("Added");
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
    }
