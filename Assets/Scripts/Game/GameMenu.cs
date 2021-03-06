using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Linq;
using System;
using GooglePlayGames;
using UnityEditor;
using UnityEngine.Rendering;

// Класс отвечающий за весь UI в самой игре
public class GameMenu : MonoBehaviour
{
    public GameObject pauseMenuCanvas, gameOverCanvas, capperField, PauseBtnCanvas, AdvertBtn, HomeBtn, LevelBtn;
    public Text gameOverText, scoreText;
    public GameObject[] goalsTexts; //goals text в паузе
    public GameObject audioBackground;

    public int totalMoney;
    public bool next = true;
    public void Start()
    {
        StartCoroutine(fadeInBackground());
    }

    public void Pause()
    {
        audioBackground.transform.GetComponent<AudioSource>().volume = 0;
        AudioManager.PlaySound(AudioManager.Audio.click);
        Time.timeScale = 0f;

       // capperField.SetActive(true);
        pauseMenuCanvas.SetActive(true);
        PauseBtnCanvas.SetActive(false);

        XElement data; // Данные XML файла
        TextAsset textAsset = (TextAsset)Resources.Load("XML/Lozalization/" + PlayerData.getInstance().lang.ToString() + "/level");
        data = XDocument.Parse(textAsset.text).Element("Level");

        string el = GameRule.TypeAI != GameRule.AI.None ? MenuManager.level.mode.ToString() + "AI" : MenuManager.level.mode.ToString();
        data = data.Element("targets").Element(el);

        goalsTexts[0].transform.GetComponent<Text>().text = data.Element("target1").Value.Replace("NUMBER", GameRule.target1.ToString());
        goalsTexts[1].transform.GetComponent<Text>().text = data.Element("target2").Value.Replace("NUMBER", GameRule.target2.ToString());
        goalsTexts[2].transform.GetComponent<Text>().text = data.Element("target3").Value.Replace("NUMBER", GameRule.target3.ToString());
    }

    public void UnPause()
    {
        Time.timeScale = 1f;
        audioBackground.transform.GetComponent<AudioSource>().volume = PlayerData.getInstance().volume;
       // capperField.SetActive(false);
        pauseMenuCanvas.SetActive(false);
        PauseBtnCanvas.SetActive(true);
        AudioManager.PlaySound(AudioManager.Audio.click);
    }

    public void ToMainMenuPressed()
    {
        Game.activeGame = false;
        Time.timeScale = 1f;
        AudioManager.PlaySound(AudioManager.Audio.click);
        LocalizationManager.clear();
        SceneManager.LoadScene("GameMenu");
    }

    public void gameOver(string message, int stars, int money1, int money2, int money3)
    {
        Game.activeGame = false;
        StartCoroutine(fadeOutBackground());

        PlayerData playerData = PlayerData.getInstance();

        createGameOverMenu();
        gameOverCanvas.SetActive(true);
        capperField.SetActive(true);
        StartCoroutine(spawnStars(stars, money1, money2, money3));

        // Запись значений в PlayerData
        totalMoney = money1 + money2 + money3;
        playerData.money += totalMoney;
        if (playerData.progress[GameRule.planetNum].first[GameRule.levelNum] < stars)
            playerData.progress[GameRule.planetNum].first[GameRule.levelNum] = (byte)stars;

        if ((GameRule.levelNum == (GameRule.levelCount - 1)) && message == "YOU WIN !" || message == "GAME OVER !")
        {
            playerData.currentPlanet++;
            playerData.progress[playerData.currentPlanet].second.second = true;

            switch (GameRule.type)
            {
                case GameRule.Type.lava: Social.ReportProgress(GPGSIds.achievement_youre_on_fire, 100f, null); break;
                case GameRule.Type.ice: Social.ReportProgress(GPGSIds.achievement_too_slippery, 100f, null); break;
                case GameRule.Type.jungle: Social.ReportProgress(GPGSIds.achievement_did_you_see_a_parrot, 100f, null); break;
                case GameRule.Type.sand: Social.ReportProgress(GPGSIds.achievement_mr_sandman, 100f, null); break;
                case GameRule.Type.water: Social.ReportProgress(GPGSIds.achievement_have_you_drown, 100f, null); break;
                default: Debug.LogError("Please, provide achievement for this planet"); break;
            }
        }

        XMLManager.SaveData(PlayerData.getInstance(), PlayerData.getInstance().ToString());

        XElement data; // Данные XML файла
        TextAsset textAsset = (TextAsset)Resources.Load("XML/Lozalization/" + PlayerData.getInstance().lang.ToString() + "/GameOverMenu");
        data = XDocument.Parse(textAsset.text).Element("GameOverMenu");

        if (message == "YOU WIN !")
        {
            gameOverText.text = data.Element("win").Value;
            Social.ReportProgress(GPGSIds.achievement_hallelujah, 100f, null);
        }   
        else if (message == "YOU LOSE !")
        {
            gameOverText.text = data.Element("lose").Value;
            PlayGamesPlatform.Instance.IncrementAchievement(GPGSIds.achievement_you_need_a_coach, 1, null);
            LevelBtn.transform.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/GameOverMenu/repeat_btn");
            next = false;
        } 
    }

