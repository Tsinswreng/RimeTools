namespace Dks.Test;

/// 測試用文本工具（跨域共用的小件）。
public static class TestText{
	/// 造一個換行固定為 `\n` 的 StringWriter。
	/// 庫本身不決定換行（端點落盤時才由平台決定），但文本級斷言不該被 Windows 的 CRLF 干擾。
	public static StringWriter MkWriter(){
		return new StringWriter{ NewLine = "\n" };
	}

	/// 把 CRLF 規範成 LF（對真實文件內容做文本級斷言時用）。
	public static str Lf(str Text){
		return Text.Replace("\r\n", "\n");
	}
}
