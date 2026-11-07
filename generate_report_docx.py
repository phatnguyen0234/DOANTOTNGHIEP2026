import os
import zipfile
import html

def escape_xml(text):
    if text is None:
        return ""
    return html.escape(str(text))

class DocxBuilder:
    def __init__(self, filename):
        self.filename = filename
        self.paragraphs = []
        
    def add_title(self, text):
        xml = f"""
        <w:p>
            <w:pPr>
                <w:jc w:val="center"/>
                <w:spacing w:before="360" w:after="240" w:line="360" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="36"/>
                    <w:szCs w:val="36"/>
                    <w:color w:val="1F4E79"/>
                </w:rPr>
                <w:t>{escape_xml(text)}</w:t>
            </w:r>
        </w:p>"""
        self.paragraphs.append(xml)

    def add_subtitle(self, text):
        xml = f"""
        <w:p>
            <w:pPr>
                <w:jc w:val="center"/>
                <w:spacing w:before="0" w:after="360" w:line="280" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:i/>
                    <w:sz w:val="26"/>
                    <w:szCs w:val="26"/>
                    <w:color w:val="595959"/>
                </w:rPr>
                <w:t>{escape_xml(text)}</w:t>
            </w:r>
        </w:p>"""
        self.paragraphs.append(xml)

    def add_heading1(self, text):
        xml = f"""
        <w:p>
            <w:pPr>
                <w:spacing w:before="360" w:after="160" w:line="300" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="30"/>
                    <w:szCs w:val="30"/>
                    <w:color w:val="1F4E79"/>
                </w:rPr>
                <w:t>{escape_xml(text)}</w:t>
            </w:r>
        </w:p>"""
        self.paragraphs.append(xml)

    def add_heading2(self, text):
        xml = f"""
        <w:p>
            <w:pPr>
                <w:spacing w:before="260" w:after="120" w:line="280" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="26"/>
                    <w:szCs w:val="26"/>
                    <w:color w:val="2E75B6"/>
                </w:rPr>
                <w:t>{escape_xml(text)}</w:t>
            </w:r>
        </w:p>"""
        self.paragraphs.append(xml)

    def add_heading3(self, text):
        xml = f"""
        <w:p>
            <w:pPr>
                <w:spacing w:before="180" w:after="80" w:line="260" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="24"/>
                    <w:szCs w:val="24"/>
                    <w:color w:val="333333"/>
                </w:rPr>
                <w:t>{escape_xml(text)}</w:t>
            </w:r>
        </w:p>"""
        self.paragraphs.append(xml)

    def add_paragraph(self, text, bold_prefix=None):
        runs = []
        if bold_prefix:
            runs.append(f"""
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="24"/>
                    <w:szCs w:val="24"/>
                </w:rPr>
                <w:t>{escape_xml(bold_prefix)}</w:t>
            </w:r>""")
        runs.append(f"""
        <w:r>
            <w:rPr>
                <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                <w:sz w:val="24"/>
                <w:szCs w:val="24"/>
            </w:rPr>
            <w:t xml:space="preserve">{escape_xml(text)}</w:t>
        </w:r>""")
        
        xml = f"""
        <w:p>
            <w:pPr>
                <w:jc w:val="both"/>
                <w:spacing w:before="60" w:after="120" w:line="300" w:lineRule="auto"/>
            </w:pPr>
            {''.join(runs)}
        </w:p>"""
        self.paragraphs.append(xml)

    def add_bullet(self, text, bold_prefix=None):
        runs = []
        if bold_prefix:
            runs.append(f"""
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:b/>
                    <w:sz w:val="24"/>
                    <w:szCs w:val="24"/>
                </w:rPr>
                <w:t>{escape_xml(bold_prefix)}</w:t>
            </w:r>""")
        runs.append(f"""
        <w:r>
            <w:rPr>
                <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                <w:sz w:val="24"/>
                <w:szCs w:val="24"/>
            </w:rPr>
            <w:t xml:space="preserve">{escape_xml(text)}</w:t>
        </w:r>""")
        
        xml = f"""
        <w:p>
            <w:pPr>
                <w:ind w:left="480" w:hanging="240"/>
                <w:jc w:val="both"/>
                <w:spacing w:before="40" w:after="80" w:line="280" w:lineRule="auto"/>
            </w:pPr>
            <w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman"/>
                    <w:sz w:val="24"/>
                    <w:szCs w:val="24"/>
                </w:rPr>
                <w:t>• </w:t>
            </w:r>
            {''.join(runs)}
        </w:p>"""
        self.paragraphs.append(xml)

    def add_code_block(self, code, title=None):
        if title:
            t_xml = f"""
            <w:p>
                <w:pPr>
                    <w:spacing w:before="160" w:after="40" w:line="240" w:lineRule="auto"/>
                </w:pPr>
                <w:r>
                    <w:rPr>
                        <w:rFonts w:ascii="Consolas" w:hAnsi="Consolas"/>
                        <w:b/>
                        <w:sz w:val="20"/>
                        <w:szCs w:val="20"/>
                        <w:color w:val="1F4E79"/>
                    </w:rPr>
                    <w:t>{escape_xml(title)}</w:t>
                </w:r>
            </w:p>"""
            self.paragraphs.append(t_xml)

        lines = code.strip().split("\n")
        line_elements = []
        for i, line in enumerate(lines):
            line_elements.append(f"""<w:r>
                <w:rPr>
                    <w:rFonts w:ascii="Consolas" w:hAnsi="Consolas"/>
                    <w:sz w:val="19"/>
                    <w:szCs w:val="19"/>
                    <w:color w:val="24292E"/>
                </w:rPr>
                <w:t xml:space="preserve">{escape_xml(line)}</w:t>
            </w:r>""")
            if i < len(lines) - 1:
                line_elements.append("<w:r><w:br/></w:r>")

        xml = f"""
        <w:p>
            <w:pPr>
                <w:pBdr>
                    <w:top w:val="single" w:sz="6" w:space="4" w:color="D0D7DE"/>
                    <w:left w:val="single" w:sz="18" w:space="6" w:color="2E75B6"/>
                    <w:bottom w:val="single" w:sz="6" w:space="4" w:color="D0D7DE"/>
                    <w:right w:val="single" w:sz="6" w:space="4" w:color="D0D7DE"/>
                </w:pBdr>
                <w:shd w:val="clear" w:color="auto" w:fill="F6F8FA"/>
                <w:ind w:left="240" w:right="240"/>
                <w:spacing w:before="60" w:after="160" w:line="220" w:lineRule="auto"/>
            </w:pPr>
            {''.join(line_elements)}
        </w:p>"""
        self.paragraphs.append(xml)

    def save(self):
        content_types = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>"""

        rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>"""

        doc_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>"""

        styles = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:docDefaults>
    <w:rPrDefault>
      <w:rPr>
        <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:cs="Times New Roman"/>
        <w:sz w:val="24"/>
        <w:szCs w:val="24"/>
        <w:lang w:val="vi-VN"/>
      </w:rPr>
    </w:rPrDefault>
    <w:pPrDefault>
      <w:pPr>
        <w:spacing w:line="300" w:lineRule="auto" w:after="120"/>
      </w:pPr>
    </w:pPrDefault>
  </w:docDefaults>
</w:styles>"""

        body_content = "".join(self.paragraphs)
        document = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    {body_content}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440"/>
    </w:sectPr>
  </w:body>
