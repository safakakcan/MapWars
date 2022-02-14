using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public static class User
{
    public static string playerId = "";
    public static string password = "";
    public static string fbId = "";
    public static int gameId = -1;
    public static string flag_base64 = "-1";
    public static int score = 0;
    public static int coins = 0;
    public static List<MagicCardData> magicCards = new List<MagicCardData>();
    public static string clan = "";
    public static List<string> friends = new List<string>();
    public static bool adsRemoved = false;
    public static float soundVolume = 1;
}

public class UserData
{
    public string playerId = "";
    public string password = "";
    public string fbId = "";
    public int gameId = -1;
    public string flag_base64 = "-1";
    public int score = 0;
    public int coins = 0;
    public List<string> magicCards = new List<string>();
    public string clan = "";
    public List<string> friends = new List<string>();
    public bool adsRemoved = false;
    public float soundVolume = 1;
}

public class Player
{
    public bool resigned = false;
    public bool defeated = false;
    public bool isHuman = false;
    public string id = "";
    public int score = 0;
    public Region capital = null;
    public Color color;
    public Texture2D flag;
    public int coins = 0;
    public MagicCardData magicCard = null;

    public Player (string Id, Color Color, Texture2D Flag, bool IsHuman = false)
    {
        id = Id;
        color = Color;
        flag = Flag;
        isHuman = IsHuman;
    }
}

[System.Serializable]
public class MagicCard
{
    public Sprite sprite;
    public string id = "";
    public string name = "";
    public string desc = "";
    public bool onlyInGame = false;
    public int turn = 0;
    public int price = 0;

    public MagicCard(string Id, string Desc, bool OnlyInGame, int Price)
    {
        id = Id;
        desc = Desc;
        onlyInGame = OnlyInGame;
        price = Price;
    }
}

public class MagicCardData
{
    public string id = "";
    public int quantity = 0;
    public int turn = 0;

    public MagicCardData(string Id, int Quantity, int Turn)
    {
        id = Id;
        quantity = Quantity;
        turn = Turn;
    }
}

public class OnlineGameData
{
    public int mapId;
    public List<Player> players;

    public OnlineGameData(int MapId, List<Player> Players)
    {
        mapId = MapId;
        players = Players;
    }
}

public class GameRequest
{
    public string id;
    public int map;

    public GameRequest (string PlayerId, int MapId)
    {
        id = PlayerId;
        map = MapId;
    }
}

public class NetPack
{
    public string cmd = "";
    public string data = "";
    public int gameId = -1;

    public NetPack (string Command, string Data = "")
    {
        cmd = Command;
        data = Data;
        gameId = User.gameId;
    }
}

public class NPDAttack
{
    public string attacker;
    public string defender;
    public bool success;

    public NPDAttack(string Attacker, string Defender, bool Success)
    {
        attacker = Attacker;
        defender = Defender;
        success = Success;
    }
}

public class NPDSetup
{
    public int map = 0;
    public List<string> players = new List<string>();
}

public class NPDPlayer
{
    public string id = "";
    public int score = 0;
    public string flag_base64 = "";

    public NPDPlayer (string Id, int Score, string Flag_Base64)
    {
        id = Id;
        score = Score;
        flag_base64 = Flag_Base64;
    }
}

public class NPDLeaderboard
{
    public int page = 1;
    public List<string> players = new List<string>();
}

public class NPDGameOver
{
    public List<string> winners = new List<string>();
    public List<int> rewards = new List<int>();
}

public class NPDUseCard
{
    public string id = "";
    public string owner = "";
}

public class NPDChatMessage
{
    public string id = "";
    public string text = "";

    public NPDChatMessage(string Id, string Text)
    {
        id = Id;
        text = Text;
    }
}


public static class Game
{
    public static string[] botNames = new string[]
    {
        "jefreyy_",
        "potterhead_",
        "GR_Gandalf",
        "Pro_FighteR",
        "SLayeRR_",
        "KingKong_",
        "deadking",
        "fullmetall",
        "winTer",
        "6_bullet",
        "Oliver",
        "Şafak",
        "i_am_hero",
        "7even",
        "ARAGORN",
        "asdfg_"
    };
}