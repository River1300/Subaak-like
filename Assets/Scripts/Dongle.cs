using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public Rigidbody2D rigid;

    public GameManager manager;
    public ParticleSystem effect;
    public int level;
    public bool isDrag;
    public bool isMerge;
    public bool isAttach;

    float deadTime;

    CircleCollider2D circle;
    Animator anim;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        anim.SetInteger("Level", level);
    }

    void OnDisable()
    {
        // 동글 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        // 동글 트랜스폼 초기화
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.zero;

        // 동글 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        circle.enabled = true;
    }

    void Update()
    {
        if(isDrag) {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // x축 경계 설정
            float leftBorder = -5.0f + transform.localScale.x / 2;
            float rightBorder = 5.0f - transform.localScale.x / 2;

            if(mousePos.x < leftBorder) {
                mousePos.x = leftBorder;
            }
            else if(mousePos.x > rightBorder) {
                mousePos.x = rightBorder;
            }

            mousePos.y = 8;
            mousePos.z = 0;
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.1f);
        }
    }

    public void Drag()
    {
        isDrag = true;
    }

    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if(isAttach)
        {
            return;
        }

        isAttach = true;
        manager.SfxPlay(GameManager.Sfx.Attach);
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if(other.gameObject.tag == "Dongle")
        {
            Dongle otherDongle = other.gameObject.GetComponent<Dongle>();
            if(level == otherDongle.level && !isMerge && !otherDongle.isMerge && level < 7)
            {
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = otherDongle.transform.position.x;
                float otherY = otherDongle.transform.position.y;

                // 1. 내가 아래에 있을 때
                // 2. 동일한 높이일 때, 내가 오른쪽에 있을 때
                if(meY < otherY || (meY == otherY && meX > otherX))
                {
                    otherDongle.Hide(transform.position);
                    LevelUp();
                }
            }
        }
    }

    public void Hide(Vector3 targetPos)
    {
        isMerge = true;

        rigid.simulated = false;
        circle.enabled = false;

        if(targetPos == Vector3.up * 100)
        {
            EffectPlay();
            manager.SfxPlay(GameManager.Sfx.LevelUp);
        }

        StartCoroutine(HideRoutine(targetPos)); // BUG_A : 5) 동글이를 지우는 코루틴 함수 호출
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int frameCount = 0;

        while(frameCount < 20)
        {
            frameCount++;

            if(targetPos != Vector3.up * 100)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.3f);
            }
            else if(targetPos == Vector3.up * 100)  // BUG_A : 6) 게임 오버 위치를 전달 받았다면 지운다.
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            
            yield return null;
        }

        manager.score += (int)Mathf.Pow(2, level);

        isMerge = false;
        gameObject.SetActive(false);
    }

    void LevelUp()
    {
        isMerge = true;

        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        StartCoroutine(LevelUpRoutine());
    }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        anim.SetInteger("Level", level + 1);
        EffectPlay();
        manager.SfxPlay(GameManager.Sfx.LevelUp);

        yield return new WaitForSeconds(0.3f);

        level++;

        manager.maxLevel = Mathf.Max(level, manager.maxLevel);

        isMerge = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if(other.tag == "Finish")
        {
            deadTime += Time.deltaTime;

            if(deadTime > 2)
            {
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);
            }
            // BUG_A : Result) 제어문을 else if 로 만들어 놓아서 밑의 제어문이 무시되고 있었다.
            if(deadTime > 5)   // BUG_A : 4) 게임 오버 함수를 호출하고
            {
                manager.GameOver();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if(other.tag == "Finish")
        {
            deadTime = 0f;
            spriteRenderer.color = Color.white;
        }
    }

    void EffectPlay()
    {
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        effect.Play();
    }
}