</w:document>"""

        with zipfile.ZipFile(self.filename, "w", zipfile.ZIP_DEFLATED) as z:
            z.writestr("[Content_Types].xml", content_types)
            z.writestr("_rels/.rels", rels)
            z.writestr("word/_rels/document.xml.rels", doc_rels)
            z.writestr("word/styles.xml", styles)
            z.writestr("word/document.xml", document)
        print(f"File created successfully: {self.filename}")

def build_full_report():
    doc_path = r"D:\Game_Project\Totnghiep2026\BaoCao_ThietKe_HeThong_3.5_3.6_3.7.docx"
    doc = DocxBuilder(doc_path)
    
    # Title
    doc.add_title("BÁO CÁO THIẾT KẾ KIẾN TRÚC VÀ CHI TIẾT XỬ LÝ HỆ THỐNG")
    doc.add_subtitle("ĐỒ ÁN TỐT NGHIỆP: GAME NÔNG TRẠI 2D (FARMING RPG)")
    
    # =========================================================================
    # 3.5. THIẾT KẾ CHỨC NĂNG QUẢN LÝ KHO ĐỒ
    # =========================================================================
    doc.add_heading1("3.5. THIẾT KẾ CHỨC NĂNG QUẢN LÝ KHO ĐỒ (INVENTORY SYSTEM)")
    
    doc.add_heading2("3.5.1. Thiết kế kiến trúc và cấu trúc dữ liệu kho đồ")
    doc.add_paragraph("Hệ thống quản lý kho đồ (Inventory System) chịu trách nhiệm quản lý toàn bộ vòng đời của vật phẩm, số lượng, phẩm chất và sức chứa trong trò chơi nông trại 2D. Hệ thống áp dụng các mô hình và nguyên lý thiết kế phần mềm hiện đại:")
    doc.add_bullet(" Tách biệt tầng lưu trữ dữ liệu (Model: Inventory, InventorySlot), tầng hiển thị giao diện (View: InventoryUI, InventorySlotUI, HotbarUI) và các bộ điều phối tương tác (Controller: HotbarController, InventoryTransferHelper).", "Mô hình MVC (Model - View - Controller):")
    doc.add_bullet(" Inventory phát ra sự kiện OnInventoryChanged mỗi khi có biến động dữ liệu (thêm, bớt, gộp stack, xóa ô). Giao diện UI tự động cập nhật mà không tạo ra quan hệ phụ thuộc ngược.", "Mẫu thiết kế Observer Pattern:")
    doc.add_bullet(" Inventory.Instance và InventoryUI.Instance cho phép các phân hệ khác (Trồng trọt, Chặt cây, Đập đá, Câu cá, Cửa hàng) truy cập kho đồ người chơi tiện lợi và an toàn.", "Mẫu thiết kế Singleton Pattern:")
    doc.add_bullet(" Sử dụng ItemData (ScriptableObject) lưu trữ thông số bất biến tĩnh; kết hợp DTO (InventoryData, InventorySlotData) để tuần tự hóa (Serialization) sang JSON khi lưu game.", "Tách biệt Dữ liệu Tĩnh và Runtime:")

    doc.add_heading3("Cấu trúc thiết kế lớp (Class Specifications)")
    doc.add_bullet(" Lưu thông tin định danh (ItemID, ItemName), hình ảnh (Icon), loại vật phẩm (ItemType, ToolType), giới hạn xếp chồng (MaxStack), giá mua bán (BuyPrice, SellPrice, CanSell).", "1. ItemData (ScriptableObject):")
    doc.add_bullet(" Quản lý trạng thái 1 ô chứa gồm ItemData, Amount và CropQuality (Normal, Silver, Gold, Iridium). Cung cấp các hàm nguyên tử: IsEmpty(), CanStack(), RemainingSpace(), AddAmount(), RemoveAmount(), SetItem(), SwapWith(), Clear().", "2. InventorySlot:")
    doc.add_bullet(" Lớp nghiệp vụ trung tâm gắn trên GameObject Player/Chest. Quản lý danh sách List<InventorySlot> với sức chứa capacity. Cung cấp API công khai: AddItem(), TryAddItem(), TryRemoveItem(), MoveOrSwapSlots(), SwapSlots(), GetItemCount(), HasItem().", "3. Inventory (MonoBehaviour):")
    doc.add_bullet(" Quản lý giao diện tổng thể túi đồ, điều phối tạo các ô UI, bắt phím ESC để bật/tắt túi đồ và xử lý hiển thị Drag Ghost khi kéo chuột.", "4. InventoryUI & InventorySlotUI:")

    doc.add_heading3("Mã nguồn triển khai chi tiết C# - Tầng Model & Cấu hình dữ liệu")
    
    code_itemdata = """using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Farming/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemType itemType;

    [Header("Tool & Farming Action Settings")]
    [SerializeField] private ToolType toolType = ToolType.None;
    [SerializeField] private string cropId = "Carrot";
    [SerializeField] private CropData cropData;

    [Header("Stack & Economy")]
    [SerializeField, Min(1)] private int maxStack = 99;
    [SerializeField, Min(0)] private int buyPrice = 0;
    [SerializeField, Min(0)] private int sellPrice = 0;
    [SerializeField] private bool canSell = true;

    public string ItemID => itemID;
    public string ItemName => itemName;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public ToolType ToolType => toolType;
    public int MaxStack => maxStack;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
    public bool CanSell => canSell;
}"""
    doc.add_code_block(code_itemdata, "Mã nguồn: ItemData.cs")

    code_inventoryslot = """using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount;
    [SerializeField] private CropQuality quality = CropQuality.Normal;

    public ItemData ItemData => itemData;
    public int Amount => amount;
    public CropQuality Quality => quality;

    public InventorySlot() { Clear(); }

    public InventorySlot(ItemData itemData, int amount, CropQuality quality = CropQuality.Normal)
    {
        if (itemData != null && amount > 0)
        {
            this.itemData = itemData;
            this.amount = Mathf.Min(amount, itemData.MaxStack);
            this.quality = quality;
        }
        else { Clear(); }
    }

    public bool IsEmpty() => itemData == null || amount <= 0;

    public bool CanStack(ItemData item, CropQuality itemQuality = CropQuality.Normal)
    {
        if (IsEmpty() || item == null || itemData == null) return false;
        return itemData.ItemID == item.ItemID && this.quality == itemQuality && amount < itemData.MaxStack;
    }

    public int RemainingSpace()
    {
        if (IsEmpty() || itemData == null) return 0;
        return Mathf.Max(0, itemData.MaxStack - amount);
    }

    public int AddAmount(int amountToAdd)
    {
        if (amountToAdd <= 0 || IsEmpty()) return amountToAdd;
        int space = RemainingSpace();
        int added = Mathf.Min(space, amountToAdd);
        amount += added;
        return amountToAdd - added;
    }

    public int RemoveAmount(int amountToRemove)
    {
        if (IsEmpty() || amountToRemove <= 0) return 0;
        int removed = Mathf.Min(amount, amountToRemove);
        amount -= removed;
        if (amount <= 0) Clear();
        return removed;
    }

    public void SetItem(ItemData newItem, int newAmount, CropQuality newQuality = CropQuality.Normal)
    {
        if (newItem == null || newAmount <= 0) { Clear(); return; }
        itemData = newItem;
        amount = Mathf.Min(newAmount, newItem.MaxStack);
        quality = newQuality;
    }

    public void SwapWith(InventorySlot other)
    {
        if (other == null) return;
        ItemData tempItem = this.itemData;
        int tempAmount = this.amount;
        CropQuality tempQuality = this.quality;

        this.itemData = other.itemData;
        this.amount = other.amount;
        this.quality = other.quality;

        other.itemData = tempItem;
        other.amount = tempAmount;
        other.quality = tempQuality;
    }

    public void Clear()
    {
        itemData = null;
        amount = 0;
        quality = CropQuality.Normal;
    }
}"""
    doc.add_code_block(code_inventoryslot, "Mã nguồn: InventorySlot.cs")

    doc.add_heading2("3.5.2. Thiết kế các chức năng xử lý và tương tác kho đồ")
    doc.add_paragraph("Kho đồ hỗ trợ các luồng nghiệp vụ phức tạp đảm bảo tính toàn vẹn dữ liệu:")
    doc.add_bullet(" Khi nhận vật phẩm mới, hàm AddItem duyệt qua các slot để xếp chồng vào các ô cùng loại và cùng phẩm cấp (quality) trước; phần dư tiếp tục điền vào ô rỗng đầu tiên tìm thấy. Nếu kho đầy, hàm trả về số lượng còn dư chính xác để bộ sinh đồ (ItemDropSpawner) tạo vật phẩm văng ra mặt đất.", "1. Luồng tiếp nhận và xếp chồng (Add / Stack Item Flow):")
    doc.add_bullet(" Sử dụng giao diện IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler. Cho phép di chuyển toàn bộ, tách 1/2 stack khi bấm chuột phải, hoặc gộp/đổi chỗ 2 ô thông qua MoveOrSwapSlots và InventoryTransferHelper.", "2. Luồng tương tác kéo thả (Drag & Drop / Swap Flow):")
    doc.add_bullet(" Người chơi bấm phím ESC để kích hoạt ToggleBag(). Giao diện Hotbar và túi đồ phối hợp hiển thị hài hòa, chặn tương tác nhân vật khi đang mở túi.", "3. Bật tắt túi đồ (Bag Toggle Flow):")
    doc.add_bullet(" Dữ liệu kho được chuyển đổi thành InventoryData và InventorySlotData, ghi file JSON. Khi tải game, ItemDatabase tìm lại ItemData theo ItemID và khôi phục chính xác từng ô.", "4. Luồng lưu và khôi phục (Save & Load Flow):")

    code_inventory = """using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField, Min(1)] private int capacity = 20;
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();
    [SerializeField] private bool isPlayerInventory = true;
    [SerializeField] private KeyCode toggleBagKey = KeyCode.Escape;
    [SerializeField] private InventoryUI inventoryUI;

    public event Action OnInventoryChanged;
    public static Inventory Instance { get; private set; }

    public int Capacity => capacity;
    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        if (isPlayerInventory)
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        InitializeSlots();
    }

    private void Update()
    {
        if (isPlayerInventory && Input.GetKeyDown(toggleBagKey))
        {
            ToggleBag();
        }
    }

    private void InitializeSlots()
    {
        if (slots == null) slots = new List<InventorySlot>();
        while (slots.Count < capacity) slots.Add(new InventorySlot());
        while (slots.Count > capacity) slots.RemoveAt(slots.Count - 1);
    }

    public bool IsValidIndex(int index) => slots != null && index >= 0 && index < slots.Count;
    public InventorySlot GetSlot(int index) => IsValidIndex(index) ? slots[index] : null;

    public int AddItem(ItemData item, int amount, CropQuality quality = CropQuality.Normal)
    {
        if (item == null || amount <= 0) return amount;
        int remainingAmount = amount;

        // 1. Stack vào các ô cùng loại & phẩm chất
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].CanStack(item, quality))
            {
                remainingAmount = slots[i].AddAmount(remainingAmount);
                if (remainingAmount <= 0) break;
            }
        }

        // 2. Tạo ô mới nếu còn dư
        if (remainingAmount > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty())
                {
                    int addCount = Mathf.Min(remainingAmount, item.MaxStack);
                    slots[i].SetItem(item, addCount, quality);
                    remainingAmount -= addCount;
                    if (remainingAmount <= 0) break;
                }
            }
        }

        if (amount - remainingAmount > 0)
        {
            OnInventoryChanged?.Invoke();
        }

        return remainingAmount;
    }

    public bool MoveOrSwapSlots(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return false;

        InventorySlot fromSlot = slots[fromIndex];
        InventorySlot toSlot = slots[toIndex];
        if (fromSlot.IsEmpty()) return false;

        if (toSlot.IsEmpty())
        {
            toSlot.SetItem(fromSlot.ItemData, fromSlot.Amount, fromSlot.Quality);
            fromSlot.Clear();
            OnInventoryChanged?.Invoke();
            return true;
        }

        if (toSlot.CanStack(fromSlot.ItemData, fromSlot.Quality))
        {
            int remaining = toSlot.AddAmount(fromSlot.Amount);
            if (remaining <= 0) fromSlot.Clear();
            else fromSlot.SetItem(fromSlot.ItemData, remaining, fromSlot.Quality);
            OnInventoryChanged?.Invoke();
            return true;
        }

        fromSlot.SwapWith(toSlot);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ToggleBag()
    {
        if (inventoryUI != null) inventoryUI.ToggleBag();
    }
}"""
    doc.add_code_block(code_inventory, "Mã nguồn: Inventory.cs (Core Business Logic)")

    code_dto = """using System;
