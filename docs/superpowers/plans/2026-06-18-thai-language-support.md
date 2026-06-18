# 泰语界面支持 — 执行计划

## 架构概览

在现有 7 语言基础上新增泰语（ไทย / th-TH）。仅涉及两个文件：本地化 JSON + 更新日志。`LocalizationService` 的现有架构天然支持新增语言（从目录扫描 *.json），无需任何代码改动。

### 涉及文件

| 文件 | 操作 | 说明 |
|------|------|------|
| `Resources/Locales/th-TH.json` | **新建** | 泰语翻译文件，翻译全部 80+ 个 key |
| `Views/AboutWindow.xaml.cs` | 修改 | 更新日志 |

---

## 切片 1: 新建泰语翻译文件

### Task 1.1: 创建 th-TH.json

**文件**: `d:\Project\Project-C#\CassieWordCheck\Resources\Locales\th-TH.json`

```json
{
  "_th-TH": "ไทย",
  "app.title": "CASSIE ตัวตรวจสอบคำ",
  "settings.title": "การตั้งค่า",
  "whitelist.title": "จัดการบัญชีขาว",
  "input.placeholder": "พิมพ์ข้อความประกาศ CASSIE ของคุณที่นี่...",
  "input.label": "ข้อความประกาศ",
  "result.label": "ผลการตรวจสอบ",
  "result.available": "ใช้ได้",
  "result.unavailable": "ใช้ไม่ได้",
  "result.coverage": "ความครอบคลุม",
  "result.total": "จำนวนคำทั้งหมด",
  "result.ignored": "ละเว้น",
  "stats.available": "ใช้ได้: {0}",
  "stats.unavailable": "ใช้ไม่ได้: {0}",
  "stats.ignored": "ละเว้น: {0}",
  "stats.coverage": "ความครอบคลุม: {0:F1}%",
  "stats.chars": "อักขระ: {0}",
  "settings.ignore_chinese": "ละเว้นอักขระจีน",
  "settings.ignore_angle": "ละเว้นเนื้อหาใน &lt;&gt;",
  "settings.wordlist_path": "ไฟล์รายการคำศัพท์",
  "settings.browse": "เรียกดู...",
  "settings.theme": "ธีม",
  "settings.reset": "รีเซ็ตค่าเริ่มต้น",
  "settings.close": "ปิด",
  "settings.language": "ภาษา",
  "whitelist.add": "เพิ่ม",
  "whitelist.remove": "ลบ",
  "whitelist.input_hint": "พิมพ์คำที่ต้องการเพิ่ม...",
  "whitelist.empty": "ไม่มีคำในบัญชีขาว",
  "whitelist.import": "นำเข้าจากไฟล์",
  "whitelist.export": "ส่งออกไปยังไฟล์",
  "whitelist.clear": "ล้างทั้งหมด",
  "whitelist.import_done": "นำเข้า {0} คำแล้ว",
  "whitelist.export_done": "ส่งออก {0} คำแล้ว",
  "whitelist.confirm_clear": "แน่ใจหรือว่าต้องการล้างบัญชีขาว?",
  "status.ready": "พร้อม",
  "status.loaded": "โหลด {0} คำจาก {1}",
  "status.load_failed": "โหลดรายการคำศัพท์ล้มเหลว: {0}",
  "status.words": "รายการคำศัพท์: {0} คำ",
  "label.dark": "มืด",
  "label.light": "สว่าง",
  "label.system": "ระบบ",
  "suggestion.title": "คำแนะนำ",
  "suggestion.replace": "แทนที่",
  "suggestion.no_suggestions": "ไม่มีคำแนะนำ",
  "suggestion.no_match": "ไม่พบคำใกล้เคียงในพจนานุกรม",
  "suggestion.wildcard": "การจับคู่ไวลด์การ์ด",
  "suggestion.fuzzy": "การสะกดใกล้เคียง",
  "suggestion.compound": "แยกคำประสม",
  "menu.copy_result": "คัดลอกผลลัพธ์",
  "menu.copy_broadcast": "คัดลอกข้อความประกาศ",
  "menu.reload_wordlist": "โหลดรายการคำศัพท์ใหม่",
  "menu.statistics": "สถิติ",
  "menu.import_words": "นำเข้าคำศัพท์",
  "menu.about": "เกี่ยวกับ",
  "menu.history": "ประวัติการตรวจสอบ",
  "stats.title": "แผงสถิติ",
  "stats.coverage_trend": "แนวโน้มความครอบคลุม",
  "stats.unavailable_trend": "แนวโน้มคำที่ใช้ไม่ได้",
  "stats.no_data": "ไม่มีข้อมูลประวัติ",
  "stats.all_time": "ทั้งหมด",
  "stats.latest": "การตรวจสอบล่าสุด",
  "import.title": "นำเข้าคำศัพท์",
  "import.txt": "ไฟล์ข้อความ (*.txt)",
  "import.csv": "ไฟล์ CSV (*.csv)",
  "import.excel": "ไฟล์ Excel (*.xlsx)",
  "import.done": "นำเข้า {0} คำแล้ว",
  "import.supplement": "รายการคำศัพท์เพิ่มเติม",
  "import.supported": "ทุกรูปแบบที่รองรับ",
  "update.check": "ตรวจสอบอัปเดต",
  "update.available": "มีเวอร์ชันใหม่!",
  "update.current": "เป็นเวอร์ชันล่าสุดแล้ว",
  "update.download": "ดาวน์โหลด",
  "update.error": "ตรวจสอบอัปเดตล้มเหลว",
  "update.new_version": "v{0} พร้อมใช้งาน (ปัจจุบัน: v{1})",
  "mode.inline": "แบบอินไลน์",
  "mode.list": "มุมมองรายการ",
  "mode.compare": "เปรียบเทียบ",
  "detail.frequency": "ปรากฏ {0} ครั้ง",
  "detail.suggestions": "คำแนะนำการสะกด",
  "wordlist_browser.tooltip": "เบราว์เซอร์รายการคำศัพท์"
}
```

**验证**: 无测试需要，确认 JSON 格式合法即可：
```bash
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
```

---

## 切片 2: 更新日志

### Task 2.1: 更新 Changelog

**文件**: `d:\Project\Project-C#\CassieWordCheck\Views\AboutWindow.xaml.cs`

在 `ChangelogText` 顶部插入：

```markdown
## `v2.3.5`（2026-06-18）— 泰语界面支持

- **新增泰语界面** — ไทย（th-TH），现有 7 语言扩展至 8 语言
```

**验证**:
```bash
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
```

---

## 验证

```bash
dotnet build d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.csproj
dotnet test d:\Project\Project-C#\CassieWordCheck\CassieWordCheck.Tests\CassieWordCheck.Tests.csproj
```

预期：0 错误，106/106 测试通过（无新增测试，因为 LocalizationService 的现有测试覆盖自动发现新语言文件）。
