using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMagicCardInfo : MonoBehaviour
{
    public MagicCard selected = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectCard(string cardId)
    {
        MagicCardData data = null;
        foreach (MagicCardData c in User.magicCards)
        {
            if (c.id == cardId)
            {
                data = c;
                break;
            }
        }
        
        selected = GetCardInfo(cardId);
        transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = selected.sprite.texture;
        transform.GetChild(2).GetComponent<TMPro.TextMeshProUGUI>().text = selected.id;
        transform.GetChild(3).GetComponent<TMPro.TextMeshProUGUI>().text = selected.desc;
        transform.GetChild(4).GetComponent<TMPro.TextMeshProUGUI>().text = data == null ? "x0" : "x" + data.quantity.ToString();
        transform.GetChild(6).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = selected.price.ToString();
    }

    public MagicCard GetCardInfo(string cardId)
    {
        MagicCard card = null;

        foreach (MagicCard c in Camera.main.GetComponent<PlayerController>().magicCards)
        {
            if (c.id == cardId)
            {
                card = c;
                break;
            }
        }

        return card;
    }

    public void BuyMagicCard()
    {
        if (User.coins >= selected.price)
        {
            bool exist = false;
            foreach (MagicCardData c in User.magicCards)
            {
                if (c.id == selected.id)
                {
                    c.quantity++;
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                MagicCardData mc = new MagicCardData(selected.id, 1, selected.turn);
                User.magicCards.Add(mc);
            }

            User.coins -= selected.price;
            Camera.main.GetComponent<NetworkController>().SaveUserData();
            Camera.main.GetComponent<PlayerController>().UIRefreshMyAssets();
        }
        else
        {
            Camera.main.GetComponent<PlayerController>().ShowMessageBox("There is no enough coins.");
        }
    }

    public void UseMagicCard()
    {
        MagicCardData data = null;
        foreach (MagicCardData c in User.magicCards)
        {
            if (c.id == selected.id)
            {
                data = c;
                break;
            }
        }

        if (data != null && data.quantity > 0)
        {
            NPDUseCard card = new NPDUseCard();
            card.id = selected.id;
            card.owner = User.playerId;
            Camera.main.GetComponent<PlayerController>().UseMagicCard(card);
        }
    }
}