using System.Collections.Generic;

[Serializable]
public class InventoryData 
{
    public int capacity;
    public List<InventorySlotData> slots = new List<InventorySlotData>();

    public InventoryData(int capacity, List<InventorySlotData> slots)
    {
        this.capacity = capacity;
        this.slots = slots;
    }
}

[Serializable]
public class InventorySlotData 
{
    public int slotIndex;
    public string itemId;
    public int amount;

    public InventorySlotData(int slotIndex, string itemId, int amount)
    {
        this.slotIndex = slotIndex;
        this.itemId = itemId;
        this.amount = amount;
    }
}"""
    doc.add_code_block(code_dto, "Mã nguồn: InventoryData.cs & InventorySlotData.cs (DTO)")

    # =========================================================================
    # 3.6. THIẾT KẾ CHỨC NĂNG RƠI ĐỒ
    # =========================================================================
    doc.add_heading1("3.6. THIẾT KẾ CHỨC NĂNG RƠI ĐỒ (DROP SYSTEM)")
    
    doc.add_heading2("3.6.1. Thiết kế kiến trúc và cấu trúc dữ liệu hệ thống rơi đồ")
    doc.add_paragraph("Hệ thống rơi đồ (Drop System) được xây dựng nhằm chuẩn hóa việc tạo, tính toán xác suất và thu gom vật phẩm trong thế giới 2D khi người chơi tương tác với môi trường (chặt cây, khai thác đá, thu hoạch nông sản, câu cá, diệt quái).")
    doc.add_bullet(" Bất kỳ thực thể nào có thể rơi vật phẩm đều cài đặt IDropSource, cung cấp DropTable, vị trí rơi và DropContext (chỉ số may mắn, cấp công cụ).", "Interface chuẩn hóa (IDropSource):")
    doc.add_bullet(" DropSystem đóng vai trò Facade tĩnh, cung cấp TriggerDrop(IDropSource) giúp mã gameplay gọi rơi đồ chỉ với 1 dòng lệnh duy nhất.", "Facade Pattern tập trung:")
    doc.add_bullet(" Tách biệt tính ngẫu nhiên (DropGenerator) khỏi việc sinh đối tượng đồ họa (ItemDropSpawner) và thực thể tương tác vật lý (WorldItem).", "Phân tách trách nhiệm chuyên biệt:")

    code_dropsource = """using UnityEngine;

