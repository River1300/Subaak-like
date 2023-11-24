using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Data
{   // 모든 점수를 0으로 초기화 한다.
    public int GoldScore = 0;
    public int SilverScore = 0;
    public int BronzeScore = 0;
    public int curScore = 0;
    public int BestScore = 0;
}

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public GameManager manager;

    string jsonData;
    string path;
    public string filename = "Save";

    // 데이터( 점수 : 클래스 생성 )
    public Data Player = new Data();

    void Awake()
    {
        if(instance == null) instance = this;
        else if(instance != this) Destroy(instance.gameObject);
        DontDestroyOnLoad(this.gameObject);

        path = Application.persistentDataPath + "/";
    }

    public void SaveData()
    {   //  => 최고 점수와 비교( curScore > BestScore )하여 최고 점수를 갱신한다.
        if(Player.BestScore < Player.curScore) Player.BestScore = Player.curScore;

        // GoldScore < curScore ? Bronze = Silver, Silver = Gold, Gold = cur
        // : SilverScore < curScore ? Bronze = Silver, Silver = cur
        // : BronzeScore < curScore ? Bronze = cur : NoSwap
        if(Player.GoldScore < Player.curScore)
        {
            Player.BronzeScore = Player.SilverScore;
            Player.SilverScore = Player.GoldScore;
            Player.GoldScore = Player.curScore;
        }
        else if(Player.SilverScore < Player.curScore)
        {
            Player.BronzeScore = Player.SilverScore;
            Player.SilverScore = Player.curScore;
        }
        else if(Player.BronzeScore < Player.curScore)
        {
             Player.BronzeScore = Player.curScore;
        }

        SaveJson();
    }

    // Data 를 Json 으로 변환하여 저장
    public void SaveJson()
    {
        jsonData = JsonUtility.ToJson(Player);

        File.WriteAllText(path + filename, jsonData);
    }

    // 저장된 Json 을 Data 로 변환하여 화면에 출력
    public void LoadData()
    {
        LoadJson();
        // BUG_C : 3) 게임을 실행한다. -> 게임 오버( 이때 점수가 저장된다. ) -> 앱을 종료하지 않고 재시작 버튼으로 게임 실행
        //      => GameManager UI의 Text가 문제인지, 아니면 string 으로 변환한 Player의 점수가 문제인지 끝내 못알아 냈지만 Text 컴포넌트가 사라졌는데 접근을 한다는 에러가 계속해서 발생하며 점수를 읽어 오지 못하였다.
    }

    void LoadJson()
    {   // BUG_C : 1) 최초로 게임을 실행 했을 때 저장된 파일이 없어서 에러가 발생
        // BUG_C : 2) 저장된 파일이 없을 경우 초기화 값을 저장한 뒤 불러오기를 진행하였다.
        if(!File.Exists(filename)) SaveJson();

        string tempData = File.ReadAllText(path + filename);

        Player = JsonUtility.FromJson<Data>(tempData);
    }
}
