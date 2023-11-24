using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

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
    int sfxCursor;

    [Header("----- [ UI ] -----")]
    public GameObject startGroup;
    public GameObject endGroup;
    public GameObject BestPanel;
    public Image NextDongleImage;
    public Sprite[] dongleSprite;
    int spriteNum = 0;
    public Text scoreTxt;
    public Text maxScoreTxt;
    public Text subScoreTxt;
    public Text GoldTxt;
    public Text SilverTxt;
    public Text BronzeTxt;

    [Header("----- [ ETC ] -----")]
    public GameObject WallPart;
    public GameObject ScorePart;
    public GameObject RecordPart;
    public GameObject LevelPart;
    public GameObject NextPart;
    public GameObject InfoPart;
    
    void Awake()
    {
        Application.targetFrameRate = 60;

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        for(int index = 0; index < poolSize; index++)
        {
            MakeDongle();
        }

        // BUG_C : Result) 그러니깐 앱을 재 실행할 때 자꾸 점수가 초기화되어서 앱이 실행될 때 Json을 불러왔다.
        DataManager.instance.LoadData();

        // if(!PlayerPrefs.HasKey("MaxScore"))
        // {
        //     PlayerPrefs.SetInt("MaxScore", 0);
        // }

        // maxScoreTxt.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }

    public void GameStart()
    {
        // 오브젝트 활성화
        WallPart.SetActive(true);
        ScorePart.SetActive(true);
        RecordPart.SetActive(true);
        LevelPart.SetActive(true);
        NextPart.SetActive(true);
        InfoPart.SetActive(true);
        startGroup.SetActive(false);

        // #2. Load Json
        // BUG_C : 4) 그래서 게임 매니저에서 점수를 배정하였다.
        maxScoreTxt.text = DataManager.instance.Player.BestScore.ToString();
        GoldTxt.text = DataManager.instance.Player.GoldScore.ToString();
        SilverTxt.text = DataManager.instance.Player.SilverScore.ToString();
        BronzeTxt.text = DataManager.instance.Player.BronzeScore.ToString();
        DataManager.instance.LoadData();

        bgmPlayer.Play();
        SfxPlay(Sfx.Button);

        Invoke("NextDongle", 1.5f);
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
        lastDongle.level = spriteNum;
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine("WaitNext");
    }

    IEnumerator WaitNext()
    {
        Next();

        while(lastDongle != null) {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        NextDongle();
    }

    void Next()
    {
        spriteNum = Random.Range(0, maxLevel);
        NextDongleImage.sprite = dongleSprite[spriteNum];
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

        if(DataManager.instance.Player.BestScore < score)
        {
            BestPanel.SetActive(true);
        }

        // #1. Save Json
        //      => 게임 오버가 된 후에 최종 점수를 curScore에 저장한다.
        DataManager.instance.Player.curScore = score;
        DataManager.instance.SaveData();

        // 최고 점수 갱신
        // int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        // PlayerPrefs.SetInt("MaxScore", maxScore);

        // 게임 오버 UI 표시
        subScoreTxt.text = "점수 : " + scoreTxt.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.Over);
    }

    public void Reset()
    {
        SfxPlay(Sfx.Button);

        StartCoroutine(ResetCoroutine());
    }

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(0);
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