public interface IDropSource
{
    DropTable GetDropTable();
    Vector3 GetDropPosition();
    DropContext GetDropContext();
}

[System.Serializable]
public struct DropContext
{
    public object source;
    public float luck;
    public int toolLevel;
    public float dropMultiplier;
}"""
    doc.add_code_block(code_dropsource, "Mã nguồn: IDropSource.cs & DropContext.cs")

    code_droptable = """using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DropEntry
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int minAmount = 1;
    [SerializeField, Min(1)] private int maxAmount = 1;
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;
    [SerializeField] private bool affectedByLuck = false;
    [SerializeField] private bool affectedByToolLevel = false;

    public ItemData Item => item;
    public int MinAmount => minAmount;
    public int MaxAmount => maxAmount;
    public float DropChance => dropChance;
    public bool AffectedByLuck => affectedByLuck;
    public bool AffectedByToolLevel => affectedByToolLevel;
}

[CreateAssetMenu(fileName = "NewDropTable", menuName = "Farming/Drop Table")]
public class DropTable : ScriptableObject
{
    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();
    public IReadOnlyList<DropEntry> Entries => entries;
}"""
    doc.add_code_block(code_droptable, "Mã nguồn: DropEntry.cs & DropTable.cs")

    code_dropgenerator = """using System.Collections.Generic;
