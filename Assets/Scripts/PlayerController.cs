using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Linq;
using SimpleFileBrowser;
using GoogleMobileAds.Common;
using GoogleMobileAds.Api;


public class PlayerController : MonoBehaviour
{
    public string id = "Player";
    public string fbId = "";
    public Texture2D flag;

    private Vector2 beginPoint = Vector2.zero;
    private Vector2 dragPoint = Vector2.zero;

    private Region selectedRegion = null;
    private Region targetRegion = null;

    public bool isActionCamera = false;
    public Vector3 camPos;
    //public Vector3 targetPos;
    public Transform camTarget;

    public AudioClip click;

    [Header("UI")]
    public GameObject mainMenu;
    public GameObject registerMenu;
    public GameObject FBregisterMenu;
    public GameObject onlineLobby;
    public GameObject leaderboard;
    public GameObject boardEntry;
    public GameObject myAssets;
    public GameObject UIMagicCard;
    public GameObject UIRegion;
    public GameObject messageBox;
    public GameObject settings;
    public GameObject chatButton;
    public GameObject chatPanel;
    public GameObject chatMsg;
    public GameObject tapFX;
    public Texture2D questionMark;
    public GameObject button_selectFlag;

    public RewardedAd rewardedAd;
    public InterstitialAd videoAd;
    private System.DateTime lastAdTime = System.DateTime.Now.AddMinutes(-10);

    public List<MagicCard> magicCards = new List<MagicCard>();
    public AudioClip magicCardSoundFX;

