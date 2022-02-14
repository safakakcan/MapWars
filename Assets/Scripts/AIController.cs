using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Act()
    {
        bool online = GetComponent<GameState>().isOnlineGame;
        yield return new WaitForSeconds(online ? Random.Range(2f, 6f) : 0);

        GameObject[] allRegions = GameObject.FindGameObjectsWithTag("Region");
        List<Region> myRegions = new List<Region>();
        foreach (GameObject region in allRegions)
        {
            if (GetComponent<GameState>().players[GetComponent<GameState>().activePlayerIndex].id == region.GetComponent<Region>().owner)
                myRegions.Add(region.GetComponent<Region>());
        }

        List<Region> A = new List<Region>();
        List<Region> B = new List<Region>();

        foreach (Region myRegion in myRegions)
        {
            foreach (Region neighbor in myRegion.neighbors)
            {
                bool isProtected = false;
                Player p = GetComponent<GameState>().GetPlayerById(neighbor.owner);
                if (p != null && p.magicCard != null)
                {
                    isProtected = p.magicCard.id == "Protection";
                }

                if (neighbor.owner != myRegion.owner && !isProtected)
                {
                    A.Add(myRegion);
                    B.Add(neighbor);
                }
            }
        }

        if (A.Count > 0)
        {
            int index = Random.Range(0, A.Count);
            float chance = GetComponent<GameState>().CalculateChance(A[index].owner, B[index].owner);
            if (GetComponent<GameState>().isOnlineGame)
            {
                NPDAttack act = new NPDAttack(A[index].id, B[index].id, Random.value <= chance);
                string json = JsonUtility.ToJson(act);
                NetPack pack = new NetPack("attack", json);
                GetComponent<NetworkController>().SendPack(pack);
            }
            else
            {
                StartCoroutine(GetComponent<GameState>().AttackToRegion(A[index].id, B[index].id, Random.value <= chance));
            }
        }
        else
        {
            GetComponent<GameState>().PassTurn();
        }
    }
}