using UnityEngine;

public struct ItemDropResult
{
    public ItemData item;
    public int amount;
    public ItemDropResult(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

public static class DropGenerator
{
    public static List<ItemDropResult> Generate(DropTable table, DropContext context = default)
    {
        List<ItemDropResult> results = new List<ItemDropResult>();
        if (table == null || table.Entries == null) return results;

        foreach (DropEntry entry in table.Entries)
        {
            if (entry == null || entry.Item == null) continue;

            float effectiveChance = entry.DropChance;
            if (entry.AffectedByLuck && context.luck > 0f)
                effectiveChance += context.luck * 0.05f;

            if (Random.value > Mathf.Clamp01(effectiveChance)) continue;

            int amount = Random.Range(entry.MinAmount, entry.MaxAmount + 1);
            if (entry.AffectedByToolLevel && context.toolLevel > 1)
                amount += (context.toolLevel - 1);

            if (context.dropMultiplier > 0f && !Mathf.Approximately(context.dropMultiplier, 1f))
                amount = Mathf.RoundToInt(amount * context.dropMultiplier);

            if (amount > 0) results.Add(new ItemDropResult(entry.Item, amount));
        }
        return results;
    }
}

public static class DropSystem
{
    public static void TriggerDrop(IDropSource source, DropContext customContext = default)
    {
        if (source == null) return;
        DropTable table = source.GetDropTable();
        if (table == null) return;

        DropContext context = customContext.source != null ? customContext : source.GetDropContext();
        TriggerDrop(table, source.GetDropPosition(), context);
    }

    public static void TriggerDrop(DropTable table, Vector3 position, DropContext context = default)
    {
        if (table == null) return;
        List<ItemDropResult> drops = DropGenerator.Generate(table, context);
        if (drops.Count > 0 && ItemDropSpawner.Instance != null)
        {
            ItemDropSpawner.Instance.SpawnDrops(drops, position);
        }
    }
}"""
    doc.add_code_block(code_dropgenerator, "Mã nguồn: DropGenerator.cs & DropSystem.cs (Facade)")

    doc.add_heading2("3.6.2. Thiết kế các chức năng xử lý và thu nhặt vật phẩm")
    doc.add_paragraph("Hệ thống xử lý vật lý trực quan và cơ chế thu gom tự động:")
    doc.add_bullet(" WorldItem thực hiện chuyển động Parabol văng nảy trong không gian 2D với độ cao bounceHeight và thời gian tossDuration, tiếp theo là một cú nảy phụ nhỏ (mini bounce) tạo cảm giác chân thực trước khi cho phép người chơi nhặt.", "1. Hiệu ứng văng nảy Parabol:")
    doc.add_bullet(" Sau khi tiếp đất, WorldItem tự động quét xung quanh trong bán kính mergeRadius = 0.5f. Nếu phát hiện vật phẩm cùng ItemID và cùng CropQuality, 2 đối tượng sẽ gộp lại thành 1 để tối ưu hiệu năng và tránh phân mảnh Collider.", "2. Tự động gộp dưới đất (Ground Merge):")
    doc.add_bullet(" ItemCollector gắn trên Player phân tách 2 vùng bán kính: Magnet Radius (bán kính 2.0m - hút vật phẩm bay tăng tốc về phía nhân vật) và Pickup Radius (bán kính 0.5m - chuyển vật phẩm vào kho đồ).", "3. Nam châm hút và thu gom (Magnet & Pickup):")
    doc.add_bullet(" Nếu kho đồ đầy hoặc chỉ còn chỗ cho một phần, WorldItem.SetAmount giữ lại số lượng dư trên mặt đất, không làm mất mát tài nguyên của người chơi.", "4. Cơ chế chống mất đồ khi tràn kho:")

    code_worlditem = """using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float bounceHeight = 0.6f;
    [SerializeField] private float tossDuration = 0.35f;
    [SerializeField] private float pickupDelay = 0.2f;
    [SerializeField] private float baseMagnetSpeed = 6f;
    [SerializeField] private float magnetAcceleration = 8f;
    [SerializeField] private float mergeRadius = 0.5f;

    private ItemData item;
    private int amount;
    private CropQuality quality = CropQuality.Normal;
    private bool canPickup = false;
    private bool isCollected = false;
    private Transform magnetTarget;
    private float currentMagnetSpeed;

    public ItemData Item => item;
    public int Amount => amount;
    public CropQuality Quality => quality;
    public bool CanPickup => canPickup && !isCollected;

    public void Initialize(ItemData itemData, int itemAmount, Vector3? landingPosition = null, CropQuality itemQuality = CropQuality.Normal)
    {
        item = itemData;
        amount = itemAmount;
        quality = itemQuality;
        isCollected = false;
        magnetTarget = null;
        currentMagnetSpeed = baseMagnetSpeed;
        if (spriteRenderer != null && item != null) spriteRenderer.sprite = item.Icon;

        if (landingPosition.HasValue)
            StartCoroutine(TossAnimationRoutine(transform.position, landingPosition.Value));
        else
            canPickup = true;
    }

    private IEnumerator TossAnimationRoutine(Vector3 startPos, Vector3 targetPos)
    {
        canPickup = false;
        float elapsed = 0f;

        // Pha 1: Parabol chính
        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tossDuration);
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arc = 4f * bounceHeight * t * (1f - t);
            transform.position = linearPos + new Vector3(0f, arc, 0f);
            yield return null;
        }