    // Start is called before the first frame update
    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        camPos = transform.position;
        StartCoroutine(LoadAllAds());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Q))
        {
            ScreenCapture.CaptureScreenshot(System.DateTime.Now.Ticks.ToString() + ".png", 2);
        }

        if (GetComponent<GameState>().movable && GetComponent<GameState>().isPlaying)
        {
            if (Input.GetMouseButtonDown(0))
            {
                beginPoint = Input.mousePosition;
                dragPoint = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && !IsPointerOverUI())
            {
                if (Vector2.Distance(beginPoint, Input.mousePosition) < 10)
                {
                    if (GetComponent<GameState>().IsTurnOnPlayer(id) && GetComponent<GameState>().movable)
                    {
                        if (!UIRegion.activeSelf)
                            SelectRegion();
                    }
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (dragPoint != Vector2.zero)
                {
                    Vector2 mousePos = Input.mousePosition;
                    Vector2 delta = dragPoint - mousePos;
                    float y = GameObject.FindGameObjectWithTag("Map").GetComponent<Map>().cameraPosition.y;
                    Vector3 position = new Vector3(transform.position.x + delta.x * 0.01f, y, transform.position.z + delta.y * 0.01f);
                    Camera.main.transform.position = position;
                    CheckBounds();
                    camPos = transform.position;
                    dragPoint = mousePos;
                }
            }
        }

        ManageCamera();
    }

    public IEnumerator LoadAllAds()
    {
        yield return new WaitForSeconds(0);

        rewardedAd = CreateRewardedAd();
        videoAd = CreateVideoAd();
    }

    public bool IsPointerOverUI()
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Where(r => r.gameObject.layer == 5).Count() > 0;
    }

    public void SelectRegion()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10000))
        {
            if (hit.collider.tag == "Region")
            {
                Click();
                if (hit.collider.GetComponent<Region>().owner == id)
                {
                    if (selectedRegion != null && selectedRegion.id == hit.collider.GetComponent<Region>().id)
                    {
                        StartCoroutine(DeselectRegions(0.0f));
                        ShowAllRegions();
                    }
                    else
                    {
                        if (selectedRegion != null)
                            selectedRegion.selected = false;
                        selectedRegion = hit.collider.GetComponent<Region>();
                        hit.collider.GetComponent<Region>().selected = true;
                        ShowOnlyNearRegions();
                    }
                }
                else if (hit.collider.GetComponent<Region>().owner != id && selectedRegion != null)
                {
                    if (targetRegion != null)
                        targetRegion.selected = false;

                    if (selectedRegion.IsRegionNeighbor(hit.collider.gameObject.GetComponent<Region>().id))
                    {
                        targetRegion = hit.collider.GetComponent<Region>();
                        hit.collider.GetComponent<Region>().selected = true;
                        
                        //Attack();
                        ShowRegionUI();
                    }
                }
            }
        }

        beginPoint = Vector2.zero;
        dragPoint = Vector2.zero;
    }

    public IEnumerator DeselectRegions(float delta = 0)
    {
        yield return new WaitForSeconds(delta);

        if (selectedRegion != null)
        {
            selectedRegion.selected = false;
            selectedRegion = null;
        }

        if (targetRegion != null)
        {
            targetRegion.selected = false;
            targetRegion = null;
        }
    }

    public void NewLocalGame()
    {
        if (!User.adsRemoved)
            ShowVideoAd();

        Player player = new Player(User.playerId, Color.red, flag, true);
        List<Player> players = new List<Player>() { player };
        GetComponent<GameState>().SetupGame(players, 0, false);
    }

    public void JoinOnlineGame(List<Player> players, int mapId)
    {
        onlineLobby.SetActive(false);
        mainMenu.SetActive(false);
        GetComponent<GameState>().SetupGame(players, mapId, true);
    }

    public void UIRequestGame()
    {
        StartCoroutine(ShowAdSendRequest());
    }

    public IEnumerator ShowAdSendRequest()
    {
        float delay = User.adsRemoved ? 0 : (ShowVideoAd() ? 5 : 0);
        yield return new WaitForSeconds(delay);
        SendGameRequest();
    }

    public void SendGameRequest()
    {
        if (GetComponent<NetworkController>().connected)
        {
            GameRequest request = new GameRequest(User.playerId, 0);
            string json = JsonUtility.ToJson(request);
            NetPack pack = new NetPack("request", json);
            GetComponent<NetworkController>().SendPack(pack);
        }
    }

    public void ClearGameRequest()
    {
        if (GetComponent<NetworkController>().connected)
        {
            NetPack pack = new NetPack("clearrequest", "");
            GetComponent<NetworkController>().SendPack(pack);
        }
    }

    public void CheckBounds()
    {
        Vector2 minBounds = new Vector2(-2, -5);
        Vector2 maxBounds = new Vector2(10, 5);

        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
        {
            minBounds = map.GetComponent<Map>().minBounds;
            maxBounds = map.GetComponent<Map>().maxBounds;
        }

        Vector3 pos = Camera.main.transform.position;

        if (pos.x > maxBounds.x)
        {
            pos = new Vector3(maxBounds.x, pos.y, pos.z);
        }
        else if (pos.x < minBounds.x)
        {
            pos = new Vector3(minBounds.x, pos.y, pos.z);
        }

        if (pos.z > maxBounds.y)
        {
            pos = new Vector3(pos.x, pos.y, maxBounds.y);
        }
        else if (pos.z < minBounds.y)
        {
            pos = new Vector3(pos.x, pos.y, minBounds.y);
        }

        Camera.main.transform.position = pos;
    }

    public void ShowOnlyNearRegions()
    {
        Transform regions = GameObject.FindGameObjectWithTag("Map").transform.GetChild(0);
        for (int i = 0; i < regions.childCount; i++)
        {
            if (selectedRegion.IsRegionNeighbor(regions.GetChild(i).gameObject.GetComponent<Region>().id) ||
                selectedRegion.id == regions.GetChild(i).gameObject.GetComponent<Region>().id)
            {
                Player p = GetComponent<GameState>().GetPlayerById(regions.GetChild(i).gameObject.GetComponent<Region>().owner);
                bool protectedUser = false;
                if (p != null && p.magicCard != null)
                {
                    protectedUser = p.magicCard.id == "Protection";
                }

                if (!protectedUser)
                {
                    regions.GetChild(i).gameObject.SetActive(true);
                }
                else
                {
                    regions.GetChild(i).gameObject.SetActive(false);
                }
            }
            else
            {
                regions.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
    
    public void ShowAllRegions()
    {
        Transform regions = GameObject.FindGameObjectWithTag("Map").transform.GetChild(0);
        for (int i = 0; i < regions.childCount; i++)
        {
            regions.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void HideAllRegions()
    {
        Transform regions = GameObject.FindGameObjectWithTag("Map").transform.GetChild(0);
        for (int i = 0; i < regions.childCount; i++)
        {
            regions.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ShowRegionUI()
    {
        UIRegion.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = targetRegion.id;
        UIRegion.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = targetRegion.owner == "" ? "< Unclaimed >" : targetRegion.owner;
        Player player = GetComponent<GameState>().GetPlayerById(targetRegion.owner);
        UIRegion.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().color = player == null ? Color.white : player.color;
        UIRegion.SetActive(true);
    }

    public void HideRegionUI()
    {
        //StartCoroutine(DeselectRegions(0));
        //ShowAllRegions();
        targetRegion.selected = false;
        targetRegion = null;
        UIRegion.SetActive(false);
    }

    public void Attack()
    {
        GameObject[] pawns = GameObject.FindGameObjectsWithTag("Pawn");
        foreach (GameObject p in pawns)
        {
            Destroy(p);
        }

        StartCoroutine(DeselectRegions(0.0f));
        ShowAllRegions();
        GetComponent<GameState>().movable = false;

        float chance = GetComponent<GameState>().CalculateChance(selectedRegion.owner, targetRegion.owner);
        if (GetComponent<GameState>().isOnlineGame)
        {
            NPDAttack act = new NPDAttack(selectedRegion.id, targetRegion.id, Random.value <= chance);
            string json = JsonUtility.ToJson(act);
            NetPack pack = new NetPack("attack", json);
            GetComponent<NetworkController>().SendPack(pack);
        }
        else
        {
            StartCoroutine(GetComponent<GameState>().AttackToRegion(selectedRegion.id, targetRegion.id, Random.value <= chance));
        }
    }

    private void ManageCamera()
    {
        if (isActionCamera)
        {
            if (camTarget != null)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(new Vector3(15, 0, 0)), 90 * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, camTarget.position + new Vector3(0, 100f, -300f), 1200 * Time.deltaTime);
            }
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(new Vector3(90, 0, 0)), 90 * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, camPos, 1300 * Time.deltaTime);
        }
    }

    public void Click()
    {
        GetComponent<AudioSource>().PlayOneShot(click);
    }

    public void QuitGame()
    {
        GetComponent<NetworkController>().SaveUserData();
        GetComponent<NetworkController>().signalThread.Interrupt();
        GetComponent<NetworkController>().thread.Interrupt();
        Application.Quit();
    }

    public void UIRegister_ListFlags()
    {
        UnityEngine.UI.ScrollRect rect;
        if (FBregisterMenu.activeSelf)
        {
            rect = FBregisterMenu.transform.GetChild(0).GetChild(5).GetComponent<UnityEngine.UI.ScrollRect>();
        }
        else
        {
            rect = registerMenu.transform.GetChild(0).GetChild(5).GetComponent<UnityEngine.UI.ScrollRect>();
        }

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

        List<Texture2D> flags = GetComponent<GameState>().flags;
        for (int i = 0; i < flags.Count; i++)
        {
            GameObject icon = Instantiate(button_selectFlag);
            icon.GetComponent<Button_SelectFlag>().index = i;
            icon.GetComponent<UnityEngine.UI.RawImage>().texture = flags[i];
            icon.transform.SetParent(rect.content);
            icon.GetComponent<RectTransform>().localScale = Vector3.one;
            icon.GetComponent<RectTransform>().localRotation = Quaternion.identity;
            icon.GetComponent<RectTransform>().localPosition = new Vector3(icon.GetComponent<RectTransform>().localPosition.x,
                icon.GetComponent<RectTransform>().localPosition.y, 0);
        }
    }

    public void UIRegister_SelectFlag(int index = 0)
    {
        if (FBregisterMenu.activeSelf)
        {
            FBregisterMenu.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = GetComponent<GameState>().flags[index];
        }
        else
        {
            registerMenu.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = GetComponent<GameState>().flags[index];
        }
        
        User.flag_base64 = index.ToString();
    }

    public void UIFBRegister_Save()
    {
        string name = FBregisterMenu.transform.GetChild(0).GetChild(4).GetComponent<TMPro.TMP_InputField>().text;
        NPDPlayer p = new NPDPlayer(name, 0, User.flag_base64);
        if (CheckId(name))
        {
            NetPack pack = new NetPack("fbregister", JsonUtility.ToJson(p));
            GetComponent<NetworkController>().SendPack(pack);
        }
        else
        {
            ShowMessageBox("Allowed characters:\n[0-9] [a-z] [A-Z] (.) and (_)");
        }
    }

    public void UIRegister_Save()
    {
        string name = registerMenu.transform.GetChild(0).GetChild(4).GetComponent<TMPro.TMP_InputField>().text;
        string password = registerMenu.transform.GetChild(0).GetChild(7).GetComponent<TMPro.TMP_InputField>().text;
        NPDPlayer p = new NPDPlayer(name + ";" + password, 0, User.flag_base64);
        if (CheckId(name))
        {
            NetPack pack = new NetPack("register", JsonUtility.ToJson(p));
            GetComponent<NetworkController>().SendPack(pack);
        }
        else
        {
            ShowMessageBox("Allowed characters:\n[0-9] [a-z] [A-Z] (.) and (_)");
        }
    }

    public void Register()
    {
        string name = registerMenu.transform.GetChild(0).GetChild(4).GetComponent<TMPro.TMP_InputField>().text;
        string password = registerMenu.transform.GetChild(0).GetChild(7).GetComponent<TMPro.TMP_InputField>().text;
        Texture2D flagTexture = (Texture2D)registerMenu.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture;
        //string flag_base64 = Texture2DToBase64(flagTexture);

        User.playerId = name;
        User.password = password;
        //User.flag_base64 = flag_base64;
        id = name;
        flag = flagTexture;
        GetComponent<NetworkController>().SaveUserData();
        registerMenu.SetActive(false);
    }

    public void LoginToAccount()
    {
        string id = registerMenu.transform.GetChild(1).GetChild(2).GetComponent<TMPro.TMP_InputField>().text;
        string pw = registerMenu.transform.GetChild(1).GetChild(3).GetComponent<TMPro.TMP_InputField>().text;
        GetComponent<NetworkController>().Login(id + ";" + pw);
    }

    public bool CheckId(string name)
    {
        string checkList = "1234567890_abcçdefgğhıijklmnoöprsştuüvyzxqwABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZXQW.";
        bool valid = true;

        if (name.Length > 0)
        {
            foreach (char c in name)
            {
                valid = false;
                foreach (char letter in checkList)
                {
                    if (c == letter)
                    {
                        valid = true;
                        break;
                    }
                }

                if (!valid)
                    break;
            }
        }

        if (name.Length > 16 || name.Length < 3)
            valid = false;

        return valid;
    }

    public void BrowseFiles()
    {
        FileBrowser.OnSuccess onSuccess = LoadFile;
        FileBrowser.SetFilters(false, new string[] { ".png", ".jpg", ".bmp" });
        FileBrowser.ShowLoadDialog(onSuccess, LoadFileCanceled, FileBrowser.PickMode.Files, false);
    }

    public void LoadFile(string[] paths)
    {
        if (paths.Length > 0)
        {
            StartCoroutine(LoadCustomFlag(paths[0]));
        }
    }

    public IEnumerator LoadCustomFlag(string url)
    {
        Debug.Log(url);
        using (WWW www = new WWW(url))
        {
            yield return www;
            try
            {
                Texture2D f = new Texture2D(1, 1);
                www.LoadImageIntoTexture(f);
                User.flag_base64 = Texture2DToBase64(flag);
                flag = f;
                ConsumeMagicCard("Raise Own Flag");
                GetComponent<NetworkController>().SaveUserData();

            }
            catch
            {
                Debug.Log("Canceled");
            }
        }
    }

    public void LoadFileCanceled()
    {
        Debug.Log("Canceled");
    }

    public Texture2D Base64ToTexture2D(string base64)
    {
        if (base64 == "-1")
        {
            return questionMark;
        }
        else if (base64.Length > 8)
        {
            byte[] flagData = System.Convert.FromBase64String(base64);
            Texture2D flag = new Texture2D(1, 1);
            flag.LoadImage(flagData);
            return flag;
        }
        else
        {
            return GetComponent<GameState>().flags[int.Parse(base64)];
        }
    }

    public string Texture2DToBase64(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToJPG();
        /*
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, t);
            bytes = ms.ToArray();
        }
        */
        return System.Convert.ToBase64String(bytes);
    }

    public void ShowLeaderboard()
    {
        if (GetComponent<NetworkController>().connected)
        {
            NetPack pack = new NetPack("leaderboard");
            GetComponent<NetworkController>().SendPack(pack);
        }

        leaderboard.SetActive(true);
    }

    public void ShowRewardedAds()
    {
        if (GetComponent<NetworkController>().connected)
        {
            if (this.rewardedAd.IsLoaded())
            {
                this.rewardedAd.Show();
            }
        }
        else
        {
            GetComponent<NetworkController>().connectionError = true;
        }
    }

    public bool ShowVideoAd()
    {
        bool show = (System.DateTime.Now > lastAdTime.AddMinutes(10)) && GetComponent<NetworkController>().connected;

        if (show)
        {
            lastAdTime = System.DateTime.Now;
            videoAd.Show();
        }

        return show;
    }

    public RewardedAd CreateRewardedAd()
    {
        string adUnitId;

        #if UNITY_ANDROID
            adUnitId = "ca-app-pub-5042501351321565/3616185880";
        #elif UNITY_IPHONE
            adUnitId = "ca-app-pub-5042501351321565/4603153863";
        #else
            adUnitId = "unexpected_platform";
        #endif

        RewardedAd ad = new RewardedAd(adUnitId);
        ad.OnUserEarnedReward += HandleUserEarnedReward;
        ad.OnAdClosed += HandleRewardedAdClosed;
        AdRequest request = new AdRequest.Builder().Build();
        ad.LoadAd(request);

        return ad;
    }

    private InterstitialAd CreateVideoAd()
    {
        #if UNITY_ANDROID
            string adUnitId = "ca-app-pub-5042501351321565/1354459409";
        #elif UNITY_IPHONE
            string adUnitId = "ca-app-pub-5042501351321565/6091780764";
        #else
            string adUnitId = "unexpected_platform";
        #endif

        InterstitialAd ad = new InterstitialAd(adUnitId);
        AdRequest request = new AdRequest.Builder().Build();
        ad.LoadAd(request);

        return ad;
    }

    public void HandleUserEarnedReward(object sender, Reward args)
    {
        User.coins += 25;
        GetComponent<AudioSource>().PlayOneShot(GetComponent<GameState>().sound_coins);
        GetComponent<NetworkController>().SaveUserData();
    }

    public void HandleRewardedAdClosed(object sender, System.EventArgs args)
    {
        rewardedAd = CreateRewardedAd();
    }

    public void UIRefreshMyAssets()
    {
        myAssets.transform.GetChild(2).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = User.coins.ToString();

        if (magicCards.Count > 0)
        {
            myAssets.transform.GetChild(4).gameObject.SetActive(true);
            bool notselected = myAssets.transform.GetChild(4).GetComponent<UIMagicCardInfo>().selected == null || myAssets.transform.GetChild(4).GetComponent<UIMagicCardInfo>().selected.id == "";
            myAssets.transform.GetChild(4).GetComponent<UIMagicCardInfo>().SelectCard(notselected ? magicCards[0].id : myAssets.transform.GetChild(4).GetComponent<UIMagicCardInfo>().selected.id);
        }
        else
        {
            myAssets.transform.GetChild(4).gameObject.SetActive(false);
        }

        UnityEngine.UI.ScrollRect rect = myAssets.transform.GetChild(3).GetComponent<UnityEngine.UI.ScrollRect>();

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

        foreach (MagicCard c in GetComponent<PlayerController>().magicCards)
        {
            GameObject magicCard = Instantiate(UIMagicCard);
            magicCard.GetComponent<UIMagicCard>().id = c.id;
            magicCard.GetComponent<UnityEngine.UI.Image>().sprite = c.sprite;

            magicCard.transform.SetParent(rect.content);
            magicCard.GetComponent<RectTransform>().localScale = Vector3.one;
            magicCard.GetComponent<RectTransform>().localRotation = Quaternion.identity;
            magicCard.GetComponent<RectTransform>().localPosition = new Vector3(magicCard.GetComponent<RectTransform>().localPosition.x,
                magicCard.GetComponent<RectTransform>().localPosition.y, 0);
        }
    }


    public void UseMagicCard(NPDUseCard card)
    {
        if (card.id == "Raise Own Flag")
        {
            BrowseFiles();
        }
        else if (card.id == "Remove Ads")
        {
            User.adsRemoved = true;
            ConsumeMagicCard(card.id);
            ShowMessageBox("All ads removed.");
        }
        else
        {
            if (GetComponent<GameState>().isPlaying && GetComponent<GameState>().isOnlineGame)
            {
                GetComponent<GameState>().UseMagicCardInGame(card);
            }
            else
            {
                ShowMessageBox("This card can be used in online game only.");
            }
        }
    }

    public void ConsumeMagicCard(string cardId)
    {
        for (int i = 0; i < User.magicCards.Count; i++)
        {
            if (User.magicCards[i].id == cardId)
            {
                User.magicCards[i].quantity--;

                GetComponent<NetworkController>().SaveUserData();
                UIRefreshMyAssets();
                break;
            }
        }
    }

    public void ShowMessageBox(string message)
    {
        messageBox.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = message;
        messageBox.SetActive(true);
    }

    public void GetSoundVolume()
    {
        settings.transform.GetChild(2).GetComponent<UnityEngine.UI.Slider>().value = User.soundVolume;
    }

    public void SetSoundVolume()
    {
        User.soundVolume = settings.transform.GetChild(2).GetComponent<UnityEngine.UI.Slider>().value;

        GetComponent<AudioSource>().volume = User.soundVolume;
        mainMenu.GetComponent<AudioSource>().volume = User.soundVolume;
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
            map.GetComponent<AudioSource>().volume = User.soundVolume;

        GameObject[] pawns = GameObject.FindGameObjectsWithTag("Pawn");
        foreach (GameObject p in pawns)
        {
            p.GetComponent<AudioSource>().volume = User.soundVolume;
        }
    }

    public void SendChatMessage()
    {
        if (chatPanel.transform.GetChild(1).GetComponent<TMPro.TMP_InputField>().text != "")
        {
            string text = chatPanel.transform.GetChild(1).GetComponent<TMPro.TMP_InputField>().text;
            NPDChatMessage msg = new NPDChatMessage(User.playerId, text);
            NetPack pack = new NetPack("chatmsg", JsonUtility.ToJson(msg));
            GetComponent<NetworkController>().SendPack(pack);
            chatPanel.transform.GetChild(1).GetComponent<TMPro.TMP_InputField>().text = "";
        }
    }

    public void ClearChat()
    {
        UnityEngine.UI.ScrollRect rect = chatPanel.transform.GetChild(0).GetComponent<UnityEngine.UI.ScrollRect>();
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
    }

    public void ToggleChat(bool show)
    {
        chatPanel.SetActive(false);
        chatButton.SetActive(show);
    }

    public void TapFX()
    {
        GetComponent<AudioSource>().PlayOneShot(magicCardSoundFX, 4);
        GameObject tap = Instantiate(tapFX);
        tap.transform.parent = GameObject.Find("Canvas").transform;
        tap.transform.rotation = Quaternion.identity;
        tap.transform.localScale = Vector3.one;
        tap.transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        Destroy(tap, 1);
    }
}