using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("----- [ CORE ] -----")]
    public bool isOver;
    public int score;
    public int maxLevel;

    [Header("----- [ OBJECT ] -----")]
    public List<Dongle> donglePool;
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<ParticleSystem> effectPool;
    public GameObject effectPrefab;
    public Transform effectGroup;
    [Range(1, 30)]
    public int poolSize;
    public int poolCursor;
    public Dongle lastDongle;

    [Header("----- [ AUDIO ] -----")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };

    [Header("----- [ TEXT ] -----")]
    public Text scoreTxt;

    int sfxCursor;

    void Awake()
    {
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        for(int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }
    }

    void Start()
    {
        bgmPlayer.Play();
        NextDongle();
    }

    Dongle MakeDongle()
    {
        // 이펙트 생성
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect " + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);

        // 동글 생성
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);
        instantDongleObj.name = "Dongle " + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        instantDongle.manager = this;
        instantDongle.effect = instantEffect;
        donglePool.Add(instantDongle);

        return instantDongle;
    }

    Dongle GetDongle()
    {
        for(int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            
            if(!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }

        return MakeDongle();
    }

    void NextDongle()
    {
        if(isOver)
        {
            return;
        }

        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext");
    }

    IEnumerator WaitNext()
    {
        while(lastDongle != null) {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }

    public void TouchDown()
    {
        if(lastDongle == null) return;
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if(lastDongle == null) return;
        lastDongle.Drop();
        lastDongle = null;
    }

// BUG_A : 1) 왜 게임 오버가 되어도 필드에 남아있는 동글이들이 지워지지 않을까?
    public void GameOver()
    {
        if(isOver) return;
        isOver = true;
        
        StartCoroutine(GameOverRoutine());  // BUG_A : 2) 동글이를 지우는 코루틴 함수 호출
    }

    IEnumerator GameOverRoutine()
    {
        // 1. 장면 안에 활성화 되어 있는 모든 동글 가져오기
        Dongle[] dongles = FindObjectsOfType<Dongle>();

        // 2. 지우기 전에 모든 동글의 물리효과 비활성화
        for(int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;
        }

        // 3. 1번의 목록을 하나씩 접근해서 지우기
        for(int index = 0; index < dongles.Length; index++) //// BUG_A : 3) 동글이를 지우고
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        SfxPlay(Sfx.Over);
    }

    public void SfxPlay(Sfx type)
    {
        switch(type)
        {
        case Sfx.LevelUp:
            sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0,3)];
            break;
        case Sfx.Next:
            sfxPlayer[sfxCursor].clip = sfxClip[3];
            break;
        case Sfx.Attach:
            sfxPlayer[sfxCursor].clip = sfxClip[4];
            break;
        case Sfx.Button:
            sfxPlayer[sfxCursor].clip = sfxClip[5];
            break;
        case Sfx.Over:
            sfxPlayer[sfxCursor].clip = sfxClip[6];
            break;
        }

        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;
    }

    void LateUpdate()
    {
        scoreTxt.text = score.ToString();
    }
}
