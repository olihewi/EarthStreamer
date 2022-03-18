using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
  public EventSystem eventSystem;
  public InputField earthdataUsername;
  public InputField earthdataPassword;
  public InputField mapboxToken;
  public InputField latitude;
  public InputField longitude;
  public Button playButton;
  public Image fader;
  private void Start()
  {
    earthdataUsername.text = PlayerPrefs.GetString("earthdata_username", "");
    earthdataPassword.text = PlayerPrefs.GetString("earthdata_password", "");
    mapboxToken.text = PlayerPrefs.GetString("mapbox_token", "");
    latitude.text = $"{PlayerPrefs.GetFloat("last_latitude", 51.5010681F)}";
    longitude.text = $"{PlayerPrefs.GetFloat("last_longitude", -2.54994893F)}";
    fader.enabled = false;
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Tab))
    {
      bool backwards = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
      Selectable current = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
      if (current == null) return;
      Selectable next = backwards ? current.FindSelectableOnLeft() : current.FindSelectableOnRight();
      if (next == null) next = backwards ? current.FindSelectableOnUp() : current.FindSelectableOnDown();
      if (next == null) return;
      next.OnSelect(new PointerEventData(eventSystem));
      eventSystem.SetSelectedGameObject(next.gameObject);
    }
  }

  private IEnumerator I_FadeOut(float _time)
  {
    fader.enabled = true;
    float t = 0.0F;
    Color col = fader.color;
    while (t < _time)
    {
      col.a = t / _time;
      fader.color = col;
      t += Time.deltaTime;
      yield return null;
    }
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
  }

  public void Play()
  {
    PlayerPrefs.SetString("earthdata_username", earthdataUsername.text);
    PlayerPrefs.SetString("earthdata_password", earthdataPassword.text);
    PlayerPrefs.SetString("mapbox_token", mapboxToken.text);
    PlayerPrefs.SetFloat("last_latitude", float.Parse(latitude.text));
    PlayerPrefs.SetFloat("last_longitude", float.Parse(longitude.text));
    StartCoroutine(I_FadeOut(1.0F));
  }

  public void OpenURL(string _url)
  {
    Application.OpenURL(_url);
  }
}
