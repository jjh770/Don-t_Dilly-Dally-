using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SceneRPCView : PunSingleton<SceneRPCView>
{
    public void PlaySfxForAll(SFXKey key)
    {
        photonView.RPC(nameof(RPC_PlaySfx), RpcTarget.All, (int)key);
    }

    [PunRPC]
    private void RPC_PlaySfx(int key)
    {
        SoundManager.Instance.Play((SFXKey)key, SoundType.Local);
    }
}