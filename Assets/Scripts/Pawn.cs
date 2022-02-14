using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pawn : MonoBehaviour
{
    public AudioClip sound_slice;
    public AudioClip sound_hit;
    public AudioClip sound_bodyfall;
    public List<AudioClip> sounds_death = new List<AudioClip>();

    private Transform ownerRegion;
    private Transform targetRegion;
    private Transform targetPawn;
    
    private bool isAttacker = false;
    private bool isSuccess = false;

    private int sequence = -1;

    private bool appearing = true;
    private bool disappearing = false;

    private bool ownerIsPlayer = false;
    private bool targetIsPlayer = false;

    private float scaleMultiply = 75f;

    public GameObject UIVictoryBanner;
    public GameObject UIDefeatBanner;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (appearing)
        {
            float scale = transform.localScale.x + (Time.deltaTime * scaleMultiply);
            if (scale < 0.5f * scaleMultiply)
            {
                transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                transform.localScale = new Vector3(0.5f * scaleMultiply, 0.5f * scaleMultiply, 0.5f * scaleMultiply);
                appearing = false;
                sequence = 0;
                if (isAttacker)
                {
                    GetComponent<Animator>().SetBool("IsMoving", true);
                }
            }
        }
        else if (disappearing)
        {
            float scale = transform.localScale.x - (Time.deltaTime * scaleMultiply);
            if (scale > 0.1f * scaleMultiply)
            {
                transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                if (!isAttacker)
                {
                    //GameObject.FindGameObjectWithTag("CoinUI").SetActive(false);
                    Camera.main.GetComponent<GameState>().PassTurn();
                }

                Destroy(this.gameObject);
            }
        }

        Vector3 targetRegionPos = targetRegion.GetChild(0).position;
        targetRegionPos.y = 0.4f * scaleMultiply;
        RotateToTarget(targetRegionPos);

        if (sequence == 0)
        {
            if (targetPawn != null && Vector3.Distance(transform.position, targetPawn.position) < 0.75f * scaleMultiply)
            {
                sequence = 1;
                StartCoroutine(Act());
            }
            else
            {
                if (isAttacker)
                {
                    transform.position += transform.forward * (Time.deltaTime * scaleMultiply);
                }
                else
                {
                    RotateToTarget(targetPawn.position);
                }
            }
        }
        else if (sequence == 4 && isAttacker)
        {
            if (Vector3.Distance(transform.position, targetRegionPos) > 0.4f * scaleMultiply)
            {
                transform.position += transform.forward * (Time.deltaTime * scaleMultiply);
            }
            else
            {
                Idle();
                sequence = -1;
                disappearing = true;
                Camera.main.GetComponent<GameState>().Conquer(targetRegion.GetComponent<Region>(), ownerRegion.GetComponent<Region>().owner, ownerIsPlayer && targetIsPlayer ? 5 : 1);
            }
        }
        else if (sequence == 3 && !isAttacker)
        {
            if (Vector3.Distance(transform.position, targetRegionPos) > 0.4f * scaleMultiply)
            {
                transform.position += transform.forward * (Time.deltaTime * scaleMultiply);
            }
            else
            {
                Idle();
                sequence = -1;
                disappearing = true;
                Camera.main.GetComponent<GameState>().Conquer(targetRegion.GetComponent<Region>(), ownerRegion.GetComponent<Region>().owner, ownerIsPlayer && targetIsPlayer ? 5 : 1);
            }
        }
    }

    public void Init(string Owner, Transform OwnerRegion, Transform TargetRegion, Transform TargetPawn, bool IsAttacker, bool IsSuccess)
    {
        ownerRegion = OwnerRegion;
        targetRegion = TargetRegion;
        targetPawn = TargetPawn;
        isAttacker = IsAttacker;
        isSuccess = IsSuccess;

        transform.localScale = Vector3.zero;
        transform.position = new Vector3(ownerRegion.GetChild(0).position.x, 0.55f * scaleMultiply, ownerRegion.GetChild(0).position.z);

        ownerIsPlayer = Camera.main.GetComponent<GameState>().GetPlayerById(ownerRegion.GetComponent<Region>().owner) != null;
        targetIsPlayer = Camera.main.GetComponent<GameState>().GetPlayerById(targetRegion.GetComponent<Region>().owner) != null;
        if (ownerIsPlayer)
            transform.GetChild(1).GetComponent<Renderer>().materials[1].mainTexture = 
                Camera.main.GetComponent<GameState>().GetPlayerById(Owner).flag;

        RotateToTarget(targetPawn.position);
        GetComponent<AudioSource>().volume = User.soundVolume;
    }

    public void RotateToTarget(Vector3 targetPos)
    {
        Vector3 lookPos = targetPos - transform.position;
        lookPos.y = 0;
        transform.rotation = Quaternion.LookRotation(lookPos);
    }

    IEnumerator Act()
    {
        if (sequence == 1)
        {
            if (isAttacker)
            {
                GetComponent<Animator>().SetBool("IsAttacking", true);
                RotateToTarget(targetPawn.position);

            }
            else
            {
                if (isSuccess)
                {
                    yield return new WaitForSeconds(0.5f);
                    GetComponent<Animator>().SetBool("IsDefending", true);
                    StartCoroutine(PlaySound(sound_hit, 0.25f));
                    RotateToTarget(targetPawn.position);

                    ShowBanner(UIDefeatBanner);
                }
                else
                {
                    yield return new WaitForSeconds(0.9f);
                    GetComponent<Animator>().SetBool("IsDying", true);

                    ShowBanner(UIVictoryBanner);

                    StartCoroutine(PlaySound(sound_slice, 0f));
                    StartCoroutine(PlaySound(sound_bodyfall, 0.2f));
                    StartCoroutine(PlaySound(sounds_death[Random.Range(0, sounds_death.Count)], 0f));
                }
            }
        }
        else
        {
            if (!isAttacker)
            {
                GetComponent<Animator>().SetBool("IsAttacking", true);
                RotateToTarget(targetPawn.position);
            }
            else
            {
                StartCoroutine(PlaySound(sound_slice, 1.6f));
                StartCoroutine(PlaySound(sound_bodyfall, 2.1f));
                StartCoroutine(PlaySound(sounds_death[Random.Range(0, sounds_death.Count)], 1.6f));
                yield return new WaitForSeconds(1.7f);
                GetComponent<Animator>().SetBool("IsDying", true);

                ShowBanner(UIDefeatBanner);

                yield return new WaitForSeconds(2.4f);
                disappearing = true;
            }
        }

        StartCoroutine(CheckSequences(sequence == 2 ? 2.4f : 1.75f));
    }

    IEnumerator CheckSequences(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Idle();

        if (sequence == 2)
        {
            sequence = 3;
            GetComponent<Animator>().SetBool("IsMoving", true);
            Camera.main.GetComponent<PlayerController>().camTarget = transform;
        }
        else
        {
            bool AttackerIsProtected = false;
            if (isAttacker)
            {
                Player p = Camera.main.GetComponent<GameState>().GetPlayerById(ownerRegion.GetComponent<Region>().owner);
                if (p != null && p.magicCard != null && p.magicCard.id == "Protection")
                {
                    AttackerIsProtected = true;
                }
            }
            else
            {
                Player p = Camera.main.GetComponent<GameState>().GetPlayerById(targetRegion.GetComponent<Region>().owner);
                if (p != null && p.magicCard != null && p.magicCard.id == "Protection")
                {
                    AttackerIsProtected = true;
                }
            }
            
            if ((isAttacker && ownerIsPlayer && targetIsPlayer && !isSuccess && !AttackerIsProtected) || 
                (!isAttacker && ownerIsPlayer && targetIsPlayer && isSuccess && !AttackerIsProtected))
            {
                sequence = 2;
                StartCoroutine(Act());
            }
            else
            {
                if (isAttacker)
                {
                    if (isSuccess)
                    {
                        yield return new WaitForSeconds(0.3f);
                        sequence = 4;
                        GetComponent<Animator>().SetBool("IsMoving", true);
                    }
                    else
                    {
                        sequence = -1;
                        disappearing = true;
                    }
                }
                else
                {
                    sequence = -1;
                    disappearing = true;
                }
            }
        }
    }

    void Idle()
    {
        GetComponent<Animator>().SetBool("IsMoving", false);
        GetComponent<Animator>().SetBool("IsAttacking", false);
        GetComponent<Animator>().SetBool("IsDefending", false);
        GetComponent<Animator>().SetBool("IsDamaging", false);
    }

    IEnumerator PlaySound(AudioClip sound, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        GetComponent<AudioSource>().pitch = Random.Range(0.8f, 1.2f);
        GetComponent<AudioSource>().PlayOneShot(sound);
    }

    void ShowBanner(GameObject bannerPrefab)
    {
        GameObject banner = Instantiate(bannerPrefab);
        banner.transform.parent = GameObject.FindGameObjectWithTag("GameUI").transform;
        banner.GetComponent<RectTransform>().localScale = Vector3.one;
        banner.GetComponent<RectTransform>().localRotation = Quaternion.identity;
        banner.GetComponent<RectTransform>().localPosition = new Vector3(0, -150, 0);
        banner.GetComponent<Animator>().Play("Anim");
        Destroy(banner, 2);
    }
}