        // Pha 2: Cú nảy phụ
        float miniDuration = 0.15f;
        elapsed = 0f;
        while (elapsed < miniDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / miniDuration);
            float arc = 4f * (bounceHeight * 0.25f) * t * (1f - t);
            transform.position = targetPos + new Vector3(0f, arc, 0f);
            yield return null;
        }

        transform.position = targetPos;
        if (pickupDelay > 0f) yield return new WaitForSeconds(pickupDelay);
        canPickup = true;

        TryMergeWithNearby();
    }

    public void TryMergeWithNearby()
    {
        if (!CanPickup) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, mergeRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject && hit.TryGetComponent<WorldItem>(out WorldItem other))
            {
                if (CanMergeWith(other))
                {
                    int total = this.amount + other.amount;
                    if (total <= item.MaxStack)
                    {
                        this.amount = total;
                        other.MarkAsCollected();
                        Destroy(other.gameObject);
                    }
                }
            }
        }
    }

    public bool CanMergeWith(WorldItem other)
    {
        return other != null && CanPickup && other.CanPickup &&
               item.ItemID == other.Item.ItemID && quality == other.Quality;
    }

    public void AttractTo(Transform target) { if (CanPickup) magnetTarget = target; }
    public void MarkAsCollected() { isCollected = true; canPickup = false; }
    public void SetAmount(int newAmount)
    {
        amount = newAmount;
        if (amount <= 0) Destroy(gameObject);
    }

    private void Update()
    {
        if (canPickup && magnetTarget != null && !isCollected)
        {
            currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, magnetTarget.position, currentMagnetSpeed * Time.deltaTime);
        }
    }
}"""
    doc.add_code_block(code_worlditem, "Mã nguồn: WorldItem.cs (Quỹ đạo Parabol & Gộp đồ)")

    code_collector = """using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField, Min(0.1f)] private float magnetRadius = 2.0f;
    [SerializeField, Min(0.1f)] private float pickupRadius = 0.5f;
    [SerializeField] private LayerMask itemLayerMask = ~0;
    private readonly Collider2D[] hitBuffer = new Collider2D[20];

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();
    }

    private void Update()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, magnetRadius, hitBuffer, itemLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].TryGetComponent<WorldItem>(out WorldItem worldItem) && worldItem.CanPickup)
            {
                float dist = Vector2.Distance(transform.position, worldItem.transform.position);
                if (dist <= pickupRadius)
                    Collect(worldItem);
                else
                    worldItem.AttractTo(transform);
            }
        }
    }

    public void Collect(WorldItem worldItem)
    {
        if (worldItem == null || !worldItem.CanPickup || inventory == null) return;

        int remaining = inventory.AddItem(worldItem.Item, worldItem.Amount, worldItem.Quality);
        if (remaining <= 0)
        {
            worldItem.MarkAsCollected();
            Destroy(worldItem.gameObject);
        }
        else
        {
            worldItem.SetAmount(remaining);
        }
    }
}"""
    doc.add_code_block(code_collector, "Mã nguồn: ItemCollector.cs (Thu gom & Xử lý tràn kho)")

    # =========================================================================
    # 3.7. THIẾT KẾ CHỨC NĂNG CÂU CÁ
    # =========================================================================
    doc.add_heading1("3.7. THIẾT KẾ CHỨC NĂNG CÂU CÁ (FISHING SYSTEM)")
    
    doc.add_heading2("3.7.1. Thiết kế kiến trúc và cấu trúc dữ liệu hệ thống câu cá")
    doc.add_paragraph("Hệ thống câu cá mô phỏng hoàn chỉnh cơ chế câu cá kinh điển (Stardew Valley-like), chia thành các phân hệ hoạt động phối hợp thông qua FishingController:")
    doc.add_bullet(" Quản lý chu trình trạng thái câu cá: Idle -> Charging -> Casting -> WaitingForBite -> FishBite -> Hooked -> Reeling -> Success/Failed.", "Máy trạng thái (FishingStateMachine):")
    doc.add_bullet(" Nhận diện vùng nước hợp lệ dựa trên Tilemap hoặc Collider LayerMask, phân vùng câu (Biển, Sông, Hồ) thông qua FishingSpotData.", "Kiểm tra mặt nước (WaterDetector):")
    doc.add_bullet(" Mô phỏng dây câu (LineRenderer) và phao câu nhấp nhô dập dềnh theo sóng nước.", "Ném phao và hiển thị (CastingController & FishingBobber):")
    doc.add_bullet(" Tách biệt thanh vật lý di chuyển theo chuột (FishingBarPhysics), thuật toán AI cá bơi ngẫu nhiên (FishMovementController) và bộ tính tiến độ bắt (FishingProgress).", "Màn chơi kéo cá (FishingMinigame):")

    code_fishingstate = """using System;