    [System.Serializable]
    public class GameOverMenu
    {
        public List<Image> stars; // звезды
        public Image moneyTargetImage1, moneyTargetImage2, moneyTargetImage3; // Картинки критериев для получения монет
        public Text targetText1, targetText2, targetText3, totalText; // текст критериев получения монет
        public Text moneyTargetText1, moneyTargetText2, moneyTargetText3, moneyTotalText; // текст количество монет
        public Text gameOver; // Текст конца игры
    }
    public GameOverMenu gameOverMenu;

    void createGameOverMenu()
    {
        XElement data; // Данные XML файла
        TextAsset textAsset = (TextAsset)Resources.Load("XML/Lozalization/" + PlayerData.getInstance().lang.ToString() + "/GameOverMenu");
        data = XDocument.Parse(textAsset.text).Element("GameOverMenu");

        gameOverMenu.gameOver.text = data.Element("gameOver").Value;
        gameOverMenu.totalText.text = data.Element("totalText").Value;

        data = data.Element(GameRule.mode.ToString());

        XElement images = data.Element("images"); // названия спрайтов
        gameOverMenu.moneyTargetImage1.sprite = Resources.Load<Sprite>("Sprites/GameOverMenu/" + images.Element("targetImage1").Value);
        gameOverMenu.moneyTargetImage2.sprite = Resources.Load<Sprite>("Sprites/GameOverMenu/" + images.Element("targetImage2").Value);
        gameOverMenu.moneyTargetImage3.sprite = Resources.Load<Sprite>("Sprites/GameOverMenu/" + images.Element("targetImage3").Value);

        XElement texts = data.Element("texts");
        gameOverMenu.targetText1.text = texts.Element("targetText1").Value;
        gameOverMenu.targetText2.text = texts.Element("targetText2").Value;
        gameOverMenu.targetText3.text = texts.Element("targetText3").Value;
    }

    IEnumerator spawnStars(int stars, int money1, int money2, int money3)
    {
        for (int i = 0; i < stars; ++i)
        {
            gameOverMenu.stars[i].gameObject.SetActive(true);
            AudioManager.PlaySound(AudioManager.Audio.rise01);
            yield return new WaitForSeconds(0.5f);
        }
        StartCoroutine(spawnCount(money1, money2, money3));
    }
    IEnumerator spawnCount(int money1, int money2, int money3)
    {
        AudioManager.PlaySound(AudioManager.Audio.rise01);
        for (int i = 1; i <= money1; ++i)
        {
            gameOverMenu.moneyTargetText1.text = i.ToString();
            yield return new WaitForSeconds(0.015f);
        }

        AudioManager.PlaySound(AudioManager.Audio.rise02);
        for (int i = 1; i <= money2; ++i)
        {
            gameOverMenu.moneyTargetText2.text = i.ToString();
            yield return new WaitForSeconds(0.015f);
        }

        AudioManager.PlaySound(AudioManager.Audio.rise03);
        for (int i = 1; i <= money3; ++i)
        {
            gameOverMenu.moneyTargetText3.text = i.ToString();
            yield return new WaitForSeconds(0.015f);
        }

        AudioManager.PlaySound(AudioManager.Audio.rise04);
        int total = money1 + money2 + money3;
        for (int i = 1; i <= total; ++i)
        {
            gameOverMenu.moneyTotalText.text = i.ToString();
            yield return new WaitForSeconds(0.015f);
        }

        Color32 gray = new Color32(255, 255, 255, 255);
        AdvertBtn.GetComponent<Button>().enabled = true;
        HomeBtn.GetComponent<Button>().enabled = true;
        LevelBtn.GetComponent<Button>().enabled = true;
        AdvertBtn.GetComponent<Image>().color = gray;
        HomeBtn.GetComponent<Image>().color = gray;
        LevelBtn.GetComponent<Image>().color = gray;
    }

    IEnumerator fadeInBackground()
    {
        AudioSource audioSource = audioBackground.transform.GetComponent<AudioSource>();
        for (float f = 0; f <= PlayerData.getInstance().volume; f += 0.001f)
        {
            audioSource.volume = f;
            yield return new WaitForSeconds(0.006f);
        }
    }

    IEnumerator fadeOutBackground()
    {
        AudioSource audioSource = audioBackground.transform.GetComponent<AudioSource>();
        float fadedVolume = ((audioSource.volume * 10f) / 100f);
        for (float f = audioSource.volume; f >= fadedVolume; f -= 0.002f)
        {
            audioSource.volume = f;
            yield return new WaitForSeconds(0.006f);
        }
    }

    public void achievementBtn()
    {
        Social.ShowAchievementsUI();
    }

    public void advertBtn()
    {
        StartCoroutine(Ads.getInstance().StartAd(totalMoney, gameOverMenu.moneyTotalText));
    }

    void NextLvl()
    {
        if(GameRule.levelNum == (GameRule.levelCount - 1))
        {
            GameRule.planetNum++;
            GameRule.levelNum = 0;
        }
        else
        {
            GameRule.levelNum++;
        }
        Level.setGameRule(true, GameRule.planetNum, GameRule.levelNum);
        AudioManager.PlaySound(AudioManager.Audio.click);
    }

   void RepeatLvl()
    {
        Level.setGameRule(true, GameRule.planetNum, GameRule.levelNum);
        AudioManager.PlaySound(AudioManager.Audio.click);
    }

    public void LvlBtn()
    {
        if (next) NextLvl();
        else RepeatLvl();
    }
}

