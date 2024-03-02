using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MessageManager : NetworkBehaviour
{
    [SerializeField] GameObject msgTempObj;
    [SerializeField] Transform msgContainer;
    List<GameObject> msgObjs = new List<GameObject>();
    int maxMsg = 8;
    float msgDismissSec = 2f;

    [TargetRpc]
    public void addMsg(NetworkConnection networkConnection, string msg)
    {
        GameObject newMsg = Instantiate(msgTempObj, msgContainer);
        // Set the text of msg
        newMsg.GetComponentInChildren<Text>().text = msg;
        msgObjs.Add(newMsg);

        // Check if the number of messages exceeds the limit
        if (msgObjs.Count > maxMsg) {
            // Remove the oldest message from the list and destroy it
            GameObject oldMsg = msgObjs[0];
            msgObjs.RemoveAt(0);
            Destroy(oldMsg);
        }

        // start timer to destroy msg
        StartCoroutine(removeMessage(newMsg));
    }

    IEnumerator removeMessage(GameObject msgObj)
    {
        yield return new WaitForSeconds(msgDismissSec);

        // Remove the message object from the list and destroy it
        msgObjs.Remove(msgObj);
        Destroy(msgObj);
    }
}
