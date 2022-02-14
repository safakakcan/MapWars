using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Threading;
/*
public class IAPController : MonoBehaviour, IStoreListener
{
    IStoreController controller;

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        this.controller = controller;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("ERROR: " + error.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        GetComponent<PlayerController>().ShowMessageBox("Purchasing is failed!");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        string productId = purchaseEvent.purchasedProduct.definition.id;
        Debug.Log(productId);
        if (productId == "25k_coins")
        {
            AddCoins(25000);
            return PurchaseProcessingResult.Complete;
        }
        else if (productId == "50k_coins")
        {
            AddCoins(50000);
            return PurchaseProcessingResult.Complete;
        }
        else if (productId == "75k_coins")
        {
            AddCoins(75000);
            return PurchaseProcessingResult.Complete;
        }
        else if (productId == "test_1")
        {
            AddCoins(1);
            return PurchaseProcessingResult.Complete;
        }
        else
        {
            return PurchaseProcessingResult.Pending;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("25k_coins", ProductType.Consumable, new IDs
        {
            {"25k_coins_google", GooglePlay.Name},
            {"25k_coins_mac", MacAppStore.Name}
        });
        builder.AddProduct("50k_coins", ProductType.Consumable, new IDs
        {
            {"50k_coins_google", GooglePlay.Name},
            {"50k_coins_mac", MacAppStore.Name}
        });
        builder.AddProduct("75k_coins", ProductType.Consumable, new IDs
        {
            {"75k_coins_google", GooglePlay.Name},
            {"75k_coins_mac", MacAppStore.Name}
        });
        builder.AddProduct("test_1", ProductType.Consumable, new IDs
        {
            {"test_1", GooglePlay.Name},
            {"test_1", MacAppStore.Name}
        });
        UnityPurchasing.Initialize(this, builder);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void AddCoins(int coins)
    {
        User.coins += coins;
        GetComponent<NetworkController>().SaveUserData();
    }

    public void IAPButton_Click(string id)
    {
        Product product = controller.products.WithID(id);
        if (product != null && product.availableToPurchase)
        {

            controller.InitiatePurchase(product);
        }
        else
        {
            Debug.Log("ERROR");
        }
    }
}
*/