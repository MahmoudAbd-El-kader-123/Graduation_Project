# شرح برانش `001-invoice-processing-pipeline`

> الغرض من الملف ده: يبقى مرجع سريع ومفصل تشرحه في المقابلة عن كل اللي اتعمل في البرانش: إيه اتضاف، إيه اتعدل، الـ flow شغال إزاي، الـ background job بتعمل إيه، الفاتورة بتيجي من الـ frontend إزاي، إزاي بتتخزن وتتبعت للـ AI، وإزاي بتحصل reconciliation مع الـ Purchase Order.

---

## 1. ملخص سريع جدا للبرانش

البرانش ده نفذ ميزة اسمها **Intelligent Invoice Processing Pipeline** داخل Backend مشروع SPIP.

الفكرة الأساسية:

1. المستخدم من الـ frontend يرفع invoice file مع `PurchaseOrderId`.
2. الـ backend يتحقق من صلاحيات المستخدم.
3. يتحقق من الملف من ناحية:
   - الامتداد.
   - MIME type.
   - حجم الملف.
   - file signature أو magic number.
4. يخزن الملف بشكل آمن باسم GUID بدل الاسم الأصلي.
5. ينشئ records في قاعدة البيانات:
   - `Invoice`
   - `UploadedFile`
   - `InvoiceProcessingLog`
6. يحط الفاتورة في queue باستخدام Hangfire.
7. الـ background job تبدأ processing من غير ما الـ frontend يستنى.
8. الـ job تبعت الملف لـ AI service عشان تستخرج بيانات الفاتورة.
9. الـ backend يستقبل JSON من الـ AI ويتحقق منه.
10. البيانات المستخرجة تتحول لـ domain model وتتخزن.
11. يحصل reconciliation بين invoice items و purchase order items باستخدام `SupplierSku`.
12. أي فروقات تتحول لـ `Discrepancy` records.
13. حالة الفاتورة تتحدث طول الطريق من `Uploaded` لحد `Completed` أو `Failed` أو `NeedsReview`.

البرانش كمان أضاف documentation/spec artifacts باستخدام SpecKit، ودي كانت لتوثيق المتطلبات والخطة والـ tasks قبل التنفيذ.

---

## 2. نطاق التغيير في البرانش

البرانش مبني فوق `Sprint_two`، والتغييرات الأساسية كانت:

- إضافة invoice upload API.
- إضافة secure local file storage.
- إضافة invoice processing background job باستخدام Hangfire.
- إضافة AI extraction integration.
- إضافة typed DTOs لرد الـ AI بدل raw string.
- إضافة reconciliation logic بين invoice و PO.
- إضافة discrepancy tracking.
- إضافة processing history لكل invoice.
- إضافة permissions جديدة للفواتير.
- إضافة EF migration جديدة باسم `AddInvoicePipeline`.
- إضافة repositories و services و interfaces جديدة حسب Clean Architecture.
- إضافة SpecKit workflow/docs/specs/tasks.

إجمالي التغيير كان كبير: حوالي 96 ملف، معظمها ملفات جديدة للميزة والـ specs.

---

## 3. الملفات الجديدة والمعدلة حسب الطبقات

### API Layer

#### `SPIP.API/Controllers/InvoicesController.cs`

ده controller جديد للفواتير.

المسؤوليات:

- استقبال upload request من الـ frontend.
- توفير endpoint لعرض تفاصيل invoice.
- توفير endpoint لتحميل الملف الأصلي.
- توفير endpoint للـ admin listing.

Endpoints:

| Endpoint | Method | Purpose | Permission |
|---|---:|---|---|
| `api/invoices/upload` | POST | رفع invoice وتحويلها لـ queue | `Invoices.Upload` |
| `api/invoices/{id}` | GET | عرض تفاصيل invoice والـ logs والـ discrepancies | `Invoices.View` |
| `api/invoices/{id}/download` | GET | تحميل الملف الأصلي بشكل آمن | `Invoices.Download` |
| `api/invoices` | GET | عرض كل الفواتير للـ admin مع filter/pagination | `Invoices.ViewAll` |

الـ controller نفسه thin controller، يعني مفيهوش business logic. هو بينادي `IInvoiceService` ويرجع `ApiResponse<T>`.

#### `SPIP.API/Program.cs`

اتعدل عشان يسجل Hangfire:

- `AddHangfire(...)`
- `AddHangfireServer()`
- `MapHangfireDashboard("/hangfire")`

واتعمل dashboard للـ Hangfire على:

```text
/hangfire
```

ومحمي بصلاحية:

```text
Invoices.ViewAll
```

يعني مش أي مستخدم يقدر يشوف dashboard بتاع jobs.

#### `SPIP.API/SPIP.API.csproj`

اتضافت packages:

- `Hangfire`
- `Hangfire.SqlServer`

وده لأن البرانش استخدم Hangfire كـ background job engine و SQL Server كـ storage للـ jobs.

---

## 4. Application Layer

الـ Application layer اتوسع بعقود و DTOs و validators و mapping profiles. الفكرة هنا إن الـ API والـ Infrastructure ما يتكلموش مع بعض بشكل مباشر في business contract، لكن كله يعدي على interfaces و DTOs واضحة.

### DTOs الخاصة بالفواتير

اتضافت ملفات تحت:

```text
SPIP.Application/DTOs/Invoice/
```

#### `UploadInvoiceRequest.cs`

ده request اللي بييجي من الـ frontend في upload.

بيحتوي على:

- `IFormFile File`
- `int PurchaseOrderId`

يعني الـ frontend لازم يبعت multipart/form-data فيه الملف ورقم الـ PO اللي هنعمل reconciliation عليه.

#### `InvoiceUploadResultDto.cs`

ده الرد بعد upload ناجح.

بيرجع:

- `InvoiceId`
- `Status`
- `FileName`

مثال:

```json
{
  "invoiceId": 42,
  "status": "Queued",
  "fileName": "invoice.pdf"
}
```

النقطة المهمة: الرد بيرجع `Queued` مش `Completed`، لأن المعالجة بتحصل asynchronous في background job.

#### `InvoiceDetailDto.cs`

ده DTO تفصيلي للـ invoice.

بيشمل:

- بيانات الفاتورة الأساسية.
- line items المستخرجة من الـ AI.
- discrepancies الناتجة من المقارنة.
- processing logs.

ده اللي يخلي الـ frontend يقدر يعرض status page كاملة للمستخدم.

#### `InvoiceListItemDto.cs`

ده DTO مختصر للـ admin listing.

فيه:

- invoice number.
- vendor name.
- status.
- total amount.
- upload date.
- email المستخدم اللي رفع.
- آخر error لو حصل فشل.

#### `InvoiceListParameters.cs`

بيمثل query parameters للـ listing:

- `Status`
- `PageNumber`
- `PageSize`

وفي service بيتم ضبط page size بحد أقصى 50.

#### `InvoiceItemDto.cs`

يمثل line item مستخرج من الفاتورة:

- `SupplierSku`
- `Description`
- `Quantity`
- `UnitPrice`
- `LineTotal`

#### `DiscrepancyDto.cs`

يمثل mismatch واحد:

- نوع الفرق.
- اسم الحقل.
- القيمة المتوقعة.
- القيمة الفعلية.
- هل اتحل ولا لأ.

#### `InvoiceProcessingLogDto.cs`

يمثل event في رحلة الفاتورة:

- `FromStatus`
- `ToStatus`
- `EventType`
- `Message`
- `Timestamp`

ده مهم جدا في المقابلة لأنك تقدر تقول إن كل transition auditable ومش مجرد status أخير.

### DTOs الخاصة بالـ AI

اتضافت تحت:

```text
SPIP.Application/DTOs/AI/
```

#### `AIExtractionResponseDto.cs`

ده شكل رد الـ AI المتوقع:

- `VendorName`
- `InvoiceNumber`
- `InvoiceDate`
- `Currency`
- `Subtotal`
- `Vat`
- `Total`
- `Items`

#### `AIExtractionItemDto.cs`

ده شكل item جاي من الـ AI:

- `SupplierSku`
- `Description`
- `Quantity`
- `UnitPrice`
- `Amount`

القرار المهم هنا إن `IAIExtractionService` بقى بيرجع typed DTO بدل string. ده قلل parsing عشوائي وخلّى الـ validation والمapping أوضح.

### Validators

#### `UploadInvoiceRequestValidator.cs`

ده FluentValidation validator للـ upload request.

بيتحقق من:

- الملف موجود.
- الملف مش فاضي.
- الحجم لا يتجاوز 10MB.
- الامتداد من الأنواع المسموحة:
  - `.pdf`
  - `.jpg`
  - `.jpeg`
  - `.png`
- الـ content type من:
  - `application/pdf`
  - `image/jpeg`
  - `image/png`
- `PurchaseOrderId` أكبر من صفر.

ده أول خط دفاع قبل service logic.

### Helper

#### `FileValidationHelper.cs`

ده helper مهم جدا للأمان.

مش بيكتفي بالامتداد ولا MIME type، لكنه بيقرأ أول bytes من الملف عشان يتأكد من الـ magic number:

- PDF لازم يبدأ بـ `%PDF`
- JPG/JPEG لازم يبدأ بـ `FF D8`
- PNG لازم يبدأ بـ `89 50 4E 47`

ليه ده مهم؟

لأن المستخدم ممكن يرفع `.exe` ويغير اسمه لـ `.pdf`. الـ extension هيعدي، لكن file signature هيفشل، وبالتالي الـ backend يرفض الملف قبل التخزين.

### Interfaces

اتضافت عقود جديدة:

#### `IInvoiceService`

الواجهة الرئيسية اللي controller بيتعامل معاها.

بتوفر:

- `UploadInvoiceAsync`
- `GetByIdAsync`
- `DownloadFileAsync`
- `GetAllPagedAsync`

#### `IInvoiceProcessingService`

الـ orchestration service بتاع background pipeline.

فيه:

```csharp
Task ProcessInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
```

#### `IReconciliationService`

مسؤول عن مقارنة invoice مع PO.

#### `IAIExtractionService`

اتعدل عشان يرجع:

```csharp
AIExtractionResponseDto
```

بدل raw string.

#### `IFileStorageService`

اتوسع بإضافة:

```csharp
SaveFileWithGuidAsync(...)
```

الmethod دي بترجع:

- `StoredPath`
- `StoredFileName`

وده عشان نحتفظ بالاسم الأصلي للمستخدم، وفي نفس الوقت نخزن على الديسك باسم آمن unique.

#### Repository interfaces

اتضاف:

- `IInvoiceRepository`
- `IInvoiceProcessingLogRepository`

وده بيخلي Infrastructure مسؤولة عن EF implementation، بينما Application عارفة بس contract.

### Mapping

#### `InvoiceMappingProfile.cs`

AutoMapper profile جديد بيحول:

- `Invoice` إلى `InvoiceDto`
- `Invoice` إلى `InvoiceDetailDto`
- `Invoice` إلى `InvoiceListItemDto`
- `InvoiceItem` إلى `InvoiceItemDto`
- `Discrepancy` إلى `DiscrepancyDto`
- `InvoiceProcessingLog` إلى `InvoiceProcessingLogDto`

وفيه mapping مهم:

- `Status` بيتحول من enum إلى string.
- `UploadedAt` بييجي من `CreatedAt`.
- `Timestamp` في logs بييجي من `CreatedAt`.
- `UploadedByUserEmail` بييجي من navigation property.

---

## 5. Domain Layer

الـ Domain هو قلب الموديل. هنا اتغيرت entities و enums عشان تدعم lifecycle كامل للفواتير.

### `InvoiceStatus.cs`

اتغيرت statuses القديمة إلى lifecycle جديد:

| Status | معناها |
|---|---|
| `Uploaded` | الفاتورة اترفعت واتخزنت |
| `Queued` | اتحطت في Hangfire queue |
| `Processing` | الـ background job بدأت تشتغل |
| `Extracted` | الـ AI رجع structured data |
| `Validated` | البيانات اتراجعت ونجحت business validation |
| `Compared` | بدأ/تم جزء المقارنة مع PO |
| `Completed` | المعالجة خلصت |
| `Failed` | technical failure حصل |
| `NeedsReview` | مشكلة business validation محتاجة تدخل بشري |