public enum FishingState
{
    Idle,
    Charging,
    Casting,
    WaitingForBite,
    FishBite,
    Hooked,
    Reeling,
    Success,
    Failed
}

public class FishingStateMachine
{
    public FishingState CurrentState { get; private set; } = FishingState.Idle;
    public FishingState PreviousState { get; private set; } = FishingState.Idle;
    public event Action<FishingState, FishingState> OnStateChanged;

    public bool ChangeState(FishingState newState)
    {
        if (CurrentState == newState) return false;
        PreviousState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(PreviousState, CurrentState);
        return true;
    }

    public void ResetToIdle() => ChangeState(FishingState.Idle);
}"""
    doc.add_code_block(code_fishingstate, "Mã nguồn: FishingState.cs & FishingStateMachine.cs")

    code_fishdata = """using UnityEngine;

public enum FishMovementType
{
    Mixed,
    Smooth,
    Sinker,
    Floater,
    Dart
}

[CreateAssetMenu(fileName = "NewFishData", menuName = "Farming/Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [SerializeField] private string fishId;
    [SerializeField] private string fishName;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemData rewardItemData;
    [SerializeField] private FishMovementType movementType;
    [SerializeField, Range(0.1f, 1f)] private float difficulty = 0.5f;
    [SerializeField] private int expReward = 25;

    public string FishName => fishName;
    public Sprite Icon => icon;
    public ItemData RewardItemData => rewardItemData;
    public FishMovementType MovementType => movementType;
    public float Difficulty => difficulty;
    public int ExpReward => expReward;
}"""
    doc.add_code_block(code_fishdata, "Mã nguồn: FishData.cs (Cấu hình loài cá)")

    doc.add_heading2("3.7.2. Thiết kế các chức năng xử lý và tương tác câu cá")
    doc.add_paragraph("Quy trình câu cá được hiện thực hóa qua 3 giai đoạn tương tác rõ rệt:")
    doc.add_bullet(" Nhấp chuột vào mặt nước để bắt đầu nạp lực (Charging). Thanh lực dao động tuần hoàn. Nhấp chuột lần 2 để chốt lực và quăng phao theo quỹ đạo parabol tới điểm tiếp nước.", "1. Giai đoạn tích lực và quăng phao (Charging & Casting):")
    doc.add_bullet(" Phao dập dềnh trên nước; sau một khoảng thời gian biteTimer ngẫu nhiên, phao bị kéo chìm và biểu tượng cá cắn câu xuất hiện. Người chơi có khoảng thời gian phản xạ (HookReactionWindow) để giật cần.", "2. Giai đoạn chờ cá cắn câu và giật cần (Bite & Hook):")
    doc.add_bullet(" Giao diện minigame xuất hiện. Người chơi di chuột để nâng/hạ thanh đỡ màu xanh bám theo vị trí cá. Khi cá nằm trong thanh đỡ, tiến độ tăng; khi ra ngoài, tiến độ giảm. Đạt 100% tiến độ: Câu thành công, nhận kinh nghiệm và vật phẩm cá được nạp vào kho đồ (nếu đầy sẽ rơi ra mặt đất).", "3. Giai đoạn Minigame kéo cá (Reeling & Reward):")

    code_fishingcontroller = """using System;
using System.Collections;
using UnityEngine;

public class FishingController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CastingController castingController;
    [SerializeField] private FishingMinigame minigame;
    [SerializeField] private Inventory inventory;
    [SerializeField] private ItemDropSpawner itemDropSpawner;
    [SerializeField] private FishingRodData defaultRodData;
    [SerializeField] private FishData fallbackFishData;

    public FishingStateMachine StateMachine { get; private set; }
    public FishingState CurrentState => StateMachine != null ? StateMachine.CurrentState : FishingState.Idle;
    public static FishingController Instance { get; private set; }

    private FishingRodData activeRodData;
    private FishData targetFish;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        StateMachine = new FishingStateMachine();
    }

    private void Start()
    {
        if (castingController != null)
            castingController.OnCastCompleted += HandleCastCompleted;
        if (minigame != null)
            minigame.OnMinigameFinished += HandleMinigameFinished;
    }

    public bool StartFishingTowards(Vector2 targetPos, FishingRodData rodData = null)
    {
        if (CurrentState != FishingState.Idle) return false;
        activeRodData = rodData != null ? rodData : defaultRodData;
        StateMachine.ChangeState(FishingState.Charging);
        castingController.StartCharging(activeRodData, targetPos - (Vector2)transform.position);
        return true;
    }

    public void ReleaseCast()
    {
        if (CurrentState != FishingState.Charging) return;
        StateMachine.ChangeState(FishingState.Casting);
        castingController.ReleaseCast((Vector2)transform.position, Vector2.down);
    }

    private void HandleCastCompleted(bool inWater, FishingSpotData spotData)
    {
        if (!inWater)
        {
            StateMachine.ChangeState(FishingState.Failed);
            castingController.RetrieveBobber();
            return;
        }

        targetFish = fallbackFishData;
        StartCoroutine(StartMinigameAfterDelay(1.0f));
    }

    private IEnumerator StartMinigameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StateMachine.ChangeState(FishingState.Reeling);
        minigame.StartMinigame(targetFish, activeRodData);
    }

    private void HandleMinigameFinished(FishingResult result)
    {
        castingController.RetrieveBobber();
        if (result.IsSuccess)
        {
            StateMachine.ChangeState(FishingState.Success);
            AwardRewards(result);
        }
        else
        {
            StateMachine.ChangeState(FishingState.Failed);
        }
        StartCoroutine(DelayedResetToIdle(2.0f));
    }

    private void AwardRewards(FishingResult result)
    {
        ItemData itemToGive = result.RewardItem ?? targetFish?.RewardItemData;
        if (itemToGive != null && inventory != null)
        {
            bool added = inventory.TryAddItem(itemToGive, result.Quantity);
            if (!added && itemDropSpawner != null)
            {
                itemDropSpawner.SpawnDrop(itemToGive, result.Quantity, transform.position);
            }
        }
    }

    private IEnumerator DelayedResetToIdle(float delay)
    {
        yield return new WaitForSeconds(delay);
        StateMachine.ResetToIdle();
    }
}"""
    doc.add_code_block(code_fishingcontroller, "Mã nguồn: FishingController.cs (Điều phối toàn bộ chu trình câu cá)")

    code_fishingminigame = """using System;
