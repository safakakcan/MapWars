using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
//using Facebook.Unity;
//using Facebook.MiniJSON;

public class NetworkController : MonoBehaviour
{
    public string version = "0.0.0";
    public string address = "37.148.212.209";
    public int port = 8090;
    public bool connected = false;
    public bool connectionError = false;
    public bool loginToAccount = false;
    public bool loginToFacebook = false;

    List<NetPack> buffer = new List<NetPack>();

    public TcpClient client;
    public NetworkStream stream;
    public StreamReader reader;
    public StreamWriter writer;
    public Thread thread;
    public Thread signalThread;
    public GameObject UIConnectionError;
    public GameObject UIConnection;
    public GameObject UILogoutQuestion;
    public GameObject UIOutOfDate;
    /*
    void Awake()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            //Handle FB.Init
            FB.Init(() => {
                FB.ActivateApp();
            });
        }
    }
    */

    // Start is called before the first frame update
    void Start()
    {
        //FB.Init(null, HideUnity);
        Thread th = new Thread(() => Connect());
        th.Start();
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<PlayerController>().mainMenu.transform.GetChild(2).GetComponent<UnityEngine.UI.Button>().interactable = connected;

        if (connectionError)
        {
            GetComponent<PlayerController>().onlineLobby.SetActive(false);
            UIConnectionError.SetActive(true);
            connectionError = false;
            Thread th = new Thread(() => Connect());
            th.Start();
        }
        else
        {
            foreach (NetPack pack in buffer)
            {
                ExecuteCommand(pack);
            }

            buffer.Clear();
        }

        if (loginToAccount)
        {
            LoadOrCreateUser();
            //FB_Login();
            loginToAccount = false;
            connectionError = false;
            UIConnectionError.SetActive(false);
            UIConnection.SetActive(false);
        }
    }

