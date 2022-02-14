using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public bool isOnlineGame = false;
    public bool isPlaying = false;
    public bool movable = false;

    public List<Player> players = new List<Player>();
    public int turn = 1;
    public int activePlayerIndex = 0;
    bool passturn = false;

    [Header("Oyunda Kullanılacak Örnekler")]
    public List<GameObject> pawns = new List<GameObject>();
    public List<GameObject> maps = new List<GameObject>();
    public List<Texture2D> flags = new List<Texture2D>();
    public AudioClip sound_gong;
    public AudioClip sound_coins;
    public AudioClip sound_coinThrow;
    public GameObject gameUI;
    public GameObject gameMenuButton;
    public GameObject UIgameOver;
    public GameObject UIplayerEntry;
    public List<Sprite> UIVictory;
    public List<Sprite> UIDefeat;
    public GameObject UICoin;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (passturn)
        {
            passturn = false;
            PassTurn();
        }
    }

    public void PassTurn()
    {
        GetComponent<PlayerController>().isActionCamera = false;
        //GetComponent<PlayerController>().ShowAllRegions();

        GameObject[] regions = GameObject.FindGameObjectsWithTag("Region");
        string winner = "";
        bool gameover = true;
        foreach (GameObject region in regions)
        {
            if (winner == "")
            {
                winner = region.GetComponent<Region>().owner;
            }
            else
            {
                if (region.GetComponent<Region>().owner != "" && region.GetComponent<Region>().owner != winner)
                {
                    gameover = false;
                    break;
                }
            }
        }

        if (gameover)
        {
            activePlayerIndex = -1;
            GameOver(winner);
            return;
        }

        bool pass = true;
        while (pass)
        {
            if (activePlayerIndex < players.Count - 1)
            {
                activePlayerIndex++;
            }
            else
            {
                activePlayerIndex = 0;
                turn++;
            }

            CheckMagicCard();

            if (!players[activePlayerIndex].resigned)
            {
                foreach (GameObject region in regions)
                {
                    if (region.GetComponent<Region>().owner == players[activePlayerIndex].id)
                    {
                        pass = false;
                        break;
                    }
                }
            }

            // When player defeated on local game
            if (pass && players[activePlayerIndex].isHuman && !isOnlineGame)
            {
                GameOver();
                break;
            }
        }

        movable = true;

        RefreshGameUI();

        if (!IsPlayerHuman(players[activePlayerIndex].id) && GetComponent<PlayerController>().id == FindAIControllerId())
        {
            StartCoroutine(GetComponent<AIController>().Act());
        }

        StartCoroutine(Countdown());
    }

    public IEnumerator Countdown()
    {
        bool timeout = true;
        for (int i = 15; i >= 0; i--)
        {
            gameUI.transform.GetChild(1).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = i.ToString();
            yield return new WaitForSeconds(1);

            if (!movable || !isPlaying)
            {
                timeout = false;
                break;
            }
        }

        if (timeout && isPlaying)
        {
            GetComponent<PlayerController>().UIRegion.SetActive(false);
            StartCoroutine(GetComponent<PlayerController>().DeselectRegions(0.0f));
            GetComponent<PlayerController>().ShowAllRegions();
            GetComponent<GameState>().movable = false;
            passturn = true;
        }
    }

    public int FindPlayerIndexById(string id)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].id == id)
            {
                return i;
            }
        }

        return -1;
    }

    public bool IsTurnOnPlayer(string Id)
    {
        if (activePlayerIndex == -1)
        {
            return false;
        }
        else
        {
            return players[activePlayerIndex].id == Id;
        }
    }

    public bool IsPlayerHuman(string id)
    {
        if (activePlayerIndex == -1)
        {
            return false;
        }
        else
        {
            return players[activePlayerIndex].isHuman;
        }
    }

    public Player GetPlayerById(string id)
    {
        foreach (Player player in players)
        {
            if (player.id == id)
            {
                return player;
            }
        }

        return null;
    }

    public Region GetRegionById(string id)
    {
        GameObject[] regions = GameObject.FindGameObjectsWithTag("Region");

        foreach (GameObject region in regions)
        {
            if (region.GetComponent<Region>().id == id)
            {
                return region.GetComponent<Region>();
            }
        }

        return null;
    }

    public string FindAIControllerId()
    {
        string aiController = "";

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].isHuman && !players[i].resigned && !players[i].defeated)
            {
                aiController = players[i].id;
                break;
            }
        }

        return aiController;
    }

    IEnumerator DelayedHide(float sec)
    {
        yield return new WaitForSeconds(sec);
        GetComponent<PlayerController>().HideAllRegions();
    }

    public IEnumerator AttackToRegion(string attacker, string defender, bool success)
    {
        movable = false;
        UICoin.SetActive(true);
        GameObject coin = GameObject.FindGameObjectWithTag("Coin");
        coin.GetComponent<Animator>().Play(success ? "Head" : "Tails");
        GetComponent<AudioSource>().pitch = Random.Range(1.0f, 1.2f);
        GetComponent<AudioSource>().PlayOneShot(sound_coinThrow);


        yield return new WaitForSeconds(3f);
        UICoin.SetActive(false);
        coin.GetComponent<Animator>().Play("Base");

        GameObject atk = Instantiate(pawns[0]);
        GameObject def = Instantiate(pawns[0]);

        Region atkRegion = GetRegionById(attacker);
        Region defRegion = GetRegionById(defender);

        atk.GetComponent<Pawn>().Init(atkRegion.owner, atkRegion.transform, defRegion.transform, def.transform, true, success);
        def.GetComponent<Pawn>().Init(defRegion.owner, defRegion.transform, atkRegion.transform, atk.transform, false, !success);

        GetComponent<PlayerController>().camTarget = atk.transform;
        GetComponent<PlayerController>().isActionCamera = true;
    }

    public void Conquer(Region region, string newOwner, int rewards)
    {
        string oldOwner = region.owner;
        
        GetComponent<AudioSource>().pitch = Random.Range(0.8f, 1.2f);
        GetComponent<AudioSource>().PlayOneShot(sound_coins);
        
        if (oldOwner != "" && region.id == GetPlayerById(oldOwner).capital.id)
        {
            GameObject[] allRegions = GameObject.FindGameObjectsWithTag("Region");
            foreach(GameObject r in allRegions)
            {
                if (r.GetComponent<Region>().owner == oldOwner)
                {
                    r.GetComponent<Region>().ChangeOwner(newOwner);
                    players[FindPlayerIndexById(newOwner)].coins += rewards;
                    players[FindPlayerIndexById(newOwner)].score += rewards;
                }
            }

            players[FindPlayerIndexById(oldOwner)].defeated = true;
        }
        else
        {
            region.ChangeOwner(newOwner);
            players[FindPlayerIndexById(newOwner)].coins += rewards;
            players[FindPlayerIndexById(newOwner)].score += rewards;
        }
    }

    public void DisclaimRegions(string playerId)
    {
        GameObject[] regions = GameObject.FindGameObjectsWithTag("Region");
        foreach (GameObject region in regions)
        {
            if (region.GetComponent<Region>().owner == playerId)
            {
                region.GetComponent<Region>().ChangeOwner();
            }
        }
    }

    public void ClearScene()
    {
        GameObject[] allPawns = GameObject.FindGameObjectsWithTag("Pawn");
        foreach( GameObject p in allPawns)
        {
            Destroy(p);
        }

        players.Clear();

        GameObject map = GameObject.FindGameObjectWithTag("Map");
        Destroy(map);
    }

    public void SetupGame(List<Player> Players, int MapId, bool IsOnlineGame)
    {
        Camera.main.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
        isOnlineGame = IsOnlineGame;
        GetComponent<PlayerController>().ToggleChat(isOnlineGame);
        GameObject map = Instantiate(maps[MapId]);
        players.Clear();
        players = Players;
        GetComponent<PlayerController>().camPos = map.GetComponent<Map>().cameraPosition;
        List<Transform> spawnRegions = map.GetComponent<Map>().spawnRegions;
        List<Color> colors = map.GetComponent<Map>().colors;
        for (int i = 0; i < spawnRegions.Count; i++)
        {
            if (i < players.Count)
            {
                players[i].color = colors[i];
                string playerId = players[i].id;
                spawnRegions[i].GetComponent<Region>().ChangeOwner(playerId);
                players[i].capital = spawnRegions[i].GetComponent<Region>();
            }
            else
            {
                string name = isOnlineGame ? Game.botNames[Random.Range(0, Game.botNames.Length)] + i.ToString("00") : "BOT_" + i.ToString("00");
                Player newPlayer = new Player(name, colors[i], flags[i], false);
                players.Add(newPlayer);
                spawnRegions[i].GetComponent<Region>().ChangeOwner(name);
                newPlayer.capital = spawnRegions[i].GetComponent<Region>();
            }
        }

        turn = 1;
        activePlayerIndex = 0;
        isPlaying = true;
        movable = true;
        StartCoroutine(Countdown());

        RefreshGameUI();

        GetComponent<AudioSource>().pitch = 1;
        GetComponent<AudioSource>().PlayOneShot(sound_gong);

        GetComponent<PlayerController>().ClearChat();
    }

    public void GameOver(string winner = "")
    {
        gameUI.SetActive(false);
        gameMenuButton.SetActive(false);

        isPlaying = false;
        movable = false;

        if (GetComponent<PlayerController>().id == winner)
        {
            UIgameOver.GetComponent<UnityEngine.UI.Image>().sprite = UIVictory[Random.Range(0, UIVictory.Count)];
            UIgameOver.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "VICTORY!";
        }
        else
        {
            UIgameOver.GetComponent<UnityEngine.UI.Image>().sprite = UIDefeat[Random.Range(0, UIDefeat.Count)];
            UIgameOver.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "DEFEAT!";
        }

        UnityEngine.UI.ScrollRect rect = UIgameOver.transform.GetChild(1).GetComponent<UnityEngine.UI.ScrollRect>();

        while (true)
        {
            if (rect.content.childCount > 0)
            {
                GameObject obj = rect.content.GetChild(0).gameObject;
                rect.content.GetChild(0).parent = null;
                Destroy(obj);
            }
            else
            {
                break;
            }
        }

        foreach (Player p in players)
        {
            GameObject entry = Instantiate(UIplayerEntry);
            
            entry.transform.SetParent(rect.content);
            
            entry.GetComponent<RectTransform>().localScale = Vector3.one;
            entry.GetComponent<RectTransform>().localRotation = Quaternion.identity;
            entry.GetComponent<RectTransform>().localPosition = new Vector3(entry.GetComponent<RectTransform>().localPosition.x,
                entry.GetComponent<RectTransform>().localPosition.y, 0);
            
            entry.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = p.flag;
            entry.transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = p.id;
            entry.transform.GetChild(4).GetComponent<TMPro.TextMeshProUGUI>().text = p.id == winner ? (p.coins * 2).ToString() : p.coins.ToString();
            entry.transform.GetChild(5).gameObject.SetActive(p.id == winner);
        }

        Player player = GetPlayerById(GetComponent<PlayerController>().id);
        User.score += player.id == winner ? player.score * 2 : player.score;
        User.coins += player.id == winner ? player.coins * 2 : player.coins;

        //GetComponent<NetworkController>().SaveUserData(isOnlineGame && GetComponent<NetworkController>().connected);
        GetComponent<NetworkController>().SaveUserData();

        ClearScene();
        UIgameOver.SetActive(true);

        GetComponent<PlayerController>().ClearChat();
        GetComponent<PlayerController>().ToggleChat(false);
    }

    public void RefreshGameUI()
    {
        int index = activePlayerIndex;
        if (index != -1 && isPlaying)
        {
            gameUI.transform.GetChild(0).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = GetPlayerById(GetComponent<PlayerController>().id).coins.ToString();
            //gameUI.transform.GetChild(1).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = turn.ToString();
            gameUI.transform.GetChild(2).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = players[index].flag;
            gameUI.transform.GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = players[index].id;
            gameUI.transform.GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().color = players[index].color;


            if (IsTurnOnPlayer(GetComponent<PlayerController>().id))
            {
                gameUI.transform.GetChild(3).gameObject.SetActive(true);
            }
            else
            {
                gameUI.transform.GetChild(3).gameObject.SetActive(false);
            }

            gameUI.SetActive(true);
            gameMenuButton.SetActive(true);
        }
    }

    public void ToggleMenuButton()
    {
        if (gameMenuButton.transform.GetChild(0).gameObject.activeSelf)
        {
            gameMenuButton.transform.GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            gameMenuButton.transform.GetChild(0).gameObject.SetActive(true);
        }
    }

    public void Resign()
    {
        if (isOnlineGame)
        {
            NetPack pack = new NetPack("resign", User.playerId);
            GetComponent<NetworkController>().SendPack(pack);
            GetComponent<PlayerController>().mainMenu.SetActive(true);
            GetComponent<PlayerController>().ClearChat();
            GetComponent<PlayerController>().ToggleChat(false);
        }
        else
        {
            GameOver();
        }

        ClearScene();
        gameUI.SetActive(false);
        UICoin.SetActive(false);
        GetComponent<PlayerController>().UIRegion.SetActive(false);
        gameMenuButton.transform.GetChild(0).gameObject.SetActive(false);
        gameMenuButton.SetActive(false);
        User.gameId = -1;
    }

    public void UseMagicCardInGame(NPDUseCard card)
    {
        if (card.id == "Protection")
        {
            Player p = GetPlayerById(card.owner);
            p.magicCard = new MagicCardData(card.id, 1, 3);
            if (card.owner == User.playerId)
            {
                GetComponent<PlayerController>().ConsumeMagicCard(card.id);
                GetComponent<PlayerController>().ShowMessageBox("You are under protection now!");
            }
        }
        else if (card.id == "Luck")
        {
            Player p = GetPlayerById(card.owner);
            p.magicCard = new MagicCardData(card.id, 1, 3);
            if (card.owner == User.playerId)
            {
                GetComponent<PlayerController>().ConsumeMagicCard(card.id);
                GetComponent<PlayerController>().ShowMessageBox("You will be very lucky!");
            }
        }
    }

    public void CheckMagicCard()
    {
        if (players[activePlayerIndex].magicCard != null)
        {
            players[activePlayerIndex].magicCard.turn--;
            if (players[activePlayerIndex].magicCard.turn <= 0)
            {
                players[activePlayerIndex].magicCard = null;
            }
        }
    }

    public float CalculateChance(string attacker, string defender)
    {
        float chance = 0.5f;
        Player atk = GetPlayerById(attacker);
        Player def = GetPlayerById(defender);

        if (atk != null && def != null)
        {
            if (atk.magicCard != null && def.magicCard != null && atk.magicCard.id == "Luck" && def.magicCard.id == "Protection")
            {
                chance = 0.5f;
            }
        }
        else if (atk != null && atk.magicCard != null)
        {
            if (atk.magicCard.id == "Luck")
            {
                chance = 1;
            }
        }
        else if (def != null && def.magicCard != null)
        {
            if (atk.magicCard.id == "Protection")
            {
                chance = 0;
            }
        }
        else if (defender == "")
        {
            chance = 0.7f;
        }

        return chance;
    }
}
