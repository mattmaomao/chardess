using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkSetup : MonoBehaviour

{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] bool onStartHostServer = false;
    [SerializeField] bool connected = false;
    [SerializeField] GameObject loadingScreen;
    int timeOut = 1;
    
    void Start() {
        loadingScreen.SetActive(true);
        Debug.Log(networkManager.mode);
        if (onStartHostServer) {
            if (networkManager.mode == NetworkManagerMode.Offline) {
                networkManager.StartServer();
            }
            else {
                networkManager.StartClient();
                networkManager.networkAddress = "localhost";
            }
        }
    }

    void Update() {
        Debug.Log(networkManager.mode);
        if (NetworkClient.ready && !connected) {
            loadingScreen.SetActive(false);
            Debug.Log("connected");
            connected = true;
        }
    }
}
