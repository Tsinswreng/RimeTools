namespace Dks.Core;

/// 從 Dkp 表的拼式解析出上古漢語音節。
/// 拼式 =「義符字形」+「韻」兩段：
///   義符字形用義符表查得聲母（手→nʰ、土→tʰ、言→ŋ、貝→p）；
///   韻段可以是聲符字形（再用聲符表查，成→eŋ、生→ˁeŋʔ、工→ˁuŋ），也可以直接寫成音（介腹+尾調）。
/// 例：挪的拼式 手ˁaj → 聲母 nʰ（手）、韻 ˁaj ⇒ 介腹 ˁa、尾調 j，Full = nʰˁaj。
/// 例：誠的拼式 言成  → 聲母 ŋ（言）、韻 成→eŋ ⇒ 介腹 e、尾調 ŋ，Full = ŋeŋ。
/// 例：辣的拼式 rˁat（沒有義符字形，整串就是音）⇒ 聲母 r、介腹 ˁa、尾調 t，Full = rˁat。
///   注意 r 歸**聲母**；ToDks 時因聲母是 r，介腹改取帶 r 那一形（rˁa）得次鍵 Y ⇒ 三拼 ryt。
/// 全程只用義符表/聲符表查字典與字串切分，不使用任何模式匹配。
public static partial class Dkp拼式Parser{
	/// 韻腹元音集（切韻用）：最後一個元音之前（含該元音）為介腹，之後為尾調。
	/// 例：eŋ ⇒ 介腹 e、尾調 ŋ；rˁat ⇒ 介腹 rˁa、尾調 t；ˁa ⇒ 介腹 ˁa、尾調（空）。
	private const str 元音集 = "aeiouəæA";

	/// 解析拼式，返回音節（聲母/介腹/尾調/Full 都填好）。
	/// 查不到字形或切不出聲母時不拋異常：能填的段照填，Full 至少是正規化後的原串。
	/// <param name="拼式">Dkp 表碼欄原樣，如 手ˁaj、言成、rˁat。</param>
	public static partial Tswg上古漢語音節 Parse(str 拼式);

	/// 把韻段切成介腹與尾調：以最後一個元音為界；沒有元音則整段當介腹、尾調為空。
	/// <param name="韻">義符之後的整段，如 ˁaj、eŋ、rˁaŋʔ。</param>
	private static partial (str 介腹, str 尾調) 切韻(str 韻);

	/// 歸一到本方案正規寫法並去掉首尾空白：
	/// 非三等標記 ˤ/ˁ→'、圓脣送氣並存時 ʷʰ→ʰʷ、清化一律後置 ʰ（n̥→nʰ）、濁塞音 g→ɡ。
	/// <param name="音">單段音值（聲母/介腹/尾調任一）。</param>
	private static partial str Normalize(str 音);
}