### `DiscrepancyType.cs`

enum جديد لأن discrepancies بقت typed بدل مجرد نص.

الأنواع:

- `MissingSku`: SKU موجود في الفاتورة لكن مش موجود في الـ PO.
- `MissingFromInvoice`: item موجود في الـ PO لكن مش موجود في الفاتورة.
- `QuantityMismatch`: الكمية مختلفة.
- `UnitPriceMismatch`: السعر مختلف.
- `AmountMismatch`: إجمالي الفاتورة لا يساوي مجموع line totals.

### `Invoice.cs`

اتضاف للـ Invoice:

- `VendorName`
- `Currency`
- `Subtotal`
- `Vat`
- `UploadedByUserId`
- `UploadedByUser`
- `ProcessingLogs`

واتغير:

- default status بقى `Uploaded`.
- `PurchaseOrderId` بقى required `int` بدل nullable، لأن الرفع لازم يكون مرتبط بـ PO.

التأثير:

- كل invoice بقت مرتبطة بمستخدم رفعها.
- ممكن نطبق ownership authorization.
- ممكن نعرض history كاملة.
- بيانات AI المستخرجة عندها مكان واضح في invoice header.

### `InvoiceItem.cs`

اتضاف:

```csharp
SupplierSku
```

وده field محوري جدا لأن reconciliation بتتم عليه.

الفكرة إن الـ AI يستخرج supplier SKU من الفاتورة، وبعدين backend يقارنه مع `Product.Sku` في PO items.

### `Discrepancy.cs`

اتضاف:

- `DiscrepancyType`
- `InvoiceItemId`
- `InvoiceItem`

التأثير:

- كل discrepancy بقت typed.
- ممكن نربط discrepancy بـ invoice item محدد.
- في حالات زي `MissingFromInvoice` مفيش invoice item، فـ `InvoiceItemId` nullable.

### `UploadedFile.cs`

اتضاف:

- `OriginalFileName`
- `StoredFileName`

الفرق:

- `OriginalFileName`: الاسم اللي المستخدم رفعه.
- `StoredFileName`: GUID filename على السيرفر.

ده يحل مشكلتين:

- أمان: ما نخزنش مباشرة باسم المستخدم.
- UX: لما المستخدم يحمل الملف يرجعله باسمه الأصلي.

### `InvoiceProcessingLog.cs`

entity جديد للتاريخ التشغيلي.

كل log فيه:

- `InvoiceId`
- `FromStatus`
- `ToStatus`
- `EventType`
- `Message`
- timestamps من `BaseEntity`

مثال:

```text
null -> Uploaded: Invoice uploaded successfully.
Uploaded -> Queued: Invoice queued for processing.
Queued -> Processing: Invoice processing started.
Processing -> Extracted: AI extraction completed.
Extracted -> Validated: Invoice data validated.
Compared -> Completed: Invoice reconciliation completed with 2 discrepancies.
```

التأثير:

- traceability.
- debugging أسهل.
- admin يقدر يعرف الفاتورة وقفت فين.
- المقابلة: تقدر تقول "ما اعتمدناش على status field بس، عملنا audit trail لكل transition".

### `Permissions.cs`

اتضاف permission group جديد:

```text
Invoices.Upload
Invoices.View
Invoices.Download
Invoices.ViewAll
```

وده متسجل dynamic داخل policies، ومتسجل كمان في migration داخل `PermissionCatalogs`.

---

## 6. Infrastructure Layer

هنا التنفيذ الحقيقي للـ services والـ repositories والـ EF configuration والـ Hangfire job.

### `InvoiceService.cs`

ده service المستخدم مباشرة من الـ controller.

#### `UploadInvoiceAsync`

دي أهم method في بداية الـ flow.

الخطوات:

1. يجيب current identity id من `ICurrentUserService`.
2. لو مفيش user يرجع `User not authenticated`.
3. يجيب domain user من `IUserRepository.GetByIdentityIdAsync`.
4. يتأكد إن الـ Purchase Order موجود.
5. يفتح stream من الملف.
6. يعمل file signature validation باستخدام `FileValidationHelper.ValidateFileSignature`.
7. يحدد content type من الامتداد.
8. يخزن الملف باستخدام `IFileStorageService.SaveFileWithGuidAsync`.
9. ينشئ `Invoice`:
   - مربوط بـ PO.
   - مربوط بالـ vendor من الـ PO.
   - مربوط بالمستخدم اللي رفع.
   - status = `Uploaded`.
   - فيه `UploadedFile`.
   - فيه أول processing log.
10. يحفظ في DB.
11. يغير status إلى `Queued`.
12. يضيف processing log جديدة.
13. يحفظ مرة تانية.
14. يعمل enqueue لـ `InvoiceProcessingJob`.
15. يرجع DTO فيه invoice id و status queued.

ليه اتعمل save قبل queue؟

عشان لما Hangfire job تبدأ، تلاقي invoice record موجود فعلا في DB ومعاه id حقيقي.

#### `GetByIdAsync`

بيجيب invoice بالتفاصيل:

- items.
- discrepancies.
- processing logs.
- uploaded files.
- uploaded user.

وبعدها يطبق ownership check:

- المستخدم يقدر يشوف invoice لو هو اللي رفعها.
- Admin يقدر يشوف أي invoice.

بعدها يرجع `InvoiceDetailDto`.

#### `DownloadFileAsync`

بيجيب invoice ويتأكد من ownership، وبعدها يجيب أول uploaded file ويرجع:

- stream.
- content type.
- original filename.

الملف لا يتم تحميله من public URL. لازم يعدي من authenticated API endpoint.

#### `GetAllPagedAsync`

للـ admin monitoring.

بيعمل:

- page number normalization.
- page size max 50.
- status filter لو موجود.
- mapping إلى list DTO.
- استخراج `LastError` من آخر processing log فيها failed/error.

### `InvoiceProcessingJob.cs`

ده Hangfire job wrapper.

الكود بسيط جدا:

```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 30, 60])]
public async Task ProcessAsync(int invoiceId, CancellationToken cancellationToken)
{
    await _processingService.ProcessInvoiceAsync(invoiceId, cancellationToken);
}
```

المهم هنا:

