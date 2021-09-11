using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotationRange;
    private Vector3 mouseInput, movement;

    public bool invertlook;

    public CharacterController charcon;

    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    private float activeMoveSpeed;

    private Camera cam;
    private Vector3 moveDirection;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public float jumpForce = 5f, gravityMod = 2.5f;

    public GameObject bulletImpact;
    //public float timeBetweenShots = 0.1f;
    private float shotCounter;

    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f, /* heatPerShot = 1f,*/ coolRate = 4f, overHeatCoolRate = 5;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int gunSelected;

    public GameObject playerHitImapct;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;

    public GameObject playerModel;

    public Transform modelGunPoint;
    public Transform gunHolder;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;
        UIController.instance.weaponTemp.maxValue = maxHeat;

        // switchGun();
        photonView.RPC("SetGun", RpcTarget.All, gunSelected);


        if (photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotationRange += mouseInput.y;
            verticalRotationRange = Mathf.Clamp(verticalRotationRange, -60, 60);

            if (invertlook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotationRange, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);

            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotationRange, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);

            }

            moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, (Input.GetAxisRaw("Vertical")));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
            }
            else
            {
                activeMoveSpeed = moveSpeed;
            }
            float yvelocity = movement.y;

            movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
            movement.y = yvelocity;
            if (charcon.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayers);
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charcon.Move(movement * Time.deltaTime);

            if (allGuns[gunSelected].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if (muzzleCounter <= 0)
                {
                    allGuns[gunSelected].muzzleFlash.SetActive(false);

                }

            }

            if (!overHeated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[gunSelected].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }
                heatCounter -= coolRate * Time.deltaTime;
            }
            else
            {
                heatCounter -= overHeatCoolRate * Time.deltaTime;
                if (heatCounter <= 0)
                {

                    overHeated = false;
                    UIController.instance.overheatedText.gameObject.SetActive(false);
                }

            }

            if (heatCounter < 0)
            {
                heatCounter = 0;
            }

            UIController.instance.weaponTemp.value = heatCounter;

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                gunSelected += 1;

                if (gunSelected >= allGuns.Length)
                {
                    gunSelected = 0;
                }
                photonView.RPC("SetGun", RpcTarget.All, gunSelected);
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                gunSelected -= 1;

                if (gunSelected < 0)
                {
                    gunSelected = allGuns.Length - 1;
                }
                photonView.RPC("SetGun", RpcTarget.All, gunSelected);
            }

            for(int i = 0; i < allGuns.Length; i ++)
            {
                if(Input.GetKeyDown((i+1).ToString()))
                {
                    gunSelected = i;
                    photonView.RPC("SetGun", RpcTarget.All, gunSelected);
                }
            }
            anim.SetBool("grounded", isGrounded);
            anim.SetFloat("speed", moveDirection.magnitude);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }


    void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.tag == "Player")
            {

                PhotonNetwork.Instantiate(playerHitImapct.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[gunSelected].shotDamage);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

                Destroy(bulletImpactObject, 10f);
            }
            //Debug.Log(hit.collider.gameObject.name);
        }



        shotCounter = allGuns[gunSelected].timeBetweenShots;


        heatCounter += allGuns[gunSelected].heatPerShot;
        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedText.gameObject.SetActive(true);
        }

        allGuns[gunSelected].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount)
    {
        TakeDamage(damager, damageAmount);
    }

    public void TakeDamage(string damager, int damageAmount)
    {
        if (photonView.IsMine)
        {
            //  Debug.Log(photonView.Owner.NickName + "has been hit by " + damager);

            currentHealth -= damageAmount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
            }

            UIController.instance.healthSlider.value = currentHealth;
        }
    }
    void LateUpdate()
    {
        if (photonView.IsMine)
        {
            cam.transform.position = viewPoint.position;
            cam.transform.rotation = viewPoint.rotation;
        }
    }

    void switchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[gunSelected].gameObject.SetActive(true);
        allGuns[gunSelected].muzzleFlash.SetActive(false);
    }
    [PunRPC]
    public void SetGun(int gunToSwitch)
    {
        if(gunToSwitch < allGuns.Length)
        {
            gunSelected = gunToSwitch;
            switchGun();
        }
    }
}
