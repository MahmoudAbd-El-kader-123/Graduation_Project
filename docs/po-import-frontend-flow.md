# Purchase-order import: Angular integration

The PO import is a three-stage workflow:

1. Preview the workbook.
2. Save the selected vendor's column mappings.
3. Import the workbook and use the returned PO ID.

All requests require a JWT. Authenticate with `POST /api/auth/login`, copy
`data.token`, and send it as:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

## 1. Preview the workbook

```http
POST /api/purchase-orders/import-preview?rowsToExtract=50
Content-Type: multipart/form-data
```

Form field:

| Field | Type |
|---|---|
| `file` | Excel file |

The response contains `data.dataGrid`, an array of rows and cells, and
`data.totalRowsFound`. Preview does not write to the database.

Angular should:

1. Render `dataGrid`.
2. Let the user choose the row containing the item headers.
3. Use the cell strings from that row as the available Excel headers.

The selected row number is not currently sent to the import endpoint. The
backend scans the first 50 rows during import and locates the first row
containing a saved mapped header. Save header strings exactly as returned by
the preview.

## 2. Load and save vendor mappings

Load existing mappings whenever the selected vendor changes:

```http
GET /api/vendor-mappings/vendor/{vendorId}
```

Save each chosen mapping:

```http
POST /api/vendor-mappings
Content-Type: application/json
```

```json
{
  "vendorId": 1,
  "systemField": "SkuSupplier",
  "excelColumn": "Item ID / SKU"
}
```

The same endpoint creates a missing mapping or updates the existing mapping
for the same vendor and system field.

Required mappings:

| System field | Example Excel header |
|---|---|
| `SkuSupplier` | `Item ID / SKU` |
| `Quantity` | `Quantity / الكمية` |
| `UnitPrice` | `Unit Price / السعر (SAR)` |

Recommended mappings:

| System field | Example Excel header | Use |
|---|---|---|
| `Description` | `Description / اسم الصنف` | New product name and description |
| `Uom` | `Unit / الوحدة` | New product unit of measure |
| `VatAmount` | `VAT Amount (SAR)` | PO item VAT calculation |
| `Barcode` | Vendor-specific barcode header | Product-match fallback |

Angular must wait for every mapping request to succeed before importing.

## 3. Import the PO

Send the original workbook:

```http
POST /api/purchase-orders/import
Content-Type: multipart/form-data
```

Form fields:

| Field | Type | Meaning |
|---|---|---|
| `File` | Excel file | Original workbook |
| `VendorId` | integer | Selected vendor |
| `HasMixedVatRates` | boolean | `true` when item VAT rates differ |

Example success response:

```json
{
  "success": true,
  "message": "Purchase order imported successfully.",
  "data": {
    "purchaseOrderId": 42,
    "itemsImported": 9,
    "productsCreated": 9,
    "productsMatched": 0,
    "rowsSkipped": 1
  },
  "errors": null
}
```

The database generates `purchaseOrderId`; Angular must not assume a fixed ID.
Use it to navigate to or load the imported PO:

```http
GET /api/purchase-orders/{purchaseOrderId}
```

## Backend database behavior

The import loads products belonging to `VendorId` and processes each valid
item row:

1. Match `Products.SkuSupplier` case-insensitively.
2. If SKU does not match and `Barcode` is mapped, try the barcode.
3. If a product matches, reuse its `ProductId`.
4. If no product matches, insert a product for the selected vendor.
5. Insert a `PurchaseOrderItem` referencing that product.
6. Insert the `PurchaseOrder` and all newly created products and items with one
   `SaveChanges` call.

New product values:

| Product field | Source |
|---|---|
| `VendorId` | Import request |
| `SkuSupplier` | Mapped `SkuSupplier` cell |
| `ErpId` | Mapped `SkuSupplier` cell |
| `Name` | Mapped `Description`, otherwise supplier SKU |
| `Description` | Mapped `Description` |
| `Uom` | Mapped `Uom`, otherwise `PCS` |
| `Barcode` | Mapped `Barcode` |
| `UnitPrice` | Mapped `UnitPrice` |

Existing product master data is not overwritten by the PO. Its ID is reused,
while the PO item stores the imported quantity and unit price.

PO item calculations:

```text
LineTotal    = Quantity × UnitPrice
VatAmount    = mapped VAT amount or the deduced common VAT amount
Amount       = LineTotal + VatAmount
PO Total     = sum of item Amount
```

## Angular result handling

After a successful import:

- Show `itemsImported`.
- Show `productsCreated` when new catalog entries were added.
- Show `productsMatched` when existing products were reused.
- Warn when `rowsSkipped` is greater than zero.
- Navigate using `purchaseOrderId`.

Suggested TypeScript shape:

```typescript
export interface PurchaseOrderImportResult {
  purchaseOrderId: number;
  itemsImported: number;
  productsCreated: number;
  productsMatched: number;
  rowsSkipped: number;
}
```

## Failure handling

| Failure | Angular response |
|---|---|
| `No column mappings found for this vendor.` | Return to mapping step |
| Required SKU, quantity, or price mapping missing | Highlight required mappings |
| Header row cannot be located | Ask the user to verify exact header selections |
| `No valid items found in the Excel file.` | Show invalid/empty item-row message |
| HTTP `401` | Reauthenticate |
| HTTP `403` | Show insufficient permission |
| Other HTTP `400` | Display the API `message` and preserve the selected file/mappings |

Do not navigate away from the import page until `success` is `true`.