- Hangfire هيشغل job في background.
- retry policy: 3 محاولات.
- delays: 10 ثواني، 30 ثانية، 60 ثانية.
- لو service رمت exception، Hangfire تعتبر المحاولة failed وتعيدها حسب السياسة.

### `InvoiceProcessingService.cs`

ده orchestrator بتاع pipeline بعد ما الفاتورة تدخل queue.

#### flow الداخلي

1. تحميل invoice بكل التفاصيل.
2. transition إلى `Processing`.
3. تحميل الملف من storage.
4. إرسال الملف إلى AI service.
5. transition إلى `Extracted`.
6. validate response.
7. لو validation فشل:
   - status = `NeedsReview`
   - log فيه الأخطاء
   - stop pipeline
8. لو validation نجح:
   - map AI response إلى invoice fields.
   - clear old invoice items.
   - add extracted invoice items.
   - save raw AI JSON في `AIExtractionResult`.
9. transition إلى `Validated`.
10. نداء `IReconciliationService.ReconcileAsync`.
11. لو حصل exception:
   - status = `Failed`
   - processing log بـ `ProcessingError`
   - save
   - rethrow عشان Hangfire retry يشتغل.

#### ValidateAIResponse

بتتأكد من:

- `VendorName` مطلوب.
- `InvoiceNumber` مطلوب.
- `InvoiceDate` مطلوب.
- `Total > 0`.
- لازم يوجد item واحد على الأقل.
- كل item لازم يحتوي:
  - `SupplierSku`
  - `Quantity > 0`
  - `UnitPrice > 0`

لو فشل validation، ده مش technical failure. ده `NeedsReview`.

الفرق بين `Failed` و `NeedsReview`:

- `Failed`: مشكلة تقنية، زي AI timeout أو HTTP error أو file missing.
- `NeedsReview`: البيانات موجودة لكن غير مقبولة business-wise، زي missing invoice number أو zero total.

#### MapAIResponse

بتحول رد الـ AI إلى domain:

- `InvoiceNumber`
- `VendorName`
- `InvoiceDate`
- `Currency`
- `Subtotal`
- `Vat`
- `TotalAmount`
- `InvoiceItems`

ملاحظات:

- `InvoiceDate` بيتعمله parse على format `yyyy-MM-dd`.
- لو التاريخ مش parseable بيرجع لـ `DateTime.UtcNow`.
- `Currency` default = `USD`.
- `Subtotal` لو مش موجود بيحسبه من مجموع item amounts.
- `Vat` default = 0.

### `AIExtractionService.cs`

ده integration مع AI service خارجي.

بيستخدم typed `HttpClient` متسجل في DI.

الخطوات:

1. يعمل `MultipartFormDataContent`.
2. يحط الملف كـ `StreamContent`.
3. يحدد content type حسب extension.
4. يبعت POST إلى:

```text
/extract
```

5. يقرأ response body.
6. لو status code مش success يرمي `HttpRequestException`.
7. يعمل deserialize إلى `AIExtractionResponseDto`.
8. لو response مش مفهوم يرمي exception.
9. لو timeout يحصل `TaskCanceledException` ويتسجل في logs.

مهم في الشرح:

الـ AI service دوره استخراج structured data فقط. الـ backend هو اللي بيعمل:

- validation.
- persistence.
- reconciliation.
- status transitions.
- business rules.

### `ReconciliationService.cs`

ده مسؤول عن مقارنة الفاتورة مع Purchase Order.

الخطوات:

1. تحميل invoice بالتفاصيل.
2. تحميل PO بالـ items.
3. لو PO مش موجود:
   - status = `NeedsReview`
   - log = Purchase Order not found
   - stop.
4. transition إلى `Compared`.
5. مسح discrepancies القديمة.
6. لكل invoice item:
   - دور على PO item بحيث:

```text
poItem.Product.Sku == invoiceItem.SupplierSku
```

   - المقارنة case-insensitive.
7. لو SKU مش موجود في PO:
   - create `MissingSku`
8. لو SKU موجود:
   - لو quantity مختلفة create `QuantityMismatch`
   - لو unit price مختلف create `UnitPriceMismatch`
9. بعد كده يلف على PO items:
   - لو في PO item مش موجود في invoice items create `MissingFromInvoice`
10. يحسب مجموع `LineTotal` في invoice items.
11. لو `TotalAmount` لا يساوي المجموع create `AmountMismatch`.
12. transition إلى `Completed` مع message فيها عدد discrepancies.

نقطة مهمة:

وجود discrepancies لا يمنع completion. الفاتورة ممكن تبقى `Completed` ومعاها discrepancies، لأن completion معناها إن المعالجة خلصت، مش إن الفاتورة سليمة 100%.

### `LocalFileStorageService.cs`

ده implementation لـ `IFileStorageService`.

بيقرأ base path من configuration:

```text
FileStorage:BasePath
```

ولو مش موجود يستخدم default:

```text
App_Data/Invoices
```

لما يخزن ملف:

- يعمل directory لو مش موجود.
- يجيب extension الأصلي.
- ينشئ filename بـ GUID.
- يعمل copy للstream.
- يرجع stored path و stored filename.

ولما يحمل:

- يفتح FileStream read-only.

ولما يحذف:

- يمسح الملف لو موجود.

### Repositories

#### `InvoiceRepository.cs`

بيوفر:

- `GetWithDetailsByIdAsync`
- `GetPagedAsync`
- `GetByUserIdAsync`

`GetWithDetailsByIdAsync` بيعمل Include لـ:

- Items
- Discrepancies
- ProcessingLogs
- UploadedFiles
- UploadedByUser

وبيرتب logs حسب `CreatedAt`.

`GetPagedAsync` بيدعم:

- filter by status.
- ordering by CreatedAt desc.
- pagination.
- include uploaded user.
- include processing logs.

#### `InvoiceProcessingLogRepository.cs`

بيجيب logs الخاصة بفاتورة معينة مرتبة زمنيا.

#### `UnitOfWork.cs`

اتعدل عشان يضيف:

- `Invoices`
- `InvoiceProcessingLogs`

### EF Configurations

#### `InvoiceConfiguration.cs`

حدد:

- max length لـ invoice number/vendor/currency.
- decimal precision لـ subtotal/vat/total.
- indexes على:
  - `UploadedByUserId`
  - `Status`
