using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviourPun
{

    private int curAttackerId;

    public int curHp;
    public int maxHp;
    public int kills;
    public bool dead;
    private bool flashingDamage;
    public MeshRenderer mr;

    public PlayerWeapon weapon;

    [Header("Stats")]
    public float moveSpeed;
    public float jumpForce;

    [Header("Components")]
    public Rigidbody rig;

    public int id;
    public Player photonPlayer;

    void Update()
    {
        if (!photonView.IsMine || dead)
            return;

        Move();

        if (Input.GetMouseButtonDown(0))
            weapon.TryShoot();


        if (Input.GetKeyDown(KeyCode.Space))
            TryJump();

    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 dir = (transform.forward * z + transform.right * x) * moveSpeed;
        dir.y = rig.velocity.y;
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, 1.5f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    [PunRPC]
    public void Initialize(Player player)
    {
        id = player.ActorNumber;
        photonPlayer = player;
        GameManager.instance.players[id - 1] = this;

        if (!photonView.IsMine)
        {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            rig.isKinematic = true;
        }
        else
        {
            GameUI.instance.Initialize(this);
        }
    }

    [PunRPC]
    public void TakeDamage(int attackerId, int damage)
    {
        if (dead)
            return;
        curHp -= damage;
        curAttackerId = attackerId;

        photonView.RPC("DamageFlash", RpcTarget.Others);

        if (curHp <= 0)
            photonView.RPC("Die", RpcTarget.All);
        GameUI.instance.UpdateHealthBar();
    }

    [PunRPC]
    void DamageFlash()
    {
        if (flashingDamage)
            return;
        StartCoroutine(DamageFlashCoRoutine());
        IEnumerator DamageFlashCoRoutine()
        {
            flashingDamage = true;
            Color defaultColor = mr.material.color;
            mr.material.color = Color.red;
            yield return new WaitForSeconds(0.05f);
            mr.material.color = defaultColor;
            flashingDamage = false;
        }
    }

    [PunRPC]
    void Die()
    {
        curHp = 0;
        dead = true;
        GameManager.instance.alivePlayers--;
        // host will check win condition
        if (PhotonNetwork.IsMasterClient)
            GameManager.instance.CheckWinCondition();
        // is this our local player?
        if (photonView.IsMine)
        {
            if (curAttackerId != 0)
                GameManager.instance.GetPlayer(curAttackerId).photonView.RPC("AddKill", RpcTarget.All);
   
            GetComponentInChildren<CameraController>().SetAsSpectator();
    
            rig.isKinematic = true;
            transform.position = new Vector3(0, -50, 0);
        }


    }
    [PunRPC]
    public void AddKill()
    {
        kills++;
        GameUI.instance.UpdatePlayerInfoText();
    }
    [PunRPC]
    public void Heal(int amountToHeal)
    {
        curHp = Mathf.Clamp(curHp + amountToHeal, 0, maxHp);
        GameUI.instance.UpdateHealthBar();
    }

}
