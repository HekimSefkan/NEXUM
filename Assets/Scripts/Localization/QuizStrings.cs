using System.Collections.Generic;

/// <summary>
/// Quiz açıklamalarının sözlük anahtarları ve Türkçe kaynak metni.
///
/// Neden burada: açıklama metni soru asset'inin içinde DURMAZ — kullanıcıya
/// görünen her metin <c>Loc</c> üzerinden geçer (tr + en). Anahtar
/// <c>ChemistryQuiz.ExplanationKey</c> ile asset adından türetilir
/// ("quiz.soru_7.explain"), yani asset ile metin ayrışamaz.
///
/// <c>NexumLocalize</c> bu sözlüğü <c>CodeStrings.All</c> ile birlikte
/// tohumlar: sözlükte anahtar zaten varsa EZİLMEZ, düzeltme JSON'da yapılır.
///
/// KİMYA DOĞRULUĞU: metinler taslaktır; yayından önce
/// <c>docs/QUIZ_REVIEW.md</c> tablosu üzerinden tek tek doğrulanacak.
///
/// Yazım kuralları: alt simge TMP &lt;sub&gt; ile (literal ₂ fontlarda yok),
/// üst simge karakteri kullanılmaz ("H+" düz yazılır), ok karakteri yoktur.
/// </summary>
public static class QuizStrings
{
    public static readonly Dictionary<string, string> All = new Dictionary<string, string>
    {
        // --- mevcut 16 soru ---------------------------------------------
        ["quiz.soru_1.explain"] =
            "HCl suda H+ iyonu verdiği için asittir; NaOH baz, NaCl tuz, H<sub>2</sub>O ise nötrdür.",
        ["quiz.soru_2.explain"] =
            "Elektronu kendine en güçlü çeken element flordur; elektronegatiflik periyodik tabloda sağa ve yukarı doğru artar.",
        ["quiz.soru_3.explain"] =
            "İki ametal elektronlarını ortaklaşa kullanır. Elektron alışverişi olsaydı iyonik bağ oluşurdu.",
        ["quiz.soru_4.explain"] =
            "Cıva oda sıcaklığında sıvı olan tek metaldir; galyum yaklaşık 30 °C'de erir, brom ise metal değildir.",
        ["quiz.soru_5.explain"] =
            "pH 7'nin üstü bazik demektir; 14'e yaklaştıkça bazlık artar, yani 13 kuvvetli bazdır.",
        ["quiz.soru_6.explain"] =
            "Organik bileşikler karbon-hidrojen iskeletine dayanır; metan (CH<sub>4</sub>) bunun en basit örneğidir.",
        ["quiz.soru_7.explain"] =
            "Elmasta her karbon dört komşusuna kovalent bağlıdır, bu yüzden çok serttir; grafitte katmanlar birbiri üzerinde kayar.",
        ["quiz.soru_8.explain"] =
            "Sirke, suda çözünmüş asetik asittir; ekşi tat asidin verdiği H+ iyonlarından gelir.",
        ["quiz.soru_9.explain"] =
            "Katalizör aktifleşme enerjisini düşürür ve tepkimeden değişmeden çıkar; inhibitör ise tepkimeyi yavaşlatır.",
        ["quiz.soru_10.explain"] =
            "Oksijenin bağ yapmayan elektron çiftleri bağları iter; açı 104,5 dereceye düşer ve molekül açısal olur.",
        ["quiz.soru_11.explain"] =
            "Demir havadaki oksijenle yavaşça yükseltgenir; pas bir demir oksittir (Fe<sub>2</sub>O<sub>3</sub>).",
        ["quiz.soru_12.explain"] =
            "Çekirdekte proton ve nötron bulunur; elektronlar çekirdeğin çevresindeki katmanlarda yer alır.",
        ["quiz.soru_13.explain"] =
            "Saf su tek tür molekülden oluşur, yani saf maddedir; hava, çelik ve deniz suyu karışımdır.",
        ["quiz.soru_14.explain"] =
            "Kezzap, derişik nitrik asittir (HNO<sub>3</sub>); sülfürik asit ise zaç yağı adıyla bilinir.",
        ["quiz.soru_15.explain"] =
            "Soygazların son katmanı tamdır ve kolay tepkime vermezler; klor ise bir elektron alıp tuz oluşturur.",
        ["quiz.soru_16.explain"] =
            "Hidrojen oksijenle patlayarak yanar ve su oluşur; karbondioksit, helyum ve azot bu koşulda yanmaz.",

        // --- 1.5'te eklenen 20 soru (oyundaki taş ve tariflere bağlı) ----
        ["quiz.soru_17.explain"] =
            "Stratosferdeki ozon tabakası Güneş'ten gelen ultraviyole ışınların büyük bölümünü tutar.",
        ["quiz.soru_18.explain"] =
            "Kireç taşı, mermer ve yumurta kabuğu ağırlıkla kalsiyum karbonattır (CaCO<sub>3</sub>).",
        ["quiz.soru_19.explain"] =
            "Kalsiyum oksite (CaO) sönmemiş kireç denir; suyla tepkimesinde ısı açığa çıkar ve sönmüş kireç oluşur.",
        ["quiz.soru_20.explain"] =
            "Kireç taşı ısıtılınca CaO ve CO<sub>2</sub>'ye ayrışır; kireç fırınları bu tepkimeyle çalışır.",
        ["quiz.soru_21.explain"] =
            "Karbondioksit suda çözününce karbonik asit (H<sub>2</sub>CO<sub>3</sub>) oluşur; gazlı içeceklerin ekşiliği buradan gelir.",
        ["quiz.soru_22.explain"] =
            "Oksijen -2 değerlikli olduğundan iki demirin toplam yükü +6 olmalıdır; demir başına +3 düşer.",
        ["quiz.soru_23.explain"] =
            "Katıda iyonlar örgüde sabittir; su içinde Na+ ve Cl- serbest kalıp yükü taşır ve çözelti iletken olur.",
        ["quiz.soru_24.explain"] =
            "Amonyak suda hidroksit iyonu oluşturur, yani çözeltisi baziktir; temizlik ürünlerinde bu yüzden kullanılır.",
        ["quiz.soru_25.explain"] =
            "Haber-Bosch yönteminde azot ve hidrojen yüksek basınçta katalizörle amonyağa dönüşür; gübre üretiminin temelidir.",
        ["quiz.soru_26.explain"] =
            "Kuru havanın hacimce yaklaşık yüzde 78'i azot, yüzde 21'i oksijendir.",
        ["quiz.soru_27.explain"] =
            "İki azot atomu arasında üçlü bağ vardır; bu güçlü bağ azot gazını oda koşullarında çok tepkimesiz kılar.",
        ["quiz.soru_28.explain"] =
            "Motor içindeki yüksek sıcaklıkta havanın azotu ve oksijeni birleşip azot monoksit verir; bu gaz havada yükseltgenerek kirliliğe yol açar.",
        ["quiz.soru_29.explain"] =
            "Karbonmonoksit hemoglobine oksijenden çok daha güçlü bağlanır; kan oksijen taşıyamaz hâle gelir.",
        ["quiz.soru_30.explain"] =
            "Oksijen yeterliyse karbondioksit, yetersizse zehirli karbonmonoksit oluşur; sobalı ortamlar bu yüzden havalandırılır.",
        ["quiz.soru_31.explain"] =
            "Su molekülleri arasındaki hidrojen bağları güçlüdür; onları koparmak için yüksek sıcaklık gerekir.",
        ["quiz.soru_32.explain"] =
            "Her su molekülünde iki hidrojen, bir oksijen vardır; elektrolizde gazlar da 2'ye 1 hacim oranında çıkar.",
        ["quiz.soru_33.explain"] =
            "Klor güçlü bir yükseltgendir; sudaki bakteri ve virüsleri etkisiz hâle getirdiği için dezenfeksiyonda kullanılır.",
        ["quiz.soru_34.explain"] =
            "Sodyum nemle şiddetli tepkime verip hidrojen açığa çıkarır; bu yüzden havayla temas etmeyecek şekilde gazyağında saklanır.",
        ["quiz.soru_35.explain"] =
            "İkisi de yalnızca oksijen atomlarından oluşur ama atom sayıları farklıdır; aynı elementin bu tür yapılarına allotrop denir.",
        ["quiz.soru_36.explain"] =
            "Kemik ve dişin sertliğini kalsiyum tuzları sağlar; kalsiyum insan vücudunda en bol bulunan metaldir.",
    };
}
