from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.enum.section import WD_SECTION
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.style import WD_STYLE_TYPE

OUT = r"D:\Game_Project\Totnghiep2026\Chuong3_ThietKe_3PhanHe_PlantUML.docx"

def shade(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr(); shd = tcPr.find(qn('w:shd'))
    if shd is None: shd = OxmlElement('w:shd'); tcPr.append(shd)
    shd.set(qn('w:fill'), fill)

def borders(cell, color='D9E2F3', sz='6'):
    tcPr = cell._tc.get_or_add_tcPr(); b = tcPr.first_child_found_in('w:tcBorders')
    if b is None: b = OxmlElement('w:tcBorders'); tcPr.append(b)
    for edge in ('top','left','bottom','right','insideH','insideV'):
        e = b.find(qn('w:'+edge))
        if e is None: e = OxmlElement('w:'+edge); b.append(e)
        e.set(qn('w:val'),'single'); e.set(qn('w:sz'),sz); e.set(qn('w:color'),color)

def set_cell(cell, text, bold=False, color='000000'):
    cell.text=''; p=cell.paragraphs[0]; p.paragraph_format.space_after=Pt(2)
    r=p.add_run(text); r.bold=bold; r.font.name='Times New Roman'; r.font.size=Pt(10.5); r.font.color.rgb=RGBColor.from_string(color)
    cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER; borders(cell)

def table(doc, headers, rows, widths=None):
    t=doc.add_table(rows=1, cols=len(headers)); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.style='Table Grid'
    for i,h in enumerate(headers): set_cell(t.rows[0].cells[i],h,True,'FFFFFF'); shade(t.rows[0].cells[i],'1F4E79')
    for ridx,row in enumerate(rows):
        cells=t.add_row().cells
        for i,v in enumerate(row): set_cell(cells[i],v); shade(cells[i], 'F3F6FA' if ridx%2 else 'FFFFFF')
    if widths:
        for row in t.rows:
            for i,w in enumerate(widths): row.cells[i].width=Inches(w)
    return t

def heading(doc, text, level=1):
    p=doc.add_paragraph(style=f'Heading {level}'); p.paragraph_format.keep_with_next=True; p.add_run(text); return p

def para(doc, text, bold_prefix=None):
    p=doc.add_paragraph(); p.paragraph_format.first_line_indent=Inches(0.3); p.paragraph_format.space_after=Pt(6); p.paragraph_format.line_spacing=1.15
    if bold_prefix:
        r=p.add_run(bold_prefix); r.bold=True; p.add_run(text)
    else: p.add_run(text)
    return p

def bullets(doc, items):
    for x in items:
        p=doc.add_paragraph(style='List Bullet'); p.paragraph_format.space_after=Pt(3); p.add_run(x)

def add_code_block(doc, text, title):
    q=doc.add_paragraph(); q.paragraph_format.space_before=Pt(6); q.paragraph_format.space_after=Pt(2)
    r=q.add_run(title); r.bold=True; r.font.color.rgb=RGBColor(31,78,121)
    p=doc.add_paragraph(); p.paragraph_format.left_indent=Inches(0.18); p.paragraph_format.right_indent=Inches(0.1); p.paragraph_format.space_after=Pt(8)
    rr=p.add_run(text.strip()); rr.font.name='Consolas'; rr.font.size=Pt(8.2); rr.font.color.rgb=RGBColor(40,40,40)

def subsystem(doc, title, ui, classes, sequence, notes):
    heading(doc,title,2)
    heading(doc,'1. Thiết kế giao diện và trải nghiệm UI/GD',3); para(doc,ui)
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('[Chèn ảnh gameplay/mockup thực tế tại đây]'); r.italic=True; r.font.color.rgb=RGBColor(100,100,100)
    heading(doc,'2. Thiết kế kiến trúc và biểu đồ lớp',3)
    table(doc,['Tầng kiến trúc','Các lớp thành phần','Trách nhiệm ở mức thiết kế'],classes,[1.3,2.0,3.6])
    para(doc,'Sơ đồ lớp cần sắp xếp theo chiều dọc: Tầng View ở trên, Tầng Control ở giữa và Tầng Thực thể/Data ở dưới. Chỉ thể hiện quan hệ và trách nhiệm chính, không liệt kê biến hoặc kiểu dữ liệu vụn vặt.')
    heading(doc,'3. Luồng xử lý chính và biểu đồ tuần tự',3); para(doc,sequence)
    table(doc,['Bước','Đối tượng gọi','Đối tượng được gọi','Kết quả trả về'],notes,[0.5,1.8,1.9,2.7])
    para(doc,'Khi vẽ biểu đồ tuần tự, mỗi mũi tên gọi phải có mũi tên nét đứt trả về tương ứng. Đối tượng được gọi phải có thanh kích hoạt trong thời gian thực thi hàm hoặc gọi tiếp đối tượng khác.')

doc=Document(); sec=doc.sections[0]; sec.top_margin=Inches(.7); sec.bottom_margin=Inches(.7); sec.left_margin=Inches(.85); sec.right_margin=Inches(.75)
styles=doc.styles; styles['Normal'].font.name='Times New Roman'; styles['Normal'].font.size=Pt(11)
for s in ('Heading 1','Heading 2','Heading 3'):
    styles[s].font.name='Times New Roman'; styles[s].font.color.rgb=RGBColor(31,78,121)
styles['Heading 1'].font.size=Pt(16); styles['Heading 2'].font.size=Pt(14); styles['Heading 3'].font.size=Pt(12)

p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('BÁO CÁO CHỈNH SỬA CHƯƠNG 3 VÀ CHƯƠNG 4'); r.bold=True; r.font.size=Pt(18); r.font.name='Times New Roman'; r.font.color.rgb=RGBColor(31,78,121)
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('Theo nhận xét của Thầy Mạnh Hùng và đối chiếu project Unity'); r.italic=True; r.font.size=Pt(12)
para(doc,'Bản chỉnh sửa này chuẩn hóa nội dung mô tả ba phân hệ theo cùng một trình tự: thiết kế giao diện và trải nghiệm, thiết kế kiến trúc bằng biểu đồ lớp, sau đó là luồng xử lý bằng biểu đồ tuần tự. Nội dung kỹ thuật được đối chiếu với các lớp đang có trong project, đồng thời loại bỏ cách trình bày liệt kê tham số mã nguồn khỏi phần thiết kế.')

heading(doc,'1. Quy tắc chỉnh sửa bắt buộc áp dụng cho toàn báo cáo',1)
table(doc,['Quy tắc','Cách áp dụng trong bản sửa'],[
('Phân chia công việc','Chuyển danh sách gạch đầu dòng thành bảng gồm Thành viên, Nhiệm vụ chính và Mô tả kỹ thuật UI/Backend.'),
('Công thức','Đặt số công thức bằng bảng không viền 1 hàng 2 cột hoặc tab phải; sửa “Net Currentcy” thành “Net Currency”.'),
('Cấu trúc chức năng','Luôn theo thứ tự: UI/GD → Class Diagram → Sequence Diagram.'),
('Biểu đồ lớp','Xếp từ trên xuống: View (UI, Tilemap, Animator) → Control (Manager, Controller) → Thực thể/Data.'),
('Biểu đồ tuần tự','Mỗi lời gọi có mũi tên trả về nét đứt; lớp được gọi có activation bar trong thời gian thực thi.'),
('Mức mô tả code','Không đưa bảng moveSpeed, float hoặc danh sách biến/kiểu dữ liệu vụn vặt; mô tả cơ chế và trách nhiệm.'),
('Chương 4','Chỉ trình bày kết quả chạy, giao diện hoàn thiện và kiểm thử; không chụp code/JSON và không lặp lại lý thuyết kiến trúc.')
],[1.5,5.4])

heading(doc,'2. Phân chia công việc',1)
table(doc,['Thành viên','Nhiệm vụ chính','Mô tả kỹ thuật UI/Backend'],[
('Thành viên 1','Quản lý nhân vật','UI: sprite 4 hướng, StaminaUI, chỉ báo vùng tương tác. Backend: PlayerMovement, PlayerStamina, FarmInputController và event cập nhật thể lực.'),
('Thành viên 2','Trồng trọt','UI: các lớp Tilemap và giai đoạn cây. Backend: SoilManager, FarmCell, CropData, CropTile và xử lý sự kiện onNewDay.'),
('Thành viên 3','Lưu trữ dữ liệu','UI: menu tạm dừng, Save/Load và thông báo. Backend: SaveManager, SaveData, PlayerData, FarmData, InventoryData và JsonUtility.')
],[1.2,1.7,4.0])

heading(doc,'3. Thiết kế ba phân hệ',1)
subsystem(doc,'3.1. Phân hệ quản lý nhân vật',
'Màn hình gameplay hiển thị nhân vật với sprite 4 hướng và các trạng thái Idle, Walk, Action. StaminaUI hiển thị thanh thể lực và số hiện tại/tối đa; màu thanh thay đổi theo mức năng lượng thông qua Gradient. FarmInputController và TileHighlighter cung cấp ô chỉ báo vùng tương tác phía trước nhân vật khi chọn công cụ.',
[('View','StaminaUI; Animator; SpriteRenderer; TileHighlighter','Hiển thị trạng thái nhân vật, thanh thể lực, hoạt ảnh và ô tương tác.'),('Control','PlayerMovement; PlayerStamina; FarmInputController','Nhận input, khóa di chuyển khi dùng công cụ, tiêu hao thể lực và phát event.'),('Thực thể/Data','PlayerData','Lưu vị trí, hướng nhìn và trạng thái thể lực để phục hồi phiên chơi.')],
'Luồng dùng công cụ: Player input → FarmInputController xác định công cụ và ô đích → PlayerMovement kiểm tra trạng thái và phát hoạt ảnh → khi hoạt ảnh hoàn tất, PlayerStamina trừ thể lực và phát event → StaminaUI nhận event, cập nhật thanh → trả kết quả hoàn tất hành động.',
[('1','PlayerInput','FarmInputController','Xác định công cụ và ô đích hợp lệ.'),('2','FarmInputController','PlayerMovement','Yêu cầu thực hiện hành động.'),('3','PlayerMovement','PlayerStamina','Trừ thể lực khi hoạt ảnh hoàn tất.'),('4','PlayerStamina','StaminaUI','Phát event giá trị thể lực mới.'),('5','StaminaUI','PlayerMovement','Trả kết quả cập nhật UI hoàn tất.')])

subsystem(doc,'3.2. Phân hệ trồng trọt',
'Giao diện gameplay minh họa các lớp Tilemap theo thứ tự nền cỏ, đất cày, đất đã tưới và cây trồng. CropTile thay đổi hình ảnh theo giai đoạn từ hạt giống, nảy mầm, trưởng thành đến sẵn sàng thu hoạch; khi không được tưới đủ ngày, cây chuyển sang trạng thái héo. FarmInputController hiển thị ô đích và nhận thao tác cuốc, gieo, tưới, bón phân, thu hoạch.',
[('View','Tilemap groundTileMap; fertilizerTileMap; CropTile; TileHighlighter','Hiển thị đất, trạng thái tưới/bón phân và các giai đoạn cây.'),('Control','SoilManager; FarmInputController; TimeManager','Điều phối thao tác canh tác, lắng nghe onNewDay và cập nhật trạng thái.'),('Thực thể/Data','CropData; FarmCell; FarmCellState; FarmData','Lưu cấu hình cây và trạng thái từng ô theo tọa độ.')],
'Ba biểu đồ tuần tự cần sửa theo cùng nguyên tắc: Dùng công cụ: FarmInputController → PlayerMovement → SoilManager → FarmCell/CropTile → trả kết quả về theo từng lời gọi. Chuyển ngày mới: TimeManager phát onNewDay → SoilManager xử lý từng FarmCell → CropTile kiểm tra tưới và tăng trưởng → SoilManager cập nhật Tilemap → trả kết quả hoàn tất. Thu hoạch: FarmInputController kiểm tra ô → SoilManager xác nhận ReadyToHarvest → CropTile trả vật phẩm → Inventory nhận vật phẩm → giao diện cập nhật.',
[('1','FarmInputController','SoilManager','Yêu cầu thao tác trên ô.'),('2','SoilManager','FarmCell','Kiểm tra/chuyển trạng thái ô.'),('3','SoilManager','CropTile','Cập nhật hình ảnh hoặc tăng trưởng.'),('4','CropTile','SoilManager','Trả trạng thái cây.'),('5','SoilManager','FarmInputController','Trả kết quả thao tác.')])

subsystem(doc,'3.3. Phân hệ lưu trữ dữ liệu',
'Menu tạm dừng cần có nút Save và Load cùng vùng thông báo kết quả. Khi người chơi ngủ qua ngày mới, hệ thống tự động lưu và có thể hiển thị thông báo tiến trình đã được lưu. Nội dung minh họa Chương 4 phải là ảnh game đang chạy hoặc bảng dữ liệu tóm tắt, không dùng ảnh file JSON.',
[('View','UI menu Save/Load; thông báo tiến trình; giao diện thời gian','Hiển thị thao tác lưu/tải và phản hồi thành công hoặc lỗi.'),('Control','SaveManager; TimeManager; Inventory; SoilManager','Thu thập dữ liệu, tuần tự hóa/giải tuần tự hóa và khôi phục trạng thái game.'),('Thực thể/Data','SaveData; PlayerData; FarmData; InventoryData; GameTimeData','Các lớp dữ liệu tuần tự hóa phục vụ lưu vị trí, thể lực, nông trại, kho đồ và thời gian.')],
'Luồng lưu: SaveLoadMenuUI → SaveManager.SaveGame() → thu thập dữ liệu từ PlayerMovement/PlayerStamina, Inventory, TimeManager và SoilManager → JsonUtility tạo nội dung lưu → ghi xuống ổ đĩa → trả kết quả true → UI hiển thị thông báo. Khi ngủ qua ngày, TimeManager gọi SaveManager.SaveGame() theo cùng quy tắc đóng gói luồng.',
[('1','SaveLoadMenuUI','SaveManager','Yêu cầu lưu game.'),('2','SaveManager','PlayerMovement/PlayerStamina','Lấy dữ liệu nhân vật.'),('3','SaveManager','Inventory/SoilManager/TimeManager','Lấy dữ liệu kho, nông trại và thời gian.'),('4','SaveManager','JsonUtility/File','Tuần tự hóa và ghi dữ liệu.'),('5','SaveManager','SaveLoadMenuUI','Trả kết quả để hiển thị thông báo.')])

heading(doc,'3.4. Mã PlantUML dùng để vẽ các biểu đồ',1)
para(doc,'Các đoạn mã dưới đây có thể sao chép trực tiếp vào PlantUML. Biểu đồ lớp dùng thứ tự View → Control → Thực thể/Data. Biểu đồ tuần tự dùng activate/deactivate để thể hiện thanh kích hoạt và mũi tên nét đứt để thể hiện kết quả trả về.')
add_code_block(doc, r'''@startuml
top to bottom direction
package "View" { class StaminaUI; class Animator; class TileHighlighter }
package "Control" { class FarmInputController; class PlayerMovement; class PlayerStamina }
package "Thực thể / Data" { class PlayerData }
FarmInputController --> PlayerMovement : gọi hành động
PlayerMovement --> Animator : phát hoạt ảnh
PlayerMovement --> PlayerStamina : ConsumeStamina()
PlayerStamina ..> StaminaUI : onStaminaChanged
PlayerMovement ..> PlayerData : dữ liệu lưu
@enduml''','3.4.1. Class Diagram - Quản lý nhân vật')
add_code_block(doc, r'''@startuml
actor Player
participant FarmInputController as Input
participant PlayerMovement as Move
participant PlayerStamina as Stamina
participant StaminaUI as UI
Player -> Input : chọn công cụ
activate Input
Input -> Move : TryUseHoe()/UsingWater()
activate Move
Move --> Input : animation hoàn tất
deactivate Move
Input -> Stamina : ConsumeStamina()
activate Stamina
Stamina ->> UI : onStaminaChanged()
activate UI
UI --> Stamina : cập nhật thanh xong
deactivate UI
Stamina --> Input : kết quả trừ thể lực
deactivate Stamina
Input --> Player : phản hồi hành động
deactivate Input
@enduml''','3.4.2. Sequence Diagram - Dùng công cụ')
add_code_block(doc, r'''@startuml
top to bottom direction
package "View" { class GroundTilemap <<Tilemap>>; class CropTile; class TileHighlighter }
package "Control" { class FarmInputController; class SoilManager; class TimeManager }
package "Thực thể / Data" { class FarmData; class FarmCell; enum FarmCellState; class CropData <<ScriptableObject>> }
FarmInputController --> SoilManager : Tilled/Water/Plant/Harvest
SoilManager --> GroundTilemap : cập nhật tile
SoilManager --> FarmData
FarmData o-- FarmCell
FarmCell --> FarmCellState
SoilManager --> CropTile : Check/Grow
CropTile --> CropData
TimeManager ..> SoilManager : onNewDay
@enduml''','3.4.3. Class Diagram - Trồng trọt')
add_code_block(doc, r'''@startuml
participant TimeManager as Time
participant SoilManager as Soil
participant FarmCell as Cell
participant CropTile as Crop
Time -> Soil : onNewDay
activate Soil
loop từng FarmCell
 Soil -> Cell : đọc trạng thái
 activate Cell
 Cell --> Soil : dữ liệu ô
 deactivate Cell
 Soil -> Crop : Check()/Grow()
 activate Crop
 Crop --> Soil : stage/héo/thu hoạch
 deactivate Crop
end
Soil --> Time : hoàn tất chuyển ngày
deactivate Soil
@enduml''','3.4.4. Sequence Diagram - Chuyển ngày onNewDay')
add_code_block(doc, r'''@startuml
actor Player
participant FarmInputController as Input
participant SoilManager as Soil
participant FarmCell as Cell
participant CropTile as Crop
participant Inventory
Player -> Input : dùng Sickle
activate Input
Input -> Soil : yêu cầu thu hoạch
activate Soil
Soil -> Cell : kiểm tra ReadyToHarvest
activate Cell
Cell --> Soil : hợp lệ
deactivate Cell
Soil -> Crop : lấy phần thưởng
activate Crop
Crop --> Soil : ItemData/số lượng
deactivate Crop
Soil -> Inventory : AddItem()
activate Inventory
Inventory --> Soil : thành công/còn dư
deactivate Inventory
Soil --> Input : kết quả thu hoạch
deactivate Soil
Input --> Player : cập nhật cây và kho
deactivate Input
@enduml''','3.4.5. Sequence Diagram - Thu hoạch')
add_code_block(doc, r'''@startuml
actor Player
participant SaveLoadMenuUI as UI
participant SaveManager as Save
participant PlayerMovement as Move
participant Inventory
participant SoilManager as Soil
participant TimeManager as Time
participant JsonUtility
participant FileSystem as File
Player -> UI : bấm Save
activate UI
UI -> Save : SaveGame()
activate Save
Save -> Move : lấy PlayerData
activate Move
Move --> Save : dữ liệu nhân vật
deactivate Move
Save -> Inventory : lấy InventoryData
activate Inventory
Inventory --> Save : dữ liệu kho
deactivate Inventory
Save -> Soil : lấy FarmData
activate Soil
Soil --> Save : dữ liệu nông trại
deactivate Soil
Save -> Time : lấy GameTimeData
activate Time
Time --> Save : dữ liệu thời gian
deactivate Time
Save -> JsonUtility : ToJson(SaveData)
activate JsonUtility
JsonUtility --> Save : chuỗi JSON
deactivate JsonUtility
Save -> File : WriteAllText()
activate File
File --> Save : ghi thành công
deactivate File
Save --> UI : true
deactivate Save
UI --> Player : hiển thị đã lưu
deactivate UI
@enduml''','3.4.6. Sequence Diagram - Lưu game')
add_code_block(doc, r'''@startuml
top to bottom direction
package "View" { class SaveLoadMenuUI; class PauseMenu; class SaveFeedbackUI }
package "Control" { class SaveManager; class TimeManager; class Inventory; class SoilManager }
package "Thực thể / Data" { class SaveData; class PlayerData; class FarmData; class InventoryData; class GameTimeData }
SaveLoadMenuUI --> SaveManager : SaveGame()/LoadGame()
SaveManager --> PlayerData
SaveManager --> FarmData
SaveManager --> InventoryData
SaveManager --> GameTimeData
SaveData *-- PlayerData
SaveData *-- FarmData
SaveData *-- InventoryData
SaveData *-- GameTimeData
TimeManager ..> SaveManager : tự động lưu
SaveManager --> Inventory
SaveManager --> SoilManager
SaveLoadMenuUI ..> SaveFeedbackUI
@enduml''','3.4.7. Class Diagram - Lưu trữ dữ liệu')

heading(doc,'4. Kết quả thực nghiệm và kiểm thử',1)
para(doc,'Chương này chỉ trình bày kết quả chạy thực tế, giao diện hoàn thiện và kiểm thử. Các nội dung giải thích kiến trúc, danh sách biến và ảnh mã nguồn được chuyển ra khỏi chương này hoặc loại bỏ.')
table(doc,['Mã kiểm thử','Kịch bản','Kết quả mong đợi','Minh chứng cần chèn'],[
('TC01','Di chuyển và dùng công cụ','Nhân vật đổi hoạt ảnh, khóa di chuyển trong hành động, thể lực giảm và thanh UI cập nhật.','Ảnh gameplay trước/sau thao tác.'),
('TC02','Cuốc, gieo, tưới và thu hoạch','Ô đất chuyển trạng thái; cây tăng trưởng theo ngày; thu hoạch đưa vật phẩm vào kho.','Ảnh Tilemap và cây ở các giai đoạn.'),
('TC03','Không tưới đủ ngày','Cây chuyển trạng thái héo, Tilemap phản ánh trạng thái.','Ảnh gameplay trạng thái héo.'),
('TC04','Lưu và tải game','Vị trí, thể lực, thời gian, kho đồ và dữ liệu nông trại được khôi phục.','Ảnh menu Save/Load và gameplay sau Load.'),
('TC05','Ngủ qua ngày mới','Thời gian chuyển ngày, thể lực được xử lý và game tự động lưu.','Ảnh giao diện buổi sáng/thông báo lưu.')
],[0.7,1.7,2.6,2.0])
heading(doc,'5. Checklist trước khi nộp',1)
bullets(doc,['Mọi phân hệ đều theo đúng thứ tự UI/GD → Class Diagram → Sequence Diagram.','Biểu đồ lớp xếp View → Control → Thực thể/Data từ trên xuống dưới.','Biểu đồ tuần tự có mũi tên trả về nét đứt và activation bar cho lớp được gọi.','Không còn ảnh code hoặc JSON trong Chương 4.','Không còn bảng liệt kê biến/kiểu dữ liệu vụn vặt; thuật ngữ “Net Currency” đã được chuẩn hóa.','Ảnh minh họa là ảnh game chạy thực tế, có chú thích và đánh số hình thống nhất.'])

doc.save(OUT); print(OUT)