using UnityEngine;

public class FishingMinigame : MonoBehaviour
{
    [SerializeField] private FishingBarPhysics barPhysics;
    [SerializeField] private FishMovementController fishMovement;
    [SerializeField] private FishingProgress progress;
    [SerializeField] private FishingMinigameUI minigameUI;

    public bool IsActive { get; private set; } = false;
    public event Action<FishingResult> OnMinigameFinished;
    private FishData currentFish;

    public void StartMinigame(FishData fish, FishingRodData rod)
    {
        currentFish = fish;
        IsActive = true;
        gameObject.SetActive(true);

        barPhysics.Initialize(rod != null ? rod.BarSizeMultiplier : 1f);
        fishMovement.Initialize(fish);
        progress.Initialize(rod != null ? rod.ProgressGainMultiplier : 1f, 1f);
        if (minigameUI != null) minigameUI.Show();
    }

    private void Update()
    {
        if (!IsActive) return;
        float dt = Time.deltaTime;

        // 1. Cập nhật thanh đỡ theo vị trí chuột y
        float mouseNormY = Mathf.Clamp01(Input.mousePosition.y / Screen.height);
        barPhysics.UpdateMousePosition(mouseNormY, dt);

        // 2. Cập nhật AI bơi của cá
        fishMovement.UpdateMovement(dt);

        // 3. Kiểm tra va chạm thanh & cá
        bool isFishInside = barPhysics.IsFishInside(fishMovement.Position);

        // 4. Cập nhật thanh tiến độ bắt cá
        progress.UpdateProgress(isFishInside, dt);

        // 5. Cập nhật UI hiển thị
        if (minigameUI != null)
        {
            minigameUI.UpdateDisplay(barPhysics.Position, barPhysics.BarHeight, fishMovement.Position, progress.Value, isFishInside);
        }
    }

    public void CompleteGame(bool success, bool perfect)
    {
        IsActive = false;
        gameObject.SetActive(false);
        FishingResult result = success 
            ? new FishingResult(currentFish, true, perfect, currentFish.ExpReward, currentFish.RewardItemData, 1) 
            : FishingResult.CreateFailed();
        OnMinigameFinished?.Invoke(result);
    }
}"""
    doc.add_code_block(code_fishingminigame, "Mã nguồn: FishingMinigame.cs (Vòng lặp Minigame kéo cá)")

    doc.save()

if __name__ == "__main__":
    build_full_report()
