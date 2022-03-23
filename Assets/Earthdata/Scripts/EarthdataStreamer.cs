using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Earthdata
{
  [ExecuteInEditMode]
  public class EarthdataStreamer : MonoBehaviour
  {
    public static EarthdataStreamer INSTANCE;
    private NetworkCredential credentials;
    private string username;
    private string password;
    private void Awake()
    {
      if (INSTANCE != null && INSTANCE != this) DestroyImmediate(this);
      else INSTANCE = this;
      username = PlayerPrefs.GetString("earthdata_username");
      password = PlayerPrefs.GetString("earthdata_password");
    }

    public async Task<Stream> GetResource(string _resource)
    {
      CookieContainer cookieContainer = new CookieContainer();
      CredentialCache credentials = new CredentialCache();
      credentials.Add(new Uri("https://urs.earthdata.nasa.gov"), "Basic", new NetworkCredential(username,password));
      HttpWebRequest request = WebRequest.CreateHttp(_resource);
      request.Method = WebRequestMethods.Http.Get;
      request.Credentials = credentials;
      request.CookieContainer = cookieContainer;
      request.PreAuthenticate = false;
      request.AllowAutoRedirect = true;
      HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync();
      Stream stream = response.GetResponseStream();
      if (_resource.EndsWith(".zip"))
      {
        ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        stream = zipArchive.Entries[0].Open();
      }
      return stream;
    }
  }
}
