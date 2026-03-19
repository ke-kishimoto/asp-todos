using System.ComponentModel.DataAnnotations;

namespace MyTodo.Web.Models;

// -----------------------------------------------------------------------
// ItemCreateViewModel : アイテム登録フォームのモデル
//
// ★ ViewModel の役割：
//   - View（画面）とのデータのやり取りを担う
//   - [Required] などのバリデーション属性を付けることで
//     入力チェックを宣言的に定義できる
//   - Domain モデル（Item）に直接 UI の都合を入れないための分離
// -----------------------------------------------------------------------
public class ItemCreateViewModel
{
    // [Required] : 未入力のとき ModelState.IsValid が false になる
    // ErrorMessage : バリデーションエラー時に表示するメッセージ
    [Required(ErrorMessage = "品目コードは必須です")]
    [MaxLength(50, ErrorMessage = "品目コードは50文字以内で入力してください")]
    public string ItemCode { get; set; } = "";

    [Required(ErrorMessage = "品目名は必須です")]
    [MaxLength(100, ErrorMessage = "品目名は100文字以内で入力してください")]
    public string ItemName { get; set; } = "";

    // [Range] : 数値の範囲バリデーション
    [Range(0, int.MaxValue, ErrorMessage = "単価は0以上の値を入力してください")]
    public int Price { get; set; } = 0;
}