- relationship مع UploadedByUser بـ restrict delete.

#### `InvoiceItemConfiguration.cs`

حدد:

- `SupplierSku` required + max length 50.
- precision لـ price/line total.

#### `DiscrepancyConfiguration.cs`

حدد:

- max lengths.
- relationship مع Invoice.
- relationship اختيارية مع InvoiceItem.
- delete behavior مضبوط عشان نتجنب cascade مشاكل.

#### `InvoiceProcessingLogConfiguration.cs`

حدد:

- `EventType` required max 100.
- `Message` max 2000.
- index مركب:

```text
InvoiceId + CreatedAt
```

وده يساعد في عرض processing history بسرعة وبترتيب زمني.

#### `UploadedFileConfiguration.cs`

حدد lengths للـ filenames والـ content type.

### `ApplicationDbContext.cs`

اتضاف:

```csharp
DbSet<InvoiceProcessingLog> InvoiceProcessingLogs
```

وده عشان EF يعرف الجدول الجديد.

---

## 7. Database Migration

اتضاف migration:

```text
SPIP.Infrastructure/Migrations/20260723031343_AddInvoicePipeline.cs
```

اسمها:

```text
AddInvoicePipeline
```

### أهم تغييرات الـ migration

#### جدول `UploadedFiles`

اتضاف:

- `OriginalFileName`
- `StoredFileName`

واتحدد max length لـ:

- `FileName`
- `ContentType`

#### جدول `Invoices`

اتعدل:

- `PurchaseOrderId` بقى non-nullable.
- `InvoiceNumber` max length 100.

واتضاف:

- `Currency`
- `Subtotal`
- `Vat`
- `UploadedByUserId`
- `VendorName`

واتضاف indexes:

- `IX_Invoices_Status`
- `IX_Invoices_UploadedByUserId`

واتضاف FK:

- `Invoices.UploadedByUserId` -> `Users_Domain.Id`

#### جدول `InvoiceItems`

اتضاف:

- `SupplierSku`

#### جدول `Discrepancies`

اتضاف:

- `DiscrepancyType`
- `InvoiceItemId`

واتحدد max length لـ expected/actual/field name.

#### جدول جديد `InvoiceProcessingLogs`

أعمدته:

- `Id`
- `InvoiceId`
- `FromStatus`
- `ToStatus`
- `EventType`
- `Message`
- `CreatedAt`
- `UpdatedAt`
- `IsDeleted`

وفيه FK على `Invoices`.

#### PermissionCatalogs

اتضاف أو اتحدث permissions:

- `Invoices.Upload`
- `Invoices.View`
- `Invoices.Download`
- `Invoices.ViewAll`
- `VendorMappings.View`
- `VendorMappings.Manage`
- `Dashboard.View`
- `Reports.View`

---

## 8. Configuration

### Hangfire

في `Program.cs`:

```csharp
var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(...);

builder.Services.AddHangfire(config => config.UseSqlServerStorage(hangfireConnection));
builder.Services.AddHangfireServer();
```

المعنى:

- لو فيه `HangfireConnection` يستخدمه.
- لو مش موجود يستخدم `DefaultConnection`.
- لو الاتنين مش موجودين التطبيق يفشل في startup برسالة واضحة.

### Hangfire Dashboard

```csharp
app.MapHangfireDashboard("/hangfire")
   .RequireAuthorization(Permissions.Invoices.ViewAll);
```

الداشبورد محمي ومش متاح للعامة.

### AI Service

في `SPIP.Infrastructure/DependencyInjection/DependencyInjection.cs`:

```csharp
services.AddHttpClient<IAIExtractionService, AIExtractionService>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["AIService:BaseUrl"] ?? "https://your-ai-service-url";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(config.GetValue("AIService:TimeoutSeconds", 30));
});
```

المطلوب في config:

```json
{
  "AIService": {
    "BaseUrl": "https://your-ai-service-url",
    "TimeoutSeconds": 30
  }
}
```

الـ service currently بيبعت POST على:

```text
/extract
```

### File Storage

`LocalFileStorageService` بيستخدم:

```text
FileStorage:BasePath
```

default:

```text
App_Data/Invoices
```

الملفات متخزنة خارج public web root، والتحميل بيتم من endpoint محمي.

### DI Registrations

اتسجلت services:

- `IInvoiceService` -> `InvoiceService`
- `IInvoiceProcessingService` -> `InvoiceProcessingService`
- `IReconciliationService` -> `ReconciliationService`
- `IFileStorageService` -> `LocalFileStorageService`
- `IAIExtractionService` -> `AIExtractionService` عبر HttpClient

واتسجلت repositories:

- `IInvoiceRepository` -> `InvoiceRepository`
- `IInvoiceProcessingLogRepository` -> `InvoiceProcessingLogRepository`

---

## 9. End-to-End Flow من الـ Frontend للـ AI للـ DB

### Step 1: Frontend Upload

الـ frontend يبعت request:

```http
POST /api/invoices/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

Fields:

```text
File = invoice.pdf
PurchaseOrderId = 10
```

### Step 2: API Authorization

`InvoicesController.Upload` عليه:

```csharp
[Authorize(Policy = Permissions.Invoices.Upload)]
```

يعني لازم المستخدم authenticated ومعاه permission upload.

### Step 3: Request Validation

FluentValidation يتأكد إن:

- الملف موجود.
- الحجم أقل من أو يساوي 10MB.
- extension مسموح.
- content type مسموح.
- PO id صحيح كرقم.

### Step 4: Service Business Checks

`InvoiceService.UploadInvoiceAsync` يتأكد من:

- المستخدم موجود في domain users.
- الـ Purchase Order موجود في DB.
- file signature صحيح.

لو أي حاجة فشلت، بيرجع BadRequest ولا ينشئ DB records.

### Step 5: Secure Storage

الملف يتخزن باسم GUID:

```text
App_Data/Invoices/{guid}.pdf
```

ويتم حفظ metadata:

- original filename.
- stored filename.
- storage path.
- content type.
- file size.

### Step 6: Create Invoice Records

ينشأ `Invoice`:

- `PurchaseOrderId`
- `VendorId` من الـ PO.
- `VendorName` من الـ vendor.
- `UploadedByUserId`
- `Status = Uploaded`
- uploaded file.
- processing log initial.

### Step 7: Queue Background Job

بعد حفظ invoice وتحديثها إلى `Queued`:

```csharp
_backgroundJobClient.Enqueue<InvoiceProcessingJob>(
    job => job.ProcessAsync(invoice.Id, CancellationToken.None)
);
```

الـ frontend يحصل على response سريع:

```text
Invoice uploaded and queued for processing.
```

### Step 8: Background Job Starts

Hangfire يشغل:

```text
InvoiceProcessingJob.ProcessAsync(invoiceId)
```

والـ job تنادي:

```text
InvoiceProcessingService.ProcessInvoiceAsync(invoiceId)
```

### Step 9: Processing Status

status يتحول:

```text
Queued -> Processing
```

ويتسجل log.

### Step 10: Read Stored File

الـ processing service يجيب `UploadedFile.StoragePath` ويفتح stream من التخزين المحلي.

### Step 11: Send To AI

`AIExtractionService` يبعت الملف كـ multipart إلى:

```text
{AIService:BaseUrl}/extract
```

مع timeout default 30 ثانية.

### Step 12: Receive AI JSON

الـ AI يرجع JSON بالشكل:

```json
{
  "vendorName": "Acosta Group",
  "invoiceNumber": "97349579",
  "invoiceDate": "2017-10-20",
  "currency": "USD",
  "subtotal": 21.31,
  "vat": 2.13,
  "total": 23.44,
  "items": [
    {
      "supplierSku": "SKU-001",
      "description": "Product name",
      "quantity": 2,
      "unitPrice": 4.49,
      "amount": 8.98
    }
  ]
}
```

### Step 13: Extracted Status

لو الـ AI call نجح:

```text
Processing -> Extracted
```

### Step 14: Validate AI Response

يتحقق من required fields والـ item values.

لو validation فشل:

```text
Extracted -> NeedsReview
```

ويتسجل سبب المشكلة.

لو validation نجح:

- map data to invoice.
- save invoice items.
- save raw AI JSON.

### Step 15: Validated Status

```text
Extracted -> Validated
```

### Step 16: Reconciliation

`ReconciliationService` يقارن invoice items مع PO items عن طريق SKU.

ينتج discrepancy records لو:

- SKU غير موجود في PO.
- PO item غير موجود في invoice.
- quantity مختلفة.
- unit price مختلف.
- invoice total لا يساوي sum line totals.

### Step 17: Completed Status

```text
Validated -> Compared -> Completed
```

والـ log النهائي يذكر عدد discrepancies.

---

## 10. Lifecycle Diagram

```text
Upload request
    |
    v
Validate auth + permissions
    |
    v
Validate file extension/MIME/size/signature
    |
    v
Store file as GUID
    |
    v
Create Invoice + UploadedFile + initial Log
    |
    v
Status: Uploaded
    |
    v
Status: Queued
    |
    v
Hangfire job
    |
    v
Status: Processing
    |
    v
AI extraction
    |
    v
Status: Extracted
    |
    v
Backend validation
    |
    +--> invalid business data --> NeedsReview
    |
    v
Map + persist extracted data
    |
    v
Status: Validated
    |
    v
Reconcile with Purchase Order
    |
    v
Status: Compared
    |
    v
Create discrepancies if any
    |
    v
Status: Completed
```

Technical error path:

```text
Any exception during background processing
    |
    v
Status: Failed
    |
    v