    void HideUnity(bool hide)
    {
        if (hide)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public void Connect()
    {
        Thread.Sleep(2000);

        try
        {
            client = new TcpClient(address, port);
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            connected = true;
            //loginToAccount = true;
            thread = new Thread(() => ListenHost());
            thread.Start();
            signalThread = new Thread(() => SendSignal());
            signalThread.Start();

            NetPack p = new NetPack("version", version);
            SendPack(p);
        }
        catch
        {
            connectionError = true;
        }
    }

    public void Login(string idpw)
    {
        NetPack pack = new NetPack("login", idpw);
        SendPack(pack);
    }

    public void Logout()
    {
        SaveUserData();
        ClearUserData();
        //FB.LogOut();
        LoadOrCreateUser();
    }

    public void Disconnect()
    {
        if (connected)
        {
            connected = false;
            thread.Interrupt();
            signalThread.Interrupt();
            client.Close();
        }
    }

    private void ListenHost()
    {
        while (connected && client.Connected)
        {
            string json = reader.ReadLine();
            NetPack pack = JsonUtility.FromJson<NetPack>(json);
            if (pack != null)
                buffer.Add(pack);
        }

        connectionError = true;
        connected = false;
    }

    public void SendPack(NetPack pack)
    {
        try
        {
            string json = JsonUtility.ToJson(pack);
            writer.WriteLine(json);
            writer.Flush();
        }
        catch
        {
            connectionError = true;
        }
    }

    private void ExecuteCommand(NetPack pack)
    {
        if (pack.cmd == "0")
        {
            NetPack answer = new NetPack("0");
            SendPack(answer);
        }
        else if (pack.cmd == "version")
        {
            if (version != pack.data)
            {
                UIOutOfDate.SetActive(true);
            }
            else
            {
                loginToAccount = true;
            }
        }
        else if (pack.cmd == "usecard")
        {
            NPDUseCard card = JsonUtility.FromJson<NPDUseCard>(pack.data);
            GetComponent<GameState>().UseMagicCardInGame(card);
        }
        else if (pack.cmd == "register")
        {
            if (pack.data == "1")
            {
                GetComponent<PlayerController>().Register();
            }
            else
            {
                GetComponent<PlayerController>().ShowMessageBox("This name is already taken.");
            }
        }
        else if (pack.cmd == "login")
        {
            if (pack.data == "0")
            {
                GetComponent<PlayerController>().ShowMessageBox("Wrong username or password!");
            }
            else
            {
                UserData u = JsonUtility.FromJson<UserData>(pack.data);
                LoadUserData(u);
                GetComponent<PlayerController>().registerMenu.SetActive(false);
                PlayerPrefs.SetString("user", User.playerId + ";" + User.password);
                PlayerPrefs.Save();
            }
        }
        else if (pack.cmd == "fbregister")
        {
            GetComponent<PlayerController>().registerMenu.SetActive(false);
            GetComponent<PlayerController>().FBregisterMenu.SetActive(true);
            GetComponent<PlayerController>().UIRegister_ListFlags();
        }
        else if (pack.cmd == "fblogin")
        {
            UserData u = JsonUtility.FromJson<UserData>(pack.data);
            LoadUserData(u);
            UIConnection.SetActive(false);
            GetComponent<PlayerController>().registerMenu.SetActive(false);
            GetComponent<PlayerController>().FBregisterMenu.SetActive(false);
        }
        else if (pack.cmd == "chatmsg")
        {
            //Handheld.Vibrate();
            UnityEngine.UI.ScrollRect rect = GetComponent<PlayerController>().chatPanel.transform.GetChild(0).GetComponent<UnityEngine.UI.ScrollRect>();
            NPDChatMessage data = JsonUtility.FromJson<NPDChatMessage>(pack.data);

            GameObject msg = Instantiate(GetComponent<PlayerController>().chatMsg);
            msg.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = data.id;
            msg.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = data.text;
            Color color = GetComponent<GameState>().GetPlayerById(data.id) != null ? GetComponent<GameState>().GetPlayerById(data.id).color : Color.white;
            msg.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().color = color;
            msg.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().color = color;

            msg.transform.SetParent(rect.content);
            msg.GetComponent<RectTransform>().localScale = Vector3.one;
            msg.GetComponent<RectTransform>().localRotation = Quaternion.identity;
            msg.GetComponent<RectTransform>().localPosition = new Vector3(msg.GetComponent<RectTransform>().localPosition.x,
                msg.GetComponent<RectTransform>().localPosition.y, 0);

            while (true)
            {
                if (rect.content.childCount > 100)
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

            StartCoroutine(AutoScroll());
        }
        else if (pack.cmd == "passturn")
        {
            GetComponent<GameState>().PassTurn();
        }
        else if (pack.cmd == "attack")
        {
            NPDAttack data = JsonUtility.FromJson<NPDAttack>(pack.data);
            StartCoroutine(GetComponent<GameState>().AttackToRegion(data.attacker, data.defender, data.success));
        }
        else if (pack.cmd == "setup")
        {
            User.gameId = pack.gameId;
            NPDSetup data = JsonUtility.FromJson<NPDSetup>(pack.data);

            List<Player> players = new List<Player>();

            foreach (string json in data.players)
            {
                NPDPlayer playerData = JsonUtility.FromJson<NPDPlayer>(json);
                Texture2D flag = GetComponent<PlayerController>().Base64ToTexture2D(playerData.flag_base64);
                Player player = new Player(playerData.id, Color.black, flag, true);
                players.Add(player);
            }

            GetComponent<GameState>().SetupGame(players, data.map, true);
            GetComponent<PlayerController>().onlineLobby.SetActive(false);
            GetComponent<PlayerController>().mainMenu.SetActive(false);
        }
        else if (pack.cmd == "leaderboard")
        {
            NPDLeaderboard data = JsonUtility.FromJson<NPDLeaderboard>(pack.data);
            UnityEngine.UI.ScrollRect rect = GetComponent<PlayerController>().leaderboard.transform.GetChild(2).gameObject.GetComponent<UnityEngine.UI.ScrollRect>();

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

            int sort = 1;
            foreach (string json in data.players)
            {
                NPDPlayer p = JsonUtility.FromJson<NPDPlayer>(json);
                
                GameObject entry = Instantiate(GetComponent<PlayerController>().boardEntry);
                entry.transform.SetParent(rect.content);

                entry.GetComponent<RectTransform>().localScale = Vector3.one;
                entry.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                entry.GetComponent<RectTransform>().localPosition = new Vector3(entry.GetComponent<RectTransform>().localPosition.x,
                    entry.GetComponent<RectTransform>().localPosition.y, 0);

                entry.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = GetComponent<PlayerController>().Base64ToTexture2D(p.flag_base64);
                entry.transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = p.id;
                entry.transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>().text = "Score: " + p.score.ToString();
                entry.transform.GetChild(4).GetComponent<TMPro.TextMeshProUGUI>().text = "#" + sort.ToString();
                sort++;
            }
        }
        else if (pack.cmd == "resign")
        {
            string AIController = GetComponent<GameState>().FindAIControllerId();
            GetComponent<GameState>().DisclaimRegions(pack.data);
            Player p = GetComponent<GameState>().GetPlayerById(pack.data);
            p.resigned = true;

            if (GetComponent<GameState>().activePlayerIndex == GetComponent<GameState>().FindPlayerIndexById(pack.data))
            {
                GetComponent<GameState>().PassTurn();
            }
            else
            {
                string newAIController = GetComponent<GameState>().FindAIControllerId();

                if (pack.data == AIController && GetComponent<PlayerController>().id == newAIController)
                {
                    GetComponent<AIController>().Act();
                }
            }
        }
        else if (pack.cmd == "gameover")
        {
            NPDGameOver data = JsonUtility.FromJson<NPDGameOver>(pack.data);
            GetComponent<GameState>().GameOver();
        }
    }

    public void LoadOrCreateUser()
    {
        if (PlayerPrefs.HasKey("user"))
        {
            string idpw = PlayerPrefs.GetString("user");
            if (IsFBID(idpw))
            {
                //FB_Login();
            }
            else
            {
                Login(idpw);
            }
        }
        else
        {
            GetComponent<PlayerController>().registerMenu.SetActive(true);
            GetComponent<PlayerController>().UIRegister_ListFlags();
        }
    }

    public bool IsFBID(string idpw)
    {
        try
        {
            return idpw.Split(';').Length <= 1;
        }
        catch
        {
            return false;
        }
    }

    public void LoadUserData(UserData data)
    {
        User.playerId = data.playerId;
        User.password = data.password;
        User.fbId = data.fbId;
        User.gameId = -1;
        User.flag_base64 = data.flag_base64;
        User.score = data.score;
        User.coins = data.coins;
        User.clan = data.clan;
        User.friends = data.friends;
        User.adsRemoved = data.adsRemoved;
        User.soundVolume = data.soundVolume;
        User.magicCards = new List<MagicCardData>();
        foreach (string j in data.magicCards)
        {
            MagicCardData card = JsonUtility.FromJson<MagicCardData>(j);
            User.magicCards.Add(card);
        }

        GetComponent<PlayerController>().GetSoundVolume();
        GetComponent<PlayerController>().id = User.playerId;
        GetComponent<PlayerController>().flag = GetComponent<PlayerController>().Base64ToTexture2D(User.flag_base64);
    }

    public void SaveUserData()
    {
        UserData data = new UserData();
        data.playerId = User.playerId;
        data.password = User.password;
        data.fbId = User.fbId;
        data.gameId = User.gameId;
        data.flag_base64 = User.flag_base64;
        data.score = User.score;
        data.coins = User.coins;
        data.clan = User.clan;
        data.friends = User.friends;
        data.adsRemoved = User.adsRemoved;
        data.soundVolume = User.soundVolume;
        foreach (MagicCardData c in User.magicCards)
        {
            string j = JsonUtility.ToJson(c);
            data.magicCards.Add(j);
        }

        PlayerPrefs.SetString("user", User.playerId + ";" + User.password);
        PlayerPrefs.Save();

        NetPack pack = new NetPack("update", JsonUtility.ToJson(data));
        SendPack(pack);
    }

    public void ClearUserData()
    {
        User.playerId = "";
        User.password = "";
        User.fbId = "";
        User.gameId = -1;
        User.flag_base64 = "0";
        User.score = 0;
        User.coins = 0;
        User.clan = "";
        User.friends = new List<string>();
        User.adsRemoved = false;
        User.soundVolume = 1;
        User.magicCards = new List<MagicCardData>();
        PlayerPrefs.DeleteAll();
    }

    public void SendSignal()
    {
        while (connected)
        {
            try
            {
                Thread.Sleep(8000);
            }
            catch { }

            SendPack(new NetPack("0"));
        }
    }

    public IEnumerator AutoScroll()
    {
        yield return new WaitForSeconds(0.1f);

        UnityEngine.UI.ScrollRect rect = GetComponent<PlayerController>().chatPanel.transform.GetChild(0).GetComponent<UnityEngine.UI.ScrollRect>();
        rect.verticalNormalizedPosition = 0;
    }

    public void ShowLogoutQuestion()
    {
        UILogoutQuestion.SetActive(true);
        UILogoutQuestion.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = string.Format("Are you sure you want to log out of user account named '{0}'?", User.playerId);
    }

    public void OpenGooglePlay()
    {
        Application.OpenURL("https://play.google.com/store/apps/details?id=com.AlbaCraft.MapWars");
    }

    public void FacebookLogin()
    {
        Application.OpenURL("https://www.facebook.com/v10.0/dialog/oauth?client_id=429397445014505&display=popup&redirect_uri=https://www.facebook.com/connect/login_success.html&response_type=token");
    }

    /*
    public void FB_Login()
    {
        if (FB.IsLoggedIn)
        {
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            NetPack pack = new NetPack("fblogin", aToken.UserId);
            SendPack(pack);
        }
        else
        {
            FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" }, AuthCallback);
        }
    }

    private void AuthCallback(ILoginResult result)
    {
        Debug.Log(result.RawResult);
        
        if (result == null || string.IsNullOrEmpty(result.Error))
        {
            if (FB.IsLoggedIn)
            {
                var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
                NetPack pack = new NetPack("fblogin", aToken.UserId);
                SendPack(pack);
            }
            else
            {
                Debug.Log("User cancelled login");
            }
        }
        else
        {
            GetComponent<PlayerController>().ShowMessageBox("Facebook Auth Failed!\n\n" + result.Error);
        }
    }
    */
}
