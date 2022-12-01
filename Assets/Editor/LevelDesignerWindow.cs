using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelDesignerWindow : EditorWindow
{
    /*
        Tasarýmda kullanacaðýmýz prefablarýn bulunduðu klasörleri
        tutacaðýmýz deðiþkenler
        */
    static List<List<string>> klasorPrefablari = new List<List<string>>();
    static string[] altKlasorler;

    /*
    Oluþturacaðýmýz arayüzde hangi prefab klasörünün
    seçili halde olduðu bilgisini tuttuðumuz deðiþken.
    */
    static int seciliKlasor = 0;

    static bool sahnePaneliniAc = false;
    static Vector2 sahnePaneliPos = Vector2.zero;

    /*
    ScrollView yani aþaðý yukarý kaydýrabildiðimiz panellerimizde
    scroll deðerini tutmak için kullandýðýmýz deðiþkenler.
    */
    static Vector2 prefabPanelScroll = Vector2.zero;
    static Vector2 klasorPanelScroll = Vector2.zero;

    //Sahnedeki kamera verisi için oluþturduðumuz deðiþken
    static Camera sahneCamera;

    static bool altObjeOlarakEkle = false;
    static bool seciliHaldeOlustur = false;
    static bool mousePanelAc = false;

    /*
    Prefab Klasörü adresini tutacaðýmýz deðiþken
    Boþ bir deðer vermemek adýna, ilk baþta veriyi elle giriyorum.
    */
    static string prefabKlasoru = "Assets/Prefabs";

    /*
    Oluþturduðumuz araç panelini aktif hale getirmek için
    MenuItem özelliðini kullanarak menüye yeni bir buton ekliyoruz
    */
    [MenuItem("EditorEducation/Level Aracý")]
    static void ShowWindow()
    {
        //LevelTasarlamaAraci Tipindeki paneli, pencereyi oluþturup aktif hale getiriyoruz.
        var window = GetWindow<LevelDesignerWindow>();
        window.titleContent = new GUIContent("LevelTasarlamaAraci");
        window.Show();
    }

    //Level aracýmýzýn ayarlarýnýn olduðu panel arayüzü
    void OnGUI()
    {
        //Label ile baþlýklar ekliyoruz
        GUILayout.Label("Level Oluþturma Aracý");
        GUILayout.Label("Seçenekler");

        /*
        Farklý seçeneklerimizi açýp kapatabileceðimiz arayüz elemanlarýný ekliyoruz.
        Toggle elemaný true veya false deðer elde ederiz.
        Alacaðý ilk deðiþken true-false deðerini tutan deðiþken, ikincisi ise metin kutusunda yazacak yazýdýr.
        bunu = ifadesi kullanarak atadýðýmýzda ise, Toggle elemanýndan gelecek true-false deðeri de bu deðiþkene atayabiliyoruz.
        */
        altObjeOlarakEkle = GUILayout.Toggle(altObjeOlarakEkle, "Alt Obje Olarak Ekle");
        seciliHaldeOlustur = GUILayout.Toggle(seciliHaldeOlustur, "Seçili Halde Oluþtur");
        mousePanelAc = GUILayout.Toggle(mousePanelAc, "Mouse Konumunda Panel Aç");

        GUILayout.Label("Seçili Klasör: " + prefabKlasoru);
        /*
        Bu kýsým biraz karmaþýk gelebilir. 
        Normalde Button elemanýný if bloguna yazmadan da kullanabiliyoruz.
        Ancak if blogunun içerisine eklediðimizde, bu buton týklanýrsa 
        ne yapýlmasýný istediðimizi belirtebiliyoruz.
        */
        if (GUILayout.Button("Prefab Klasörü Seç"))
        {
            PrefabSecmeEkrani();
        }

        //Eðer prefabKlasörü seçili deðilse, boþta ise, bir dosya yolu belirtmiyorsa uyarý oluþturuyoruz.
        if (prefabKlasoru == "")
        {
            /*
            HelpBox sayesinde bir hatýrlatma, yardým görünümü oluþturuyoruz ve kullanýcýyý  bilgilendirebiliyoruz.
            Alacaðý ilk deðiþken mesajýmýz iken, ikinci deðiþken mesajýmýzýn önem durumunu belirten MessageType. 
            "Warning"  yerine "Error" ve "Info" da kullanabilirsiniz
            */
            EditorGUILayout.HelpBox("Prefab klasörü assets altýnda olmalýdýr", MessageType.Warning);
        }

        GUILayout.Label("Seçilen Alt Klasörler");
        //Seçili olan prefab klasörlerinin altýnda olan klasörleri sýrayla dolanýp ismini ve kaç adet prefaba sahip olduðunu panele yazdýrýyoruz.
        for (int i = 0; i < altKlasorler.Length; i++)
        {
            GUILayout.Label(altKlasorler[i] + " klasörü " + klasorPrefablari[i].Count + " adet prefab Bulunduruyor");
        }
    }
    //Panel penceremiz aktif hale gelince PaneliYukle ile gerekli verileri çekiyoruz.
    void OnEnable()
    {
        PaneliYukle();
    }

    void OnDisable()
    {
        /*
        Panelimiz kapandýðýnda Scene paneli aktifken çaðýrýlan 
        listener olayýndan fonksiyonumuzu siliyoruz.
        */
        SceneView.duringSceneGui -= OnSceneView;
    }
    void OnInspectorUpdate()
    {
        PaneliYukle();
    }

    void PaneliYukle()
    {
        /*
        Burada listener olayýndan kaydýmýzý önce silip sonra tekrar aktif hale getiriyoruz.
        Bunu yapma amacýmýz bir noktada bizden baðýmsýz þekilde listener kaydý silinememiþse,
        tekrar atama yapmadan önce kaydý sildiðimizden emin olmak.
        */
        SceneView.duringSceneGui -= OnSceneView;
        SceneView.duringSceneGui += OnSceneView;

        /*
        Sahnede varolan kameralarý bir diziye alýyoruz
        */
        Camera[] kameralar = SceneView.GetAllSceneCameras();

        //Eðer hiçbir kamera bulunamazsa bir hata bildirimi oluþturuyoruz ve hiçbir þey yapmadan geri döndürüyoruz.
        if (kameralar == null || kameralar.Length == 0)
        {
            Debug.LogWarning("Kamera boþ");
            return;
        }
        else
        {
            //Eðer kamera bulunduysa ilk kamerayý alýyoruz. Bu kamera Scene Panelinde görüntü saðlayan kameradýr.
            sahneCamera = kameralar[0];
        }

        //Kameraya eriþebildiysk prefablarý göstereceðimiz fonksiyonu çaðýrýyoruz.
        PrefablariYukle();
    }

    //Level aracýmýzýn Scene paneli içerisinde oluþturacaðýmýz arayüz
    void OnSceneView(SceneView scene)
    {
        //eðer sahne kamerasýný bulamadýysa hiçbir þey yapmadan geri dönüyoruz.
        if (sahneCamera == null)
            return;
        /*
        Handles.BeginGUI ve Handles.EndGUI fonksiyonlarý Scene panelinde 
        kendi yazacaðýmýz arayüzü göstermek için kullanmamýz gereken zorunlu kodlar.
        Bu iki komut arasýna yazacaðýmýz arayüz komutlarý Scene panelinde gözükecektir.
        */
        Handles.BeginGUI();
        /*
        Scene panelinin sol alt köþesine yazý yazdýrmak için bu Label komutunu kullanýyoruz.
        Aldýðý ilk deðiþken Rect tipinde olacak. UI ile uðraþtýysanýz Rect Transform isimli bileþene aþinasýnýzdýr.
        Rect tipi 4 adet deðiþkene ihtiyaç duyuyor, X konumu - Y konumu - Geniþlik - Yükseklik.
        Label için 2.deðiþken ise yazdýrmak istediðimiz yazý
        3. deðiþken de stil ayarý. Burada toolbarButton kullanmayý tercih ettim.
        Siz de farklý stiller deneyebilirsiniz.
        */
        GUI.Label(new Rect(sahneCamera.scaledPixelWidth - 150, sahneCamera.scaledPixelHeight - 20, 150, 20), "Sahne Aracý", EditorStyles.toolbarButton);
        //Eðer sahnePaneliniAc deðiþkenimiz true deðere sahipse prefablarý gösterdiðimiz paneli açýyoruz.
        if (sahnePaneliniAc)
        {
            PrefabPaneli();
        }
        Handles.EndGUI();

        /*
        Event tipi burada  etkileþimlerini, olaylarýný kontrol için kullandýðýmýz bir tip.
        Bu sayede týklama, tuþa basma durumlarýný kontrol edebiliyoruz.
        Event.current ile o an bir iþlem yapýldýysa bunun bilgisini elde ediyoruz.
        */
        Event e = Event.current;

        /*
        Burada switch kullanma sebebim tamamen örnek amaçlý. Ýf bloguyla da deneyebilirsiniz.
        */
        switch (e.type)
        {
            //Eðer herhangi bir tuþ basýlmayý býrakýldýysa
            case EventType.KeyUp:
                //eðer basýlmayý býrakýlan tuþ Tab tuþu ise
                if (e.keyCode == KeyCode.Tab)
                {
                    /*
                    Sahne panelini varolanýn tersi hale getiriyoruz.
                    Yani true ise false, false ise true hale geliyor.
                    */
                    sahnePaneliniAc = !sahnePaneliniAc;

                    //Eðer mouse konumunda panelin açýlmasýný istiyorsak, true deðerde ise
                    if (mousePanelAc)
                    {
                        //Sahne kamerasýný baz alarak, farenin konumunu elde ediyoruz.
                        Vector2 geciciPos = sahneCamera.ScreenToViewportPoint(Event.current.mousePosition);
                        /*
                         x ve y konumlarýný kontrol ediyoruz.
                         Scene paneline göre fare konumun aldýðýmýz için panelin içerisinde mi,
                         yoksa panelin sýnýrlarý dýþýnda mý bunu kontrol ediyoruz.
                         Panelin sol üstü (0,0) iken sað altý (1,1) deðerlerine sahiptir.
                         */
                        if (geciciPos.x > 0 && geciciPos.x < 1 && geciciPos.y > 0 && geciciPos.y < 1)
                        {
                            sahnePaneliPos = sahneCamera.ViewportToScreenPoint(geciciPos);
                        }
                        else
                        {
                            sahnePaneliPos = Vector2.zero;
                        }
                    }
                }
                break;
        }
    }

    void PrefabPaneli()
    {
        //Yaratacaðýmýz arayüz için stillendirme oluþturuyoruz
        GUIStyle areaStyle = new GUIStyle(GUI.skin.box);
        areaStyle.alignment = TextAnchor.UpperCenter;

        //Oluþturacaðýmýz panelin ölçülerini ve konum bilgisini tutmak için bir Rect deðiþkeni oluþturduk.
        Rect panelRect;
        //Eðer mouse konumunda açmak istiyorsak, daha öncesinde oluþturduðumuz sahnePaneliPos isimli deðiþkeni kullanýyoruz.
        if (mousePanelAc)
        {
            panelRect = new Rect(sahnePaneliPos.x, sahnePaneliPos.y, 200, 300);
        }
        else
        {
            /*
            Mouse konumunda açýlmasýný istemediðimiz zamanlarda sol tarafta olmasýný istiyorum.
            Bu yüzden Rect deðiþkeninin ilk 2 deðiþkeni sýfýr, yani sol üstten baþlatýyoruz.
            240 birim geniþlik, sabit olmasýný istediðim için.
            Son deðiþken ise Scene panelinin yüksekliðini elde etmemizi saðlayan bir kod. Yani
            Scene panelinin yüksekliði ile bizim oluþturduðumuz panelin yüksekliði ayný ölçüde olacak.
            */
            panelRect = new Rect(0, 0, 240, SceneView.currentDrawingSceneView.camera.scaledPixelHeight);
        }

        /*
        BeginArea ve EndArea bir arayüz bölgesi oluþturmak için kullandýðýmýz komut.
        Bu ikisi arasýnda yazdýðýmýz arayüz bileþenleri BeginArea içinde verdiðimiz 
        ilk deðiþken olan Rect tipi deðiþkene göre belirli bir alan içerisinde kalacak.
        2. deðiþken olarak ise stil deðiþkeni veriyoruz.
        */
        GUILayout.BeginArea(panelRect, areaStyle);

        /*
        Klasörleri seçilebilir halde tutmak istiyorum ve bunlarý bir scrollview içerisinde tutarak 
        kaydýrýlabilir bir panel içerisinde tutacaðým.
        BeginScrollView ile aþaðý-yukarý, saða-sola kaydýrýlabilir bölümler oluþturabilirim.
        Sýrasýyla deðiþkenleri yazarsak
        1.klasorPanelScroll:Kaydýrmanýn hangi durumda olduðunu gösteriyor, sað mý sol mu aþaðý mý yukarý mý, bunun verisini vector2 olarak tutuyoruz.
        Dikkat ederseniz = ile yine klasorPanelScroll deðiþkenine atama yapýyoruz. Scrollview üzerinde kaydýrma yaptýðýmýzda, bu þekilde verimizi güncelleyebiliyoruz, sabit kalmýyor.
        2. deðiþken true deðere sahip, bu deðiþkenin karþýlýðý horizontalScroll aslýnda, yani saða sola kaydýrma. Ben de bu þekilde olmasýný istediðim için true veriyorum.
        3. deðiþken de bu sefer dikeyde kaydýrma durumunu soruyor, false veriyorum. Çünkü yatayda kaydýrma olmasýný istemiyorum.
        4. deðiþkende horizontal yani yatay kaydýrma çubuðum için istediðim stili veriyorum
        5. deðiþken de vertical yani dikey kaydýrma çubuðu için stil deðiþkeni, herhangi bir stil atamasýný istemiyorum.
        6. deðiþken de ölçülerde sýnýrlandýrma yapmamýzý saðlayan deðiþken. MinHeight 40 vererek, en az 40 birim yükseklikte olmasýný saðlýyorum.
        */
        klasorPanelScroll = GUILayout.BeginScrollView(klasorPanelScroll, true, false, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.MinHeight(40));

        /*
        ScrollView içerisine bir adet toolbar koyuyorum. Bu aslýnda bir sürü butonu içinde barýndýran ancak sadece 1 tanesinin aktif-seçili olabildiði bir arayüz yapýsý.
        Bunu da hangi alt klasörün seçildiðini tutmak için kullanýyorum. 
        Aldýðý ilk deðiþken int tipinde ID deðiþkeni
        2.deðiþken ise içerisine dolduracaðýmýz butonlara yazýlacak isimlerin olduðu altKlasörler dizisi.
        Yine dikkat ederseniz, oluþturduðumuz arayüzden veriyi alabilmek için = ile seciliKlasor deðiþkenine atama yapýyorum.
        */
        seciliKlasor = GUILayout.Toolbar(seciliKlasor, altKlasorler);

        //EndScrollView komutunu çaðýrmayý unutmayýn.
        GUILayout.EndScrollView();

        /*
        Bu sefer de prefablarý göstereceðim bir scrollview oluþturacaðým. 
        Bir önceki ScrollView dan farklý olarak bu sefer dikeyde yani vertical halde hareket istiyorum.
        Yine MinHeight kullandým ancak dikkat ederseniz, Area için verdiðim panelRect yüksekliðinden 40 çýkarýyorum.
        Bu sayede bu iki ScrollView birbirine tam olarak oturup ekraný kaplayacaklar.
        Ayrýca bu sefer prefabPanelScroll deðiþkenini kullandýðým dikkatinizden kaçmasýn. Her bir scroll için ayrý bir deðiþkende bu veriyi tutmamýz gerekiyor.
        */
        prefabPanelScroll = GUILayout.BeginScrollView(prefabPanelScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.MinHeight(panelRect.height - 40));

        /*
        Toolbar sayesinde elde ettiðim seçiliKlasor ile sadece istediðimiz klasördeki prefablarý
        sýrayla dolanýp ekranda bunlar için birer arayüz elemaný oluþturuyorum.
        */
        for (int i = 0; i < klasorPrefablari[seciliKlasor].Count; i++)
        {
            /*
            Bu kýsýmda yaptýðým þey prefab ismini elde edebilmek için
            bir filtreleme iþlemi. Dosya yolu halinde tuttuðum için
            elimize geçen veri "klasorismi/isim.prefab" formatýnda.
            Burada da sadece isim kýsmýna eriþmek için filtreleme yapýyorum.
            Substring ile en sondaki slash "/" sonrasý ve .prefab öncesi kýsýmlarý alarak
            isim deðerini elde ediyorum.
            */
            int index = klasorPrefablari[seciliKlasor][i].LastIndexOf("/");
            string isim = "";
            if (index >= 0)
            {
                isim = klasorPrefablari[seciliKlasor][i].Substring(index + 1, klasorPrefablari[seciliKlasor][i].Length - index - 8);
            }
            else
            {
                isim = klasorPrefablari[seciliKlasor][i];
            }

            /*
            Özelleþtirilebilir halde bir buton oluþturmak için
            GUIContent tipinde bir deðiþken oluþturuyorum.
            text deðiþkenine ismi, image deðiþkenine ise prefabGorseliAl 
            fonksiyonu ile elde ettiðim resim verisini atýyorum.
             */
            GUIContent icerik = new GUIContent();
            icerik.text = isim;
            icerik.image = prefabGorseliAl(klasorPrefablari[seciliKlasor][i]);
            /*
            Her bir prefab için ayrý ayrý butonlarý oluþturuyorum. Dikkat ederseniz Button'da deðiþken olarak icerik deðiþkenini kullandým.
            Ve bu butonlarýn týklanma durumuna da ObjeOlustur isimli fonksiyonu veriyorum.
            */
            if (GUILayout.Button(icerik))
            {
                ObjeOlustur(klasorPrefablari[seciliKlasor][i]);
            }
        }
        //Prefablar için olan scrollview elemanýný sonlandýrýyorum.
        GUILayout.EndScrollView();
        //Arayüz panelimi sonlandýrýyorum.
        GUILayout.EndArea();
    }

    //Verilen dosya yolundaki prefabý sahnede oluþturmamýzý saðlar.
    void ObjeOlustur(string prefabYolu)
    {
        //Verilen prefabYolu ile dosyayý bulup bir obje deðiþkenine aktarýyorum.
        Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabYolu);
        /*
        Objeyi InstantiatePrefab komutuyla sahnede yaratýyorum ve bir deðiþkene atýyorum.
        Oyun içerisinde kullandýðýmýz Instantiate komutundan farklý olarak obje bilgisi dýþýnda
        hangi sahnede yaratacaðýmýzý da eklememiz gerekiyor.
        Aktif sahneyi de EditorSceneManager.GetActiveScene() komutu ile alýyoruz.
        */
        GameObject yeniObje = PrefabUtility.InstantiatePrefab(obj as GameObject, EditorSceneManager.GetActiveScene()) as GameObject;
        /*
        Eðer seçili bir obje varsa ve ayarlarýmýda "Alt Obje olarak" oluþturma aktifse yeni objeyi 
        seçili olanýn alt objesi - child haline getiriyoruz.
        */
        if (Selection.activeGameObject != null && altObjeOlarakEkle)
        {
            yeniObje.transform.parent = Selection.activeGameObject.transform;
        }
        /*
        Eðer yeni yaratýlan objenin otomatik seçili hale gelmesi özelliðimiz aktif ise
        Selection.activeGameObject deðiþkenine atama yapýyoruz, bak bu obje aktif olsun diyoruz.
        */
        if (seciliHaldeOlustur)
        {
            Selection.activeGameObject = yeniObje;
        }

        /*
        Geri alma iþlemini de yapabilmek amacýyla Undo yapýsýna yaptýðýmýz iþlemi kaydediyoruz.
        Burada RegisterCreatedObjectUndo komutu ile bir obje yarattýðýmýzý ve bunun geri alýnabilir
        hale gelmesini istediðimi belirtiyoruz.
        ilk deðiþken yarattýðýmýz yeni obje, ikincisi ise Edit butonu altýnda göstereceðimiz yazý.
        Ýster CTRL-Z ile istersek edit altýnda bu yazýya sahip butona kaldýrarak ekleme iþlemini geri alabiliriz.

        */
        Undo.RegisterCreatedObjectUndo(yeniObje, "Yeni eklenen objeyi kaldýr");

        //Obje yaratýmý sonrasýnda paneli kapatmak amacýyla bu komutu ekledim. Ýsterseniz aktif halde býrakabilirsiniz.
        sahnePaneliniAc = false;
    }

    //Seçtiðimiz klasör altýndaki klasörleri ve prefablarýn yollarýný gerekli dizilere aktarmamýzý saðlar.
    void PrefablariYukle()
    {
        //Eðer klasör seçili deðilse hiçbir iþlem yapmadan geri dönüyoruz.
        if (prefabKlasoru == "")
        {
            return;
        }
        //Varolan prefablarýn listesini temizliyoruz.
        klasorPrefablari.Clear();

        /*
        Seçili klasörün alt klasörlerini elde ediyoruz.
        Bu haliyle yalnýzca bir alttaki klasörlerin isimlerini alabiliyoruz.
        Ancak bütün prefablara eriþebiliyoruz.
        */
        string[] klasorYollari = AssetDatabase.GetSubFolders(prefabKlasoru);
        altKlasorler = new string[klasorYollari.Length];

        //Klasörlerin isimlerii altKlasorler dizimize kaydediyoruz
        for (int i = 0; i < klasorYollari.Length; i++)
        {
            int ayirmaIndeksi = klasorYollari[i].LastIndexOf('/');
            altKlasorler[i] = klasorYollari[i].Substring(ayirmaIndeksi + 1);
        }

        /*
        Bütün alt klasörleri dolanarak içerisindeki prefablara eriþiyoruz
        ve klasorPrefablari dizimize bunlarýn dosya yollarýný ekliyoruz.
        */
        foreach (string klasor in klasorYollari)
        {
            List<string> gecici = new List<string>();
            string[] altPrefablar = AssetDatabase.FindAssets("t:prefab", new string[] { klasor });
            foreach (string prefabGUID in altPrefablar)
            {
                string prefabYolu = AssetDatabase.GUIDToAssetPath(prefabGUID);
                gecici.Add(prefabYolu);
            }
            klasorPrefablari.Add(gecici);
        }
    }

    //Klasör seçimi ekranýný açmamýzý saðlar.
    void PrefabSecmeEkrani()
    {
        /*
        OpenFolderPanel komutu ile dosya seçimi için dosya gezginini açabiliyoruz.
        Burada verdiðimiz ilk deðiþken olan "Prefab Klasörü" dosya gezgininde yazacak baþlýk,
        ikinci deðiþken klasör, son deðiþken ise seçilebilecek dosya tipi.
        Bu ekranda seçim yapýldýðýnda bize string tipinde dosya yolu verisini iletecektir. 
        Bu veriyi geciciYol isimli deðiþkene atýyoruz.
        */
        string geciciYol = EditorUtility.OpenFolderPanel("Prefab Klasörü", "", "folder");

        /*
        Assets klasörü altýndaki dosyalara bakacaðýz. Bu yüzden IndexOf ve
        Substring ile filtreleme yaparak prefabKlasoru deðiþkenimizi guncelliyoruz.
        Eðer "/Assets/" içeren bir string verimiz yoksa, klasör farklý bir yerde seçildi demektir.
        Bu durumda string deðeri "" hale getiriyoruz.
        Tabii ki, Assets ismine sahip alakasýz bir yerdeki alt klasör de seçilebilir. Çok iyi bir 
        filtreleme yöntemi deðil. Nasýl bir yol ile daha iyi hale getirebilirsiniz diye düþünmek
        iyi bir egzersiz olabilir :)
        */
        int index = geciciYol.IndexOf("/Assets/");
        if (index >= 0)
        {
            prefabKlasoru = geciciYol.Substring(index + 1);
            PrefablariYukle();
        }
        else
        {
            prefabKlasoru = "";
        }
    }

    //Verilen dosya yolundaki prefabýn görseli elde etmemizi saðlar.
    Texture2D prefabGorseliAl(string prefabYolu)
    {
        Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabYolu);
        /*
        Burada asýl iþi yapan GetAssetPreview komutu. Project panelindeki görselleri de bu kod saðlýyor aslýnda.
        Bizim de iþimizi faslasýyla kolaylaþtýrýyor. Verilen objenin görselini önizleme bir görüntüsünü elde etmemizi saðlýyor.
        */
        return AssetPreview.GetAssetPreview(obj);
    }
}