Hangfire retries according to policy
```

---

## 11. Security impact

البرانش زود الأمان في كذا نقطة:

### Authentication and Authorization

كل invoice endpoints عليها `[Authorize]`.

كل operation لها permission منفصلة:

- upload.
- view.
- download.
- view all.

### Ownership Check

Procurement user لا يرى إلا invoices اللي هو رفعها.

Admin يقدر يشوف الكل.

التحقق موجود في:

- `GetByIdAsync`
- `DownloadFileAsync`

### Secure File Handling

الملف:

- لا يتخزن في `wwwroot`.
- لا يتاح عبر URL مباشر.
- يتخزن باسم GUID.
- يتم تحميله فقط من endpoint محمي.

### File Validation

في طبقتين:

1. FluentValidation:
   - extension.
   - content type.
   - size.
2. File signature helper:
   - magic number.

ده يمنع renamed malicious files.

### Hangfire Dashboard Protection

dashboard مش مفتوح؛ محمي بـ:

```text
Invoices.ViewAll
```

---

## 12. Error handling and retry

### Business validation errors

لو AI رجع JSON ناقص أو values غير منطقية:

- status = `NeedsReview`
- log فيه تفاصيل.
- pipeline يقف قبل reconciliation.
- لا يتم throw exception.

ليه؟

لأن دي مش مشكلة infrastructure. دي فاتورة محتاجة مراجعة بشرية.

### Technical errors

لو حصل:

- AI timeout.
- AI service HTTP error.
- file not found.
- deserialization failure.
- exception غير متوقعة.

الـ service:

- status = `Failed`
- يضيف processing log.
- يعمل rethrow.

ليه rethrow؟

عشان Hangfire يعرف إن job failed ويطبق retry policy.

### Retry policy

في `InvoiceProcessingJob`:

```text
Attempts = 3
Delays = 10s, 30s, 60s
```

---

## 13. Reconciliation details

المقارنة بتتم باستخدام:

```text
InvoiceItem.SupplierSku
```

ضد:

```text
PurchaseOrderItem.Product.Sku
```

المقارنة case-insensitive.

### Scenarios

#### SKU في الفاتورة مش موجود في PO

ينشأ:

```text
DiscrepancyType.MissingSku
FieldName = SupplierSku
ExpectedValue = N/A
ActualValue = invoice SKU
```

#### PO item مش موجود في الفاتورة

ينشأ:

```text
DiscrepancyType.MissingFromInvoice
FieldName = SupplierSku
ExpectedValue = PO SKU
ActualValue = N/A
```

#### Quantity mismatch

ينشأ:

```text
DiscrepancyType.QuantityMismatch
FieldName = Quantity
ExpectedValue = PO quantity
ActualValue = invoice quantity
```

#### Unit price mismatch

ينشأ:

```text
DiscrepancyType.UnitPriceMismatch
FieldName = UnitPrice
ExpectedValue = PO unit price
ActualValue = invoice unit price
```

#### Total mismatch

ينشأ:

```text
DiscrepancyType.AmountMismatch
FieldName = TotalAmount
ExpectedValue = sum of invoice line totals
ActualValue = invoice total
```

---

## 14. ليه استخدمنا Background Job؟

لأن invoice processing ممكن ياخد وقت:

- قراءة ملف.
- AI/OCR processing.
- network call.
- validation.
- database writes.
- reconciliation.

لو كل ده حصل في request مباشر، الـ frontend هيستنى وممكن يحصل timeout وتجربة المستخدم تبقى سيئة.

بـ Hangfire:

- المستخدم ياخد confirmation بسرعة.
- الشغل الثقيل يحصل في الخلفية.
- فيه retry لو حصل error.
- فيه dashboard للمراقبة.
- status/logs يخلوا المستخدم يراجع التقدم.

---

## 15. Clean Architecture في التنفيذ

التقسيم كان كالتالي:

### API

مسؤولة عن:

- HTTP endpoints.
- authorization attributes.
- response wrapping.

### Application

مسؤولة عن:

- DTOs.
- Interfaces.
- Validators.
- Mapping profile.
- Contracts بين الطبقات.

### Domain

مسؤولة عن:

- Entities.
- Enums.
- Permissions constants.
- Core business concepts.

### Infrastructure

مسؤولة عن:

- EF repositories.
- DB context/configurations.
- Hangfire job.
- File storage.
- AI HTTP integration.
- Business service implementations.

نقطة شرح قوية:

الـ AI service نفسه معزول خلف `IAIExtractionService`، فلو غيرنا AI provider بعدين، ممكن نغير implementation في Infrastructure من غير ما نغير controller أو domain.

---

## 16. SpecKit and documentation artifacts

البرانش أضاف ملفات كثيرة تحت:

```text
.specify/
.agents/skills/
specs/001-invoice-processing-pipeline/
```

دي مش runtime code، لكنها design/process artifacts.

### `.specify/`

بتحتوي على:

- templates.
- scripts.
- workflow metadata.
- constitution.
- integration manifests.

الهدف منها تنظيم طريقة كتابة specs/plans/tasks.

### `.agents/skills/`

دي skills خاصة بـ SpecKit workflow:

- clarify.
- plan.
- tasks.
- implement.
- analyze.
- converge.
- وغيرها.

الغرض منها مساعدة agent أو workflow يمشي feature development بطريقة منظمة.

### `specs/001-invoice-processing-pipeline/`

فيها design artifacts للميزة:

- `spec.md`: المتطلبات وقصص المستخدم والـ acceptance criteria.
- `plan.md`: خطة التنفيذ التقنية.
- `data-model.md`: وصف الموديل والعلاقات.
- `contracts/api-contracts.md`: API contracts.
- `research.md`: قرارات بحثية.
- `quickstart.md`: سيناريوهات تشغيل/تحقق.
- `tasks.md`: tasks تفصيلية، ومعظمها marked done.
- `checklists/requirements.md`: checklist للمتطلبات.

### `Plan.md`

ملف عالي المستوى يشرح Sprint 3:

- أهداف sprint.
- actors.
- workflow.
- business rules.
- lifecycle.
- security requirements.
- out of scope.

### `SPIP_Backend_Complete_Technical_Documentation.md`

توثيق backend شامل اتضاف في البرانش، غالبا كمرجع تقني عام أوسع من feature واحدة.

---

## 17. API examples

### Upload invoice

```http
POST /api/invoices/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data
```

Form fields:

```text
File = invoice.pdf
PurchaseOrderId = 10
```

Success:

```json
{
  "success": true,
  "message": "Invoice uploaded and queued for processing.",
  "data": {
    "invoiceId": 42,
    "status": "Queued",
    "fileName": "invoice.pdf"
  }
}
```

### Get invoice details

```http
GET /api/invoices/42
Authorization: Bearer <token>
```

يرجع:

- invoice header.
- items.
- discrepancies.
- processing logs.

### Download invoice file

```http
GET /api/invoices/42/download
Authorization: Bearer <token>
```

يرجع file stream باسم الملف الأصلي و content type الصحيح.

### Admin list

```http
GET /api/invoices?status=Failed&pageNumber=1&pageSize=10
Authorization: Bearer <admin-token>
```

---

## 18. نقاط مهمة تقولها في المقابلة

### Elevator pitch

في البرانش ده نفذنا end-to-end invoice processing pipeline. المستخدم بيرفع invoice مرتبطة بـ Purchase Order، backend بيتحقق من الملف وبيخزنه بأمان، وبعدها بيحط job في Hangfire عشان المعالجة تحصل asynchronously. الـ job تبعت الملف لـ AI extraction service، تستقبل structured JSON، تعمل validation وmapping، تحفظ البيانات، وتقارن line items مع الـ PO باستخدام Supplier SKU، وتسجل أي discrepancies، مع processing logs لكل status transition.

### ليه معمولة async؟

عشان AI extraction وdocument processing عمليات بطيئة وممكن تفشل أو تعمل timeout. بدل ما الـ frontend يستنى، بنرجع response سريع بحالة `Queued`، ونخلي Hangfire يتعامل مع التنفيذ والـ retries.

### إيه دور الـ AI؟

الـ AI مسؤول فقط عن document understanding واستخراج JSON من الملف. لكنه لا يقرر business rules، ولا يحدث DB، ولا يعمل reconciliation. كل business logic في backend.

### إيه أهم security decisions؟

- JWT + permissions لكل endpoint.
- ownership checks.
- file stored outside web root.
- GUID filename.
- download through secured endpoint فقط.
- validation بالextension والـ MIME والـ magic number.

### إيه الفرق بين Failed و NeedsReview؟

- `Failed`: خطأ تقني، مثل AI service timeout أو HTTP failure. ده يخلي Hangfire retry يشتغل.
- `NeedsReview`: البيانات المستخرجة غير صالحة business-wise، مثل missing invoice number أو total <= 0. ده لا يحتاج retry غالبا، يحتاج تدخل بشري.

### إيه أنواع discrepancies؟

- Missing SKU.
- Missing from invoice.
- Quantity mismatch.
- Unit price mismatch.
- Amount mismatch.

### إيه تأثير processing logs؟

بتخلي كل خطوة auditable. نقدر نعرف الفاتورة وصلت لأي status، وفشلت ليه، واتعملها extraction ولا reconciliation، وده مهم للـ admin monitoring والـ debugging.

---

## 19. ملاحظات تقنية محتملة لو اتسألت

### هل الـ AI API endpoint configurable؟

الـ base URL configurable من:

```text
AIService:BaseUrl
```

والtimeout configurable من:

```text
AIService:TimeoutSeconds
```

لكن path المستخدم حاليا داخل service هو:

```text
/extract
```

لو عايزين نخليه fully configurable بعدين ممكن نضيف:

```text
AIService:ExtractPath
```

### هل في tolerance للـ price comparison؟

لا، حسب الـ MVP المقارنة exact match. أي اختلاف في unit price يعتبر discrepancy.

### هل duplicate invoices ممنوعة؟

لا، duplicate detection خارج scope. كل upload بيعتبر invoice record مستقل.

### هل processing completion بيعمل notification؟

لا، SignalR/notifications خارج scope. المستخدم يقدر يعمل polling على status endpoint.

### هل الملفات public؟

لا. التخزين محلي خارج web root، والتحميل من endpoint محمي.

### هل discrepancies تمنع completion؟

لا. الفاتورة ممكن تكون `Completed` ومعاها discrepancies، لأن completed معناها إن pipeline خلصت.

---

## 20. Known caveats / حاجات محتاجة مراجعة لاحقا

دي نقاط ممكن تذكرها كتحسينات مستقبلية لو اتسألت:

- جعل AI extract path configurable بدل hardcoded `/extract`.
- إضافة API key/header للـ AI service لو مطلوب.
- إضافة tests end-to-end للـ quickstart scenarios.
- إضافة tolerance configurable للأسعار لو business طلبت.
- إضافة duplicate invoice detection.
- إضافة SignalR notification لما processing تخلص.
- إضافة virus scanning لو النظام هيقبل ملفات production من موردين خارجيين.
- تحسين handling لحالة date parsing بدل fallback لـ `DateTime.UtcNow`.
- تسجيل AI confidence score الحقيقي لو AI service يرجعه بدل `1.0`.

---

## 21. One-minute explanation

لو محتاج تقولها في دقيقة:

> البرانش ده حول invoice upload من مجرد file upload إلى processing pipeline كاملة. الـ frontend يرفع invoice مع PurchaseOrderId، backend يعمل authentication/authorization وfile validation قوي يشمل magic number، يخزن الملف باسم GUID خارج web root، ينشئ invoice وuploaded file وprocessing logs، ثم يستخدم Hangfire عشان يشغل processing asynchronously. الـ background job تقرأ الملف وتبعته لـ AI extraction service، تستقبل typed JSON، تعمل business validation، تحفظ header/items وraw AI result، وبعدها تعمل reconciliation مع Purchase Order items باستخدام SupplierSku. أي اختلافات في SKU أو quantity أو unit price أو total تتحفظ كـ discrepancies. طول الرحلة status بيتغير من Uploaded لQueued لProcessing لExtracted لValidated لCompared لCompleted، ولو حصل technical error تبقى Failed وتتعمل retry، ولو البيانات ناقصة تبقى NeedsReview. كمان اتضافت permissions، repositories، EF migration، DTOs، AutoMapper profile، وconfiguration لـ Hangfire والـ AI والـ file storage.

---

## 22. ملف سريع بأسماء أهم الملفات

### أهم runtime files

```text
SPIP.API/Controllers/InvoicesController.cs
SPIP.API/Program.cs
SPIP.Application/DTOs/Invoice/*
SPIP.Application/DTOs/AI/AIExtractionResponseDto.cs
SPIP.Application/DTOs/AI/AIExtractionItemDto.cs
SPIP.Application/Validators/UploadInvoiceRequestValidator.cs
SPIP.Application/Helpers/FileValidationHelper.cs
SPIP.Application/Mapping/InvoiceMappingProfile.cs
SPIP.Application/Interfaces/Services/IInvoiceService.cs
SPIP.Application/Interfaces/Services/IInvoiceProcessingService.cs
SPIP.Application/Interfaces/Services/IReconciliationService.cs
SPIP.Application/Interfaces/AI/IAIExtractionService.cs
SPIP.Application/Interfaces/Storage/IFileStorageService.cs
SPIP.Domain/Entities/Invoice.cs
SPIP.Domain/Entities/InvoiceItem.cs
SPIP.Domain/Entities/Discrepancy.cs
SPIP.Domain/Entities/UploadedFile.cs
SPIP.Domain/Entities/InvoiceProcessingLog.cs
SPIP.Domain/Enums/InvoiceStatus.cs
SPIP.Domain/Enums/DiscrepancyType.cs
SPIP.Domain/Constants/Permissions.cs
SPIP.Infrastructure/BackgroundJobs/InvoiceProcessingJob.cs
SPIP.Infrastructure/Services/InvoiceService.cs
SPIP.Infrastructure/Services/InvoiceProcessingService.cs
SPIP.Infrastructure/Services/AIExtractionService.cs
SPIP.Infrastructure/Services/ReconciliationService.cs
SPIP.Infrastructure/Services/LocalFileStorageService.cs
SPIP.Infrastructure/Repositories/InvoiceRepository.cs
SPIP.Infrastructure/Repositories/InvoiceProcessingLogRepository.cs
SPIP.Infrastructure/Persistence/Configurations/*
SPIP.Infrastructure/Migrations/20260723031343_AddInvoicePipeline.cs
```

### أهم docs/spec files

```text
Plan.md
SPIP_Backend_Complete_Technical_Documentation.md
specs/001-invoice-processing-pipeline/spec.md
specs/001-invoice-processing-pipeline/plan.md
specs/001-invoice-processing-pipeline/tasks.md
specs/001-invoice-processing-pipeline/data-model.md
specs/001-invoice-processing-pipeline/contracts/api-contracts.md
specs/001-invoice-processing-pipeline/quickstart.md
```

