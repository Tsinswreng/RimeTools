namespace Dks.Core;

/// 把布之道《廣韻形聲考》(msoeg 體系) 的擬音適配成 Tswg上古漢語音節。
/// 輸入是 OC_msoegDK.dict.yaml 碼欄 `ASCII*IPA` 中 `*` 之後的 IPA 段
/// （如 pʰrˤu、pʰrˤuh、m̥(r)uk、[k.r]ˤoiʔ）。
/// 本步只做**記法**轉換、信息儘量不丟；任何「多個音值收到同一個鍵」的歸併都屬於
/// 「我的擬音 → Dks 三拼」那一步（DksKeyboard + SvcDks.ToDks），不在此處做。
/// 適配規則（對應 ISvcDks.Mk上古漢語音節From布之道 的文檔）：
///   1. 小括號及內容刪掉：b(r)u → bu
///   2. 中括號刪掉、保留內容：l[a]u → lau
///   3. 音節末尾的 h（去聲）改成 s：pʰrˤuh → pʰrˤus
///   4. 清化符（組合下環 ̥ / 組合上環 ̊）改成後置 ʰ：w̥ → wʰ、n̥ˤu → nʰˤu（清化信息保留）
///   5. ɫ → j：ɫa → ja
///   6. ǝ → ə（同音異寫）
///   7. 元音韻尾 i/u → j/w：tˤui → tˤuj、dˤiuk → dˤiwk（本擬音要求用半元音作韻尾）
///   8. 聲母段緊跟輔音的 w → 上標 ʷ：kwrˤaŋʔ → kʷrˤaŋʔ（msoeg 的圓脣寫法）
/// 原樣保留、不歸併的：ɬ（ToDks 時走 s 鍵）、hl/hm/hn/hŋ/hr（msoeg 的清化流音鼻音）、
/// ʍ／w̥（清化圓脣半元音）、ml（本擬音同樣寫 ml）。
/// 例：m̥(r)uk → mʰruk ⇒ 聲母 mʰ、介腹 ru、尾調 k ⇒ 三拼 .tk。
/// 例：dˤiuk → dˤiwk ⇒ 聲母 d、介腹 ˤi、尾調 wk ⇒ 三拼 dc,。
public static partial class 布之道拼式Parser{
	/// 適配並解析；返回音節（形態與 Mk上古漢語音節FromDkp 的結果一致，Full 用本方案正規寫法）。
	/// <param name="布之道Spelling">布之道原表碼欄 `*` 之後的 IPA 段。</param>
	public static partial Tswg上古漢語音節 Parse(str 布之道Spelling);

	/// 只做字面適配、不切分（便於單測與復用）。
	/// <param name="拼式">布之道 IPA 段。</param>
	public static partial str Adapt(str 拼式);

	/// 把「緊跟在元音之後」的 i/u 改寫成半元音 j/w（適配規則第 7 條）。
	/// <param name="音">已去括號、已改符號的純音串。</param>
	private static partial str Mk韻尾半元音(str 音);

	/// 把聲母段緊跟輔音的 w 改寫成上標 ʷ（適配規則第 8 條，msoeg 的圓脣寫法）。
	/// <param name="音">已過前 7 條的純音串。</param>
	private static partial str Mk圓脣上標(str 音);
}
