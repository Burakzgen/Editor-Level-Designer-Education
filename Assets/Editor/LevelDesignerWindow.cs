using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelDesignerWindow : EditorWindow
{
    /*
        Tasar�mda kullanaca��m�z prefablar�n bulundu�u klas�rleri
        tutaca��m�z de�i�kenler
        */
    static List<List<string>> klasorPrefablari = new List<List<string>>();
    static string[] altKlasorler;

    /*
    Olu�turaca��m�z aray�zde hangi prefab klas�r�n�n
    se�ili halde oldu�u bilgisini tuttu�umuz de�i�ken.
    */
    static int seciliKlasor = 0;

    static bool sahnePaneliniAc = false;
    static Vector2 sahnePaneliPos = Vector2.zero;

    /*
    ScrollView yani a�a�� yukar� kayd�rabildi�imiz panellerimizde
    scroll de�erini tutmak i�in kulland���m�z de�i�kenler.
    */
    static Vector2 prefabPanelScroll = Vector2.zero;
    static Vector2 klasorPanelScroll = Vector2.zero;

    //Sahnedeki kamera verisi i�in olu�turdu�umuz de�i�ken
    static Camera sahneCamera;

    static bool altObjeOlarakEkle = false;
    static bool seciliHaldeOlustur = false;
    static bool mousePanelAc = false;

    /*
    Prefab Klas�r� adresini tutaca��m�z de�i�ken
    Bo� bir de�er vermemek ad�na, ilk ba�ta veriyi elle giriyorum.
    */
    static string prefabKlasoru = "Assets/Prefabs";

    /*
    Olu�turdu�umuz ara� panelini aktif hale getirmek i�in
    MenuItem �zelli�ini kullanarak men�ye yeni bir buton ekliyoruz
    */
    [MenuItem("EditorEducation/Level Arac�")]
    static void ShowWindow()
    {
        //LevelTasarlamaAraci Tipindeki paneli, pencereyi olu�turup aktif hale getiriyoruz.
        var window = GetWindow<LevelDesignerWindow>();
        window.titleContent = new GUIContent("LevelTasarlamaAraci");
        window.Show();
    }

    //Level arac�m�z�n ayarlar�n�n oldu�u panel aray�z�
    void OnGUI()
    {
        //Label ile ba�l�klar ekliyoruz
        GUILayout.Label("Level Olu�turma Arac�");
        GUILayout.Label("Se�enekler");

        /*
        Farkl� se�eneklerimizi a��p kapatabilece�imiz aray�z elemanlar�n� ekliyoruz.
        Toggle eleman� true veya false de�er elde ederiz.
        Alaca�� ilk de�i�ken true-false de�erini tutan de�i�ken, ikincisi ise metin kutusunda yazacak yaz�d�r.
        bunu = ifadesi kullanarak atad���m�zda ise, Toggle eleman�ndan gelecek true-false de�eri de bu de�i�kene atayabiliyoruz.
        */
        altObjeOlarakEkle = GUILayout.Toggle(altObjeOlarakEkle, "Alt Obje Olarak Ekle");
        seciliHaldeOlustur = GUILayout.Toggle(seciliHaldeOlustur, "Se�ili Halde Olu�tur");
        mousePanelAc = GUILayout.Toggle(mousePanelAc, "Mouse Konumunda Panel A�");

        GUILayout.Label("Se�ili Klas�r: " + prefabKlasoru);
        /*
        Bu k�s�m biraz karma��k gelebilir. 
        Normalde Button eleman�n� if bloguna yazmadan da kullanabiliyoruz.
        Ancak if blogunun i�erisine ekledi�imizde, bu buton t�klan�rsa 
        ne yap�lmas�n� istedi�imizi belirtebiliyoruz.
        */
        if (GUILayout.Button("Prefab Klas�r� Se�"))
        {
            PrefabSecmeEkrani();
        }

        //E�er prefabKlas�r� se�ili de�ilse, bo�ta ise, bir dosya yolu belirtmiyorsa uyar� olu�turuyoruz.
        if (prefabKlasoru == "")
        {
            /*
            HelpBox sayesinde bir hat�rlatma, yard�m g�r�n�m� olu�turuyoruz ve kullan�c�y�  bilgilendirebiliyoruz.
            Alaca�� ilk de�i�ken mesaj�m�z iken, ikinci de�i�ken mesaj�m�z�n �nem durumunu belirten MessageType. 
            "Warning"  yerine "Error" ve "Info" da kullanabilirsiniz
            */
            EditorGUILayout.HelpBox("Prefab klas�r� assets alt�nda olmal�d�r", MessageType.Warning);
        }

        GUILayout.Label("Se�ilen Alt Klas�rler");
        //Se�ili olan prefab klas�rlerinin alt�nda olan klas�rleri s�rayla dolan�p ismini ve ka� adet prefaba sahip oldu�unu panele yazd�r�yoruz.
        for (int i = 0; i < altKlasorler.Length; i++)
        {
            GUILayout.Label(altKlasorler[i] + " klas�r� " + klasorPrefablari[i].Count + " adet prefab Bulunduruyor");
        }
    }
    //Panel penceremiz aktif hale gelince PaneliYukle ile gerekli verileri �ekiyoruz.
    void OnEnable()
    {
        PaneliYukle();
    }

    void OnDisable()
    {
        /*
        Panelimiz kapand���nda Scene paneli aktifken �a��r�lan 
        listener olay�ndan fonksiyonumuzu siliyoruz.
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
        Burada listener olay�ndan kayd�m�z� �nce silip sonra tekrar aktif hale getiriyoruz.
        Bunu yapma amac�m�z bir noktada bizden ba��ms�z �ekilde listener kayd� silinememi�se,
        tekrar atama yapmadan �nce kayd� sildi�imizden emin olmak.
        */
        SceneView.duringSceneGui -= OnSceneView;
        SceneView.duringSceneGui += OnSceneView;

        /*
        Sahnede varolan kameralar� bir diziye al�yoruz
        */
        Camera[] kameralar = SceneView.GetAllSceneCameras();

        //E�er hi�bir kamera bulunamazsa bir hata bildirimi olu�turuyoruz ve hi�bir �ey yapmadan geri d�nd�r�yoruz.
        if (kameralar == null || kameralar.Length == 0)
        {
            Debug.LogWarning("Kamera bo�");
            return;
        }
        else
        {
            //E�er kamera bulunduysa ilk kameray� al�yoruz. Bu kamera Scene Panelinde g�r�nt� sa�layan kamerad�r.
            sahneCamera = kameralar[0];
        }

        //Kameraya eri�ebildiysk prefablar� g�sterece�imiz fonksiyonu �a��r�yoruz.
        PrefablariYukle();
    }

    //Level arac�m�z�n Scene paneli i�erisinde olu�turaca��m�z aray�z
    void OnSceneView(SceneView scene)
    {
        //e�er sahne kameras�n� bulamad�ysa hi�bir �ey yapmadan geri d�n�yoruz.
        if (sahneCamera == null)
            return;
        /*
        Handles.BeginGUI ve Handles.EndGUI fonksiyonlar� Scene panelinde 
        kendi yazaca��m�z aray�z� g�stermek i�in kullanmam�z gereken zorunlu kodlar.
        Bu iki komut aras�na yazaca��m�z aray�z komutlar� Scene panelinde g�z�kecektir.
        */
        Handles.BeginGUI();
        /*
        Scene panelinin sol alt k��esine yaz� yazd�rmak i�in bu Label komutunu kullan�yoruz.
        Ald��� ilk de�i�ken Rect tipinde olacak. UI ile u�ra�t�ysan�z Rect Transform isimli bile�ene a�inas�n�zd�r.
        Rect tipi 4 adet de�i�kene ihtiya� duyuyor, X konumu - Y konumu - Geni�lik - Y�kseklik.
        Label i�in 2.de�i�ken ise yazd�rmak istedi�imiz yaz�
        3. de�i�ken de stil ayar�. Burada toolbarButton kullanmay� tercih ettim.
        Siz de farkl� stiller deneyebilirsiniz.
        */
        GUI.Label(new Rect(sahneCamera.scaledPixelWidth - 150, sahneCamera.scaledPixelHeight - 20, 150, 20), "Sahne Arac�", EditorStyles.toolbarButton);
        //E�er sahnePaneliniAc de�i�kenimiz true de�ere sahipse prefablar� g�sterdi�imiz paneli a��yoruz.
        if (sahnePaneliniAc)
        {
            PrefabPaneli();
        }
        Handles.EndGUI();

        /*
        Event tipi burada  etkile�imlerini, olaylar�n� kontrol i�in kulland���m�z bir tip.
        Bu sayede t�klama, tu�a basma durumlar�n� kontrol edebiliyoruz.
        Event.current ile o an bir i�lem yap�ld�ysa bunun bilgisini elde ediyoruz.
        */
        Event e = Event.current;

        /*
        Burada switch kullanma sebebim tamamen �rnek ama�l�. �f bloguyla da deneyebilirsiniz.
        */
        switch (e.type)
        {
            //E�er herhangi bir tu� bas�lmay� b�rak�ld�ysa
            case EventType.KeyUp:
                //e�er bas�lmay� b�rak�lan tu� Tab tu�u ise
                if (e.keyCode == KeyCode.Tab)
                {
                    /*
                    Sahne panelini varolan�n tersi hale getiriyoruz.
                    Yani true ise false, false ise true hale geliyor.
                    */
                    sahnePaneliniAc = !sahnePaneliniAc;

                    //E�er mouse konumunda panelin a��lmas�n� istiyorsak, true de�erde ise
                    if (mousePanelAc)
                    {
                        //Sahne kameras�n� baz alarak, farenin konumunu elde ediyoruz.
                        Vector2 geciciPos = sahneCamera.ScreenToViewportPoint(Event.current.mousePosition);
                        /*
                         x ve y konumlar�n� kontrol ediyoruz.
                         Scene paneline g�re fare konumun ald���m�z i�in panelin i�erisinde mi,
                         yoksa panelin s�n�rlar� d���nda m� bunu kontrol ediyoruz.
                         Panelin sol �st� (0,0) iken sa� alt� (1,1) de�erlerine sahiptir.
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
        //Yarataca��m�z aray�z i�in stillendirme olu�turuyoruz
        GUIStyle areaStyle = new GUIStyle(GUI.skin.box);
        areaStyle.alignment = TextAnchor.UpperCenter;

        //Olu�turaca��m�z panelin �l��lerini ve konum bilgisini tutmak i�in bir Rect de�i�keni olu�turduk.
        Rect panelRect;
        //E�er mouse konumunda a�mak istiyorsak, daha �ncesinde olu�turdu�umuz sahnePaneliPos isimli de�i�keni kullan�yoruz.
        if (mousePanelAc)
        {
            panelRect = new Rect(sahnePaneliPos.x, sahnePaneliPos.y, 200, 300);
        }
        else
        {
            /*
            Mouse konumunda a��lmas�n� istemedi�imiz zamanlarda sol tarafta olmas�n� istiyorum.
            Bu y�zden Rect de�i�keninin ilk 2 de�i�keni s�f�r, yani sol �stten ba�lat�yoruz.
            240 birim geni�lik, sabit olmas�n� istedi�im i�in.
            Son de�i�ken ise Scene panelinin y�ksekli�ini elde etmemizi sa�layan bir kod. Yani
            Scene panelinin y�ksekli�i ile bizim olu�turdu�umuz panelin y�ksekli�i ayn� �l��de olacak.
            */
            panelRect = new Rect(0, 0, 240, SceneView.currentDrawingSceneView.camera.scaledPixelHeight);
        }

        /*
        BeginArea ve EndArea bir aray�z b�lgesi olu�turmak i�in kulland���m�z komut.
        Bu ikisi aras�nda yazd���m�z aray�z bile�enleri BeginArea i�inde verdi�imiz 
        ilk de�i�ken olan Rect tipi de�i�kene g�re belirli bir alan i�erisinde kalacak.
        2. de�i�ken olarak ise stil de�i�keni veriyoruz.
        */
        GUILayout.BeginArea(panelRect, areaStyle);

        /*
        Klas�rleri se�ilebilir halde tutmak istiyorum ve bunlar� bir scrollview i�erisinde tutarak 
        kayd�r�labilir bir panel i�erisinde tutaca��m.
        BeginScrollView ile a�a��-yukar�, sa�a-sola kayd�r�labilir b�l�mler olu�turabilirim.
        S�ras�yla de�i�kenleri yazarsak
        1.klasorPanelScroll:Kayd�rman�n hangi durumda oldu�unu g�steriyor, sa� m� sol mu a�a�� m� yukar� m�, bunun verisini vector2 olarak tutuyoruz.
        Dikkat ederseniz = ile yine klasorPanelScroll de�i�kenine atama yap�yoruz. Scrollview �zerinde kayd�rma yapt���m�zda, bu �ekilde verimizi g�ncelleyebiliyoruz, sabit kalm�yor.
        2. de�i�ken true de�ere sahip, bu de�i�kenin kar��l��� horizontalScroll asl�nda, yani sa�a sola kayd�rma. Ben de bu �ekilde olmas�n� istedi�im i�in true veriyorum.
        3. de�i�ken de bu sefer dikeyde kayd�rma durumunu soruyor, false veriyorum. ��nk� yatayda kayd�rma olmas�n� istemiyorum.
        4. de�i�kende horizontal yani yatay kayd�rma �ubu�um i�in istedi�im stili veriyorum
        5. de�i�ken de vertical yani dikey kayd�rma �ubu�u i�in stil de�i�keni, herhangi bir stil atamas�n� istemiyorum.
        6. de�i�ken de �l��lerde s�n�rland�rma yapmam�z� sa�layan de�i�ken. MinHeight 40 vererek, en az 40 birim y�kseklikte olmas�n� sa�l�yorum.
        */
        klasorPanelScroll = GUILayout.BeginScrollView(klasorPanelScroll, true, false, GUI.skin.horizontalScrollbar, GUIStyle.none, GUILayout.MinHeight(40));

        /*
        ScrollView i�erisine bir adet toolbar koyuyorum. Bu asl�nda bir s�r� butonu i�inde bar�nd�ran ancak sadece 1 tanesinin aktif-se�ili olabildi�i bir aray�z yap�s�.
        Bunu da hangi alt klas�r�n se�ildi�ini tutmak i�in kullan�yorum. 
        Ald��� ilk de�i�ken int tipinde ID de�i�keni
        2.de�i�ken ise i�erisine dolduraca��m�z butonlara yaz�lacak isimlerin oldu�u altKlas�rler dizisi.
        Yine dikkat ederseniz, olu�turdu�umuz aray�zden veriyi alabilmek i�in = ile seciliKlasor de�i�kenine atama yap�yorum.
        */
        seciliKlasor = GUILayout.Toolbar(seciliKlasor, altKlasorler);

        //EndScrollView komutunu �a��rmay� unutmay�n.
        GUILayout.EndScrollView();

        /*
        Bu sefer de prefablar� g�sterece�im bir scrollview olu�turaca��m. 
        Bir �nceki ScrollView dan farkl� olarak bu sefer dikeyde yani vertical halde hareket istiyorum.
        Yine MinHeight kulland�m ancak dikkat ederseniz, Area i�in verdi�im panelRect y�ksekli�inden 40 ��kar�yorum.
        Bu sayede bu iki ScrollView birbirine tam olarak oturup ekran� kaplayacaklar.
        Ayr�ca bu sefer prefabPanelScroll de�i�kenini kulland���m dikkatinizden ka�mas�n. Her bir scroll i�in ayr� bir de�i�kende bu veriyi tutmam�z gerekiyor.
        */
        prefabPanelScroll = GUILayout.BeginScrollView(prefabPanelScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.MinHeight(panelRect.height - 40));

        /*
        Toolbar sayesinde elde etti�im se�iliKlasor ile sadece istedi�imiz klas�rdeki prefablar�
        s�rayla dolan�p ekranda bunlar i�in birer aray�z eleman� olu�turuyorum.
        */
        for (int i = 0; i < klasorPrefablari[seciliKlasor].Count; i++)
        {
            /*
            Bu k�s�mda yapt���m �ey prefab ismini elde edebilmek i�in
            bir filtreleme i�lemi. Dosya yolu halinde tuttu�um i�in
            elimize ge�en veri "klasorismi/isim.prefab" format�nda.
            Burada da sadece isim k�sm�na eri�mek i�in filtreleme yap�yorum.
            Substring ile en sondaki slash "/" sonras� ve .prefab �ncesi k�s�mlar� alarak
            isim de�erini elde ediyorum.
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
            �zelle�tirilebilir halde bir buton olu�turmak i�in
            GUIContent tipinde bir de�i�ken olu�turuyorum.
            text de�i�kenine ismi, image de�i�kenine ise prefabGorseliAl 
            fonksiyonu ile elde etti�im resim verisini at�yorum.
             */
            GUIContent icerik = new GUIContent();
            icerik.text = isim;
            icerik.image = prefabGorseliAl(klasorPrefablari[seciliKlasor][i]);
            /*
            Her bir prefab i�in ayr� ayr� butonlar� olu�turuyorum. Dikkat ederseniz Button'da de�i�ken olarak icerik de�i�kenini kulland�m.
            Ve bu butonlar�n t�klanma durumuna da ObjeOlustur isimli fonksiyonu veriyorum.
            */
            if (GUILayout.Button(icerik))
            {
                ObjeOlustur(klasorPrefablari[seciliKlasor][i]);
            }
        }
        //Prefablar i�in olan scrollview eleman�n� sonland�r�yorum.
        GUILayout.EndScrollView();
        //Aray�z panelimi sonland�r�yorum.
        GUILayout.EndArea();
    }

    //Verilen dosya yolundaki prefab� sahnede olu�turmam�z� sa�lar.
    void ObjeOlustur(string prefabYolu)
    {
        //Verilen prefabYolu ile dosyay� bulup bir obje de�i�kenine aktar�yorum.
        Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabYolu);
        /*
        Objeyi InstantiatePrefab komutuyla sahnede yarat�yorum ve bir de�i�kene at�yorum.
        Oyun i�erisinde kulland���m�z Instantiate komutundan farkl� olarak obje bilgisi d���nda
        hangi sahnede yarataca��m�z� da eklememiz gerekiyor.
        Aktif sahneyi de EditorSceneManager.GetActiveScene() komutu ile al�yoruz.
        */
        GameObject yeniObje = PrefabUtility.InstantiatePrefab(obj as GameObject, EditorSceneManager.GetActiveScene()) as GameObject;
        /*
        E�er se�ili bir obje varsa ve ayarlar�m�da "Alt Obje olarak" olu�turma aktifse yeni objeyi 
        se�ili olan�n alt objesi - child haline getiriyoruz.
        */
        if (Selection.activeGameObject != null && altObjeOlarakEkle)
        {
            yeniObje.transform.parent = Selection.activeGameObject.transform;
        }
        /*
        E�er yeni yarat�lan objenin otomatik se�ili hale gelmesi �zelli�imiz aktif ise
        Selection.activeGameObject de�i�kenine atama yap�yoruz, bak bu obje aktif olsun diyoruz.
        */
        if (seciliHaldeOlustur)
        {
            Selection.activeGameObject = yeniObje;
        }

        /*
        Geri alma i�lemini de yapabilmek amac�yla Undo yap�s�na yapt���m�z i�lemi kaydediyoruz.
        Burada RegisterCreatedObjectUndo komutu ile bir obje yaratt���m�z� ve bunun geri al�nabilir
        hale gelmesini istedi�imi belirtiyoruz.
        ilk de�i�ken yaratt���m�z yeni obje, ikincisi ise Edit butonu alt�nda g�sterece�imiz yaz�.
        �ster CTRL-Z ile istersek edit alt�nda bu yaz�ya sahip butona kald�rarak ekleme i�lemini geri alabiliriz.

        */
        Undo.RegisterCreatedObjectUndo(yeniObje, "Yeni eklenen objeyi kald�r");

        //Obje yarat�m� sonras�nda paneli kapatmak amac�yla bu komutu ekledim. �sterseniz aktif halde b�rakabilirsiniz.
        sahnePaneliniAc = false;
    }

    //Se�ti�imiz klas�r alt�ndaki klas�rleri ve prefablar�n yollar�n� gerekli dizilere aktarmam�z� sa�lar.
    void PrefablariYukle()
    {
        //E�er klas�r se�ili de�ilse hi�bir i�lem yapmadan geri d�n�yoruz.
        if (prefabKlasoru == "")
        {
            return;
        }
        //Varolan prefablar�n listesini temizliyoruz.
        klasorPrefablari.Clear();

        /*
        Se�ili klas�r�n alt klas�rlerini elde ediyoruz.
        Bu haliyle yaln�zca bir alttaki klas�rlerin isimlerini alabiliyoruz.
        Ancak b�t�n prefablara eri�ebiliyoruz.
        */
        string[] klasorYollari = AssetDatabase.GetSubFolders(prefabKlasoru);
        altKlasorler = new string[klasorYollari.Length];

        //Klas�rlerin isimlerii altKlasorler dizimize kaydediyoruz
        for (int i = 0; i < klasorYollari.Length; i++)
        {
            int ayirmaIndeksi = klasorYollari[i].LastIndexOf('/');
            altKlasorler[i] = klasorYollari[i].Substring(ayirmaIndeksi + 1);
        }

        /*
        B�t�n alt klas�rleri dolanarak i�erisindeki prefablara eri�iyoruz
        ve klasorPrefablari dizimize bunlar�n dosya yollar�n� ekliyoruz.
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

    //Klas�r se�imi ekran�n� a�mam�z� sa�lar.
    void PrefabSecmeEkrani()
    {
        /*
        OpenFolderPanel komutu ile dosya se�imi i�in dosya gezginini a�abiliyoruz.
        Burada verdi�imiz ilk de�i�ken olan "Prefab Klas�r�" dosya gezgininde yazacak ba�l�k,
        ikinci de�i�ken klas�r, son de�i�ken ise se�ilebilecek dosya tipi.
        Bu ekranda se�im yap�ld���nda bize string tipinde dosya yolu verisini iletecektir. 
        Bu veriyi geciciYol isimli de�i�kene at�yoruz.
        */
        string geciciYol = EditorUtility.OpenFolderPanel("Prefab Klas�r�", "", "folder");

        /*
        Assets klas�r� alt�ndaki dosyalara bakaca��z. Bu y�zden IndexOf ve
        Substring ile filtreleme yaparak prefabKlasoru de�i�kenimizi guncelliyoruz.
        E�er "/Assets/" i�eren bir string verimiz yoksa, klas�r farkl� bir yerde se�ildi demektir.
        Bu durumda string de�eri "" hale getiriyoruz.
        Tabii ki, Assets ismine sahip alakas�z bir yerdeki alt klas�r de se�ilebilir. �ok iyi bir 
        filtreleme y�ntemi de�il. Nas�l bir yol ile daha iyi hale getirebilirsiniz diye d���nmek
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

    //Verilen dosya yolundaki prefab�n g�rseli elde etmemizi sa�lar.
    Texture2D prefabGorseliAl(string prefabYolu)
    {
        Object obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabYolu);
        /*
        Burada as�l i�i yapan GetAssetPreview komutu. Project panelindeki g�rselleri de bu kod sa�l�yor asl�nda.
        Bizim de i�imizi faslas�yla kolayla�t�r�yor. Verilen objenin g�rselini �nizleme bir g�r�nt�s�n� elde etmemizi sa�l�yor.
        */
        return AssetPreview.GetAssetPreview(obj);
    }
}
