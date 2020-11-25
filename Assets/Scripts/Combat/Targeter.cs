using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targeter : NetworkBehaviour
{
    private Targetable target;

    #region Server
    [Command]
    public void CmdSetTarget(GameObject targetGameObject)
    {
        if (!targetGameObject.TryGetComponent<Targetable>(out Targetable target))
            return;

        this.target = target;
    }

    [Server]
    public void ClearTarget()
    {
        this.target = null;
    }
    #endregion
}